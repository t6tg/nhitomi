import { Message, MessageEmbed, MessageEmbedOptions, MessageReaction, PartialMessage, User, PartialUser } from 'discord.js'
import { Lock } from 'semaphore-async-await'
import deepEqual from 'fast-deep-equal'
import config from 'config'
import { MessageContext } from './context'
import { Locale } from './locales'

type InteractiveInput = {
  userId: string
  channelId: string
  resolve(message: Message): void
  resolveDirect(message: string): void
  reject(error?: Error): void
}

const pendingInputs: InteractiveInput[] = []

const interactives: { [id: string]: InteractiveMessage } = {}
export function getInteractive(message: Message | PartialMessage): InteractiveMessage | undefined { return interactives[message.id] }

export type RenderResult = {
  message?: string
  embed?: MessageEmbedOptions
}

/** Represents an interactive, stateful message. */
export abstract class InteractiveMessage {
  /** Lock must be consumed during all stateful operation on this message. */
  readonly lock: Lock = new Lock()

  /** Timeout responsible for destroying this interactive after a delay. */
  readonly timeout = setTimeout(() => this.destroy(true), config.get<number>('interactive.timeout') * 1000)

  /** Context of the command message. */
  context?: MessageContext

  /** Message that contains the rendered interactive content. */
  rendered?: Message

  /** List of triggers that alter interactive state. */
  triggers?: ReactionTrigger[]

  private lastView: {
    message?: string
    embed?: MessageEmbed
  } = {}

  /** Initializes this interactive message. Context need not be ref'ed. */
  initialize(context: MessageContext): Promise<boolean> {
    if (this.context) {
      delete interactives[this.context.message.id]
      this.context.destroy()
    }

    this.context = context.ref()
    interactives[this.context.message.id] = this

    if (this.rendered) {
      delete interactives[this.rendered.id]
      this.rendered = undefined
    }

    return this.update()
  }

  /** Renders this interactive immediately. */
  async update(): Promise<boolean> {
    await this.lock.wait()
    try {
      this.timeout.refresh()

      const result = await this.render(this.context?.locale ?? Locale.default)
      const view = {
        message: result.message,
        embed: result.embed ? new MessageEmbed(result.embed) : undefined
      }

      if (!view.message && !view.embed)
        return false

      const lastRendered = this.rendered

      if (this.rendered?.editable) {
        if (deepEqual(this.lastView, view)) {
          console.debug('skipping rendering for interactive', this.constructor.name, this.rendered.id)
          return false
        }

        this.rendered = await this.rendered.edit(view.message, view.embed)
      }
      else {
        this.rendered = await this.context?.reply(view.message, view.embed)
      }

      if (lastRendered) delete interactives[lastRendered.id]
      if (this.rendered) {
        interactives[this.rendered.id] = this

        console.debug('rendered interactive', this.constructor.name, this.rendered.id)

        if (this.rendered.id !== lastRendered?.id) {
          const triggers = this.triggers = this.createTriggers()
          const rendered = this.rendered

          for (const trigger of triggers)
            trigger.interactive = this

          // attach triggers outside lock
          setTimeout(async () => {
            for (const trigger of triggers)
              try { trigger.reaction = await rendered.react(trigger.emoji) }
              catch { /* ignored */ }
          }, 0)
        }
      }

      this.lastView = view
      return true
    }
    finally {
      this.lock.signal()
    }
  }

  /** Creates a list of triggers that alter the state of this interactive. */
  protected createTriggers(): ReactionTrigger[] { return [] }

  /** Constructs a new view of this interactive. */
  protected abstract render(l: Locale): Promise<RenderResult>

  readonly ownedInputs: InteractiveInput[] = []

  /** Waits for a message from the owner of this interactive. This will never reject. */
  async waitInput(content: string, timeout?: number): Promise<string> {
    const context = this.context

    if (!context)
      return ''

    const sent = await context.reply(content)

    return new Promise<string>(resolve => {
      const input: InteractiveInput = {
        userId: context.message.author.id,
        channelId: context.message.channel.id,

        resolveDirect: async message => {
          const i1 = pendingInputs.indexOf(input)
          if (i1 !== -1) pendingInputs.splice(i1, 1)

          const i2 = this.ownedInputs.indexOf(input)
          if (i2 !== -1) this.ownedInputs.splice(i2, 1)

          resolve(message)

          try { if (sent.deletable) await sent.delete() }
          catch { /* ignored */ }
        },

        resolve: async received => {
          input.resolveDirect(received.content)

          try { if (received.deletable) await received.delete() }
          catch { /* ignored */ }
        },

        reject: () => input.resolveDirect('')
      }

      pendingInputs.push(input)
      this.ownedInputs.push(input)

      setTimeout(input.reject, (timeout || config.get<number>('interactive.inputTimeout')) * 1000)
    })
  }

  /**
   * Destroys this interactive, deleting all related messages.
   * @param expiring true if interactive is being destroyed because it expired
   */
  async destroy(expiring?: boolean): Promise<void> {
    // reject pending inputs before entering lock to ensure it gets freed
    for (const input of this.ownedInputs) input.reject()

    await this.lock.wait()
    try {
      if (this.rendered) console.debug('destroying interactive', this.constructor.name, this.rendered.id, 'expiring', expiring || false)

      for (const input of this.ownedInputs)
        input.reject()

      try { if (!expiring && this.context?.message.deletable) await this.context.message.delete() }
      catch { /* ignored */ }

      if (this.context) {
        delete interactives[this.context.message.id]
        this.context.destroy()
        this.context = undefined
      }

      try { if (!expiring && this.rendered?.deletable) await this.rendered.delete() }
      catch { /* ignored */ }

      if (this.rendered) {
        delete interactives[this.rendered.id]
        this.rendered = undefined
      }

      this.triggers = undefined
    }
    finally {
      this.lock.signal()
    }
  }
}

export async function handleInteractiveMessage(message: Message): Promise<boolean> {
  const userId = message.author.id
  const channelId = message.channel.id

  for (const input of pendingInputs)
    if (input.userId === userId && input.channelId === channelId) {
      input.resolve(message)
      return true
    }

  return false
}

export async function handleInteractiveMessageDeleted(message: Message | PartialMessage): Promise<boolean> {
  const interactive = getInteractive(message)

  if (!interactive || message.id !== interactive.rendered?.id)
    return false

  await interactive.destroy()
  return true
}

export async function handleInteractiveReaction(reaction: MessageReaction, user: User | PartialUser): Promise<boolean> {
  const interactive = getInteractive(reaction.message)

  if (!interactive || reaction.message.id !== interactive.rendered?.id)
    return false

  // reactor must be command author
  if (user.id !== interactive.context?.message.author.id)
    return false

  // prevent triggers while pending inputs
  if (interactive.ownedInputs.length)
    return false

  const trigger = interactive.triggers?.find(t => t.emoji === reaction.emoji.name)

  if (!trigger)
    return false

  return await trigger.invoke()
}

/** Represents an interactive trigger that is invoked via reactions. */
export abstract class ReactionTrigger {
  abstract readonly emoji: string

  interactive?: InteractiveMessage
  reaction?: MessageReaction

  get context(): MessageContext | undefined { return this.interactive?.context }

  /** Runs this trigger immediately. */
  async invoke(): Promise<boolean> {
    const interactive = this.interactive

    if (!interactive?.rendered)
      return false

    let result: boolean

    await interactive.lock.wait()
    try {
      console.debug('invoking trigger', this.emoji, 'for interactive', interactive.constructor.name, interactive.rendered.id)

      result = await this.run()
    }
    finally {
      interactive.lock.signal()
    }

    if (result)
      result = await interactive.update()

    return result
  }

  /** Alters the state of the interactive while the message is locked. Returning true will rerender the interactive. */
  protected abstract run(): Promise<boolean>
}
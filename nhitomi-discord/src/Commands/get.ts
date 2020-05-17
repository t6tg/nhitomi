import { CommandFunc } from '.'
import { InteractiveMessage, RenderResult, ReactionTrigger } from '../interactive'
import { Locale } from '../locales'
import { Book, BookContent, BookTag, ObjectType, SpecialCollection } from 'nhitomi-api'
import { Api } from '../api'
import { DestroyTrigger } from '../Triggers/destroy'
import { MessageContext } from '../context'
import { Message } from 'discord.js'
import { ReadTrigger } from '../Triggers/read'
import { FavoriteTrigger, FavoriteTriggerTarget } from '../Triggers/favorite'

export class BookMessage extends InteractiveMessage {
  constructor(
    readonly book: Book,
    public content: BookContent
  ) {
    super()
  }

  protected async render(l: Locale): Promise<RenderResult> {
    return BookMessage.renderStatic(l, this.book, this.content)
  }

  static renderStatic(l: Locale, book: Book, content: BookContent): RenderResult {
    l = l.section('get.book')

    return {
      message: Object.values(book.contents.reduce((a, b) => {
        const k = `${b.source}/${b.language}`;
        (a[k] = a[k] || []).push(b)
        return a
      }, {} as Record<string, BookContent[]>)).map(x => {
        const { source, language } = x[0]

        const urls = x.map(c => c.sourceUrl).sort()

        for (let i = 0; i < urls.length; i++)
          if (urls[i] === content.sourceUrl)
            urls[i] = `**${urls[i]}**`

        return `${source} (${language.split('-')[0]}): ${urls.join(' ')}`
      }).sort().join('\n'),
      embed: {
        title: book.primaryName,
        description: book.englishName === book.primaryName ? undefined : book.englishName,
        url: Api.getWebLink(`books/${book.id}/contents/${content.id}`),
        image: {
          url: Api.getApiLink(`books/${book.id}/contents/${content.id}/pages/0/thumb`)
        },
        color: 'GREEN',
        author: {
          name: (book.tags.artist || book.tags.circle || [content.source]).sort().join(', '),
          iconURL: Api.getWebLink(`assets/icons/${content.source}.jpg`)
        },
        footer: {
          text: `${book.id}/${content.id} (${l.section('categories').get(book.category)}, ${l.get('pageCount', { count: content.pageCount })})`
        },
        fields: Object.values(BookTag).filter(t => book.tags[t]?.length).map(t => ({
          name: l.section('tags').get(t),
          value: book.tags[t]?.sort().join(', '),
          inline: true
        }))
      }
    }
  }

  get favoriteObject(): FavoriteTriggerTarget['favoriteObject'] {
    return {
      id: this.book.id,
      name: this.book.primaryName
    }
  }

  protected createTriggers(): ReactionTrigger[] {
    return [
      ...super.createTriggers(),

      new FavoriteTrigger(this, ObjectType.Book, SpecialCollection.Favorites),
      new ReadTrigger(this),
      new DestroyTrigger()
    ]
  }
}

export async function handleGetLink(context: MessageContext, link: string | undefined): Promise<{
  type: ObjectType.Book
  book: Book
  content: BookContent
} | {
  type: 'notFound'
}> {
  if (link) {
    // try finding books
    const { matches: [bookMatch] } = await context.api.book.getBooksByLink({ strict: true, getBookByLinkRequest: { link } })

    if (bookMatch) {
      const { book, selectedContentId } = bookMatch
      const content = book.contents.find(c => c.id === selectedContentId)

      if (content)
        return { type: ObjectType.Book, book, content }
    }
  }

  return { type: 'notFound' }
}

export function replyNotFound(context: MessageContext, input: string): Promise<Message> {
  const l = context.locale.section('get.notFound')

  return context.reply(`
${input && l.get('message', { input })}

> - ${l.get('usageLink', { example: 'https://nhentai.net/g/123/' })}
> - ${l.get('usageSource', { example: 'hitomi 123' })}`)
}

export const run: CommandFunc = async (context, link) => {
  const result = await handleGetLink(context, link)

  switch (result.type) {
    case ObjectType.Book: {
      const { book, content } = result

      return await new BookMessage(book, content).initialize(context)
    }

    case 'notFound': {
      await replyNotFound(context, link || '')
      return true
    }
  }
}

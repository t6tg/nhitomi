import React, { ComponentProps, ReactNode, useState } from 'react'
import { Tooltip } from './Tooltip'
import { useSpring, animated } from 'react-spring'
import { convertHex } from '../theme'
import { cx } from 'emotion'
import { colors } from '../theme.json'

export const Dropdown = ({ interactive = true, appendTo = document.body, placement = 'bottom-start', touch = true, padding = false, overlayProps, ...props }: ComponentProps<typeof Tooltip>) => {
  return (
    <Tooltip
      interactive={interactive}
      appendTo={appendTo}
      placement={placement}
      touch={touch}
      padding={padding}
      overlayProps={{
        className: 'py-1',
        ...overlayProps
      }}

      {...props} />
  )
}

export const DropdownItem = ({ children, className, padding = true }: { children?: ReactNode, className?: string, padding?: boolean }) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    backgroundColor: convertHex('#fff', hover ? 0.1 : 0)
  })

  return (
    <animated.div
      style={style}
      className={cx('truncate cursor-pointer', { 'px-2 py-1': padding }, className)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      {children}
    </animated.div>
  )
}

export const DropdownGroup = ({ name, children, className }: { name?: ReactNode, children?: ReactNode, className?: string }) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    color: hover ? colors.gray[500] : colors.gray[800]
  })

  return (
    <div className={cx('pl-2', className)}>
      <animated.div style={style} className='cursor-default py-1 truncate'>{name}</animated.div>

      <div
        className='rounded-l-sm overflow-hidden'
        onMouseEnter={() => setHover(true)}
        onMouseLeave={() => setHover(false)}
        children={children} />
    </div>
  )
}

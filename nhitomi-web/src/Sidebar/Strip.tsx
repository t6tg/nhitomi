import React from 'react'
import { cx, css } from 'emotion'
import { ReadFilled, FolderOutlined, InfoCircleOutlined, HeartOutlined, SettingOutlined, BookOutlined, SettingFilled, FolderOpenFilled, InfoCircleFilled } from '@ant-design/icons'
import { RoundIconButton } from '../Components/RoundIconButton'
import { Tooltip } from '../Components/Tooltip'
import { FormattedMessage } from 'react-intl'
import { BookListingLink } from '../BookListing'
import { SettingsLink } from '../Settings'
import { Route, Switch } from 'react-router-dom'
import { useSprings, animated } from 'react-spring'

export const StripWidth = 64

export const Strip = () => {
  const styles = useSprings(5, [0, 1, 2, 3, 4].map(offset => ({
    delay: offset * 20,
    from: { opacity: 0, marginLeft: -5 },
    to: { opacity: 1, marginLeft: 0 }
  })))

  return (
    <div className={cx('fixed top-0 left-0 bottom-0 z-10 text-white py-4 flex flex-col items-center', css`width: ${StripWidth}px;`)}>
      <animated.div className='leading-none mb-4' style={styles[0]}>
        <Tooltip overlay={<span><FormattedMessage id='pages.home.title' /> <HeartOutlined /></span>} placement='right'>
          <BookListingLink>
            <img alt='logo' className='w-10 h-10' src='/logo-40x40.png' />
          </BookListingLink>
        </Tooltip>
      </animated.div>

      <animated.div className='leading-none' style={styles[1]}>
        <Tooltip overlay={<FormattedMessage id='pages.bookListing.title' />} placement='right'>
          <BookListingLink>
            <RoundIconButton>
              <Switch>
                <Route path='/books'><ReadFilled /></Route>
                <Route><BookOutlined /></Route>
              </Switch>
            </RoundIconButton>
          </BookListingLink>
        </Tooltip>
      </animated.div>

      <animated.div className='leading-none' style={styles[2]}>
        <Tooltip overlay={<FormattedMessage id='pages.collectionListing.title' />} placement='right'>
          <RoundIconButton>
            <Switch>
              <Route path='/collections'><FolderOpenFilled /></Route>
              <Route><FolderOutlined /></Route>
            </Switch>
          </RoundIconButton>
        </Tooltip>
      </animated.div>

      <animated.div className='leading-none' style={styles[3]}>
        <Tooltip overlay={<FormattedMessage id='pages.settings.title' />} placement='right'>
          <SettingsLink>
            <RoundIconButton>
              <Switch>
                <Route path='/settings'><SettingFilled /></Route>
                <Route><SettingOutlined /></Route>
              </Switch>
            </RoundIconButton>
          </SettingsLink>
        </Tooltip>
      </animated.div>

      <animated.div className='leading-none' style={styles[4]}>
        <Tooltip overlay={<FormattedMessage id='pages.about.title' />} placement='right'>
          <RoundIconButton>
            <Switch>
              <Route path='/about'><InfoCircleFilled /></Route>
              <Route><InfoCircleOutlined /></Route>
            </Switch>
          </RoundIconButton>
        </Tooltip>
      </animated.div>
    </div>
  )
}
# <img src="https://github.com/cnapolit/ScreenSaver/blob/main/.github/icon.png" width="50"> ScreenSaver Extension [![Crowdin](https://badges.crowdin.net/screensaver/localized.svg)](https://crowdin.com/project/screensaver)
ScreenSaver is an extension to the Video Game Manager app <a href="https://github.com/JosefNemec/Playnite">Playnite</a>
designed to display various content of Games as ScreenSavers. 

[![ScreenSaver Demo](https://img.youtube.com/vi/uFgXJ1UMQro/0.jpg)](https://www.youtube.com/watch?v=uFgXJ1UMQro)

This extension took inspiration in design & implementation from both Playnite Sounds and SplashScreen.
## Notable Features
* Display a ScreenSaver based on the Playnite game library after a set interval of inactivity
  - Supports video, music, logos, and background images
    - Only background images are natively supported
    - See <a href="https://github.com/cnapolit/ScreenSaver#integrated-extensions">Integrated Extensions</a> for details
  - Displays a clock with the date & time
* Supported forms of input include: Mouse, Keyboard, & Gamepad
  - DirectInput (PS4/5, Switch) devices are not currently supported
* Specify groups of games to display in the ScreenSaver
  - select Static lists of games or set a dynamic filter via the main application
* Preview ScreenSavers on a per game basis
* Various customization options via settings
## Updates
### 2.0.0
#### New Features
* Added Groups to Sounds
  - Display particular groups of games
  - Sort by particular fields
  - Static Vs. Dynamic: Specify a list of games or filter via the main view
    - Whitelist: dynamic groups will always include listed games, regardless of filter
    - Sort: overrides filter sort when specified
* Will now mute/unmute Playnite Sounds
* Media content now loops on end
* Can specify a specific monitor to display to
* Displays a clock while active
#### Bugfixes
* ScreenSaver no longer plays during gameplay when it shouldn't
## Integrated Extensions
### <a href="https://github.com/joyrider3774/PlayniteSound">Playnite Sounds</a>
  - Supports a yet to be released version of Sounds
    - Prior versions are not supported.
  - Allows game specific music to be played
  - Mutes/unmutes Sounds when ScreenSaver starts/stops
### <a href="https://github.com/darklinkpower/PlayniteExtensionsCollection">ExtraMetaDataLoader</a>
  - Allows logos & video to be displayed/played
## Contributing
If there's a feature or bug you want to work on for this Extension, let me know!
## Supporting
There's this site, I guess. I do drink coffee, but who knows what I'll spend this on ¯\\\_(ツ)\_/¯

<a href='https://ko-fi.com/justrollinc' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=3' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

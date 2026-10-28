# Customize

**English** · [Français](../customize.md)

## From the remote

| Action | Where |
|---|---|
| Add or remove a favorite | Hold OK on a tile, or *All apps* → hold OK |
| Reorder the favorites | Hold OK → *Move*, then ◀ ▶ and OK |
| Hide an app | Hold OK → *Hide* (Recovery → *Show hidden apps again* to undo) |
| Change the theme | ◐ button at the top right, or Settings → *Theme* with ◀ ▶: *Automatic*, then each of the 5 themes (remembered) |
| Cursor sound | Settings → *Cursor sound*, ◀ ▶: Drop, Wood, Kalimba or Off. The chosen sound plays on each change |
| Home button | Settings → *Home button* |
| Show or hide the weather | Settings → *Weather* (if a place is set) |
| Language | Settings → *Language*, ◀ ▶: *Automatic* (the TV's language), Français or English |
| Volume | Settings → *Volume*: ◀ ▶ adjusts, OK mutes or unmutes |
| HDMI devices | Settings → *HDMI inputs*: for each port, ◀ ▶ picks the device (see below) |
| Start from scratch | Settings → Recovery and diagnostics → *Reset all settings* |

The Settings panel has two parts:
- **Television**: volume, TV settings (Samsung's) and Samsung Home (Smart Hub);
- **Quiet Home**: theme, cursor sound, weather, Home button, language, HDMI inputs, then *Recovery and diagnostics*.

Each setting shows its value on the right and changes in place with ◀ ▶ (or OK), without closing the panel.

The configuration is stored on the TV, in the app's `data` folder. It is kept across updates and erased on uninstall.

## Language

The interface exists in **French** and **English**. With *Automatic* (the default), Quiet Home uses French if the TV's language is French, English otherwise; it checks again when you come back from the TV settings. Changing the value in Settings → *Language* redraws the whole screen at once: rows, date, greeting, weather and panels.

The translations are in `native/src/QuietHome.Core/Lang.cs`: the French text is the key, the table gives the English text. A text missing from the table stays in French. A test checks that every entry keeps its `{0}` placeholders.

## Default favorites and devices

They are defined in `native/src/QuietHome.Core/DefaultConfig.cs`:

- **Favorites**: each `PinSpec` lists candidate Tizen identifiers and a fallback keyword, looked up in the app's name. To find the identifiers on your TV: Settings → Recovery → **Diagnostics**, which lists every installed app with its identifier.
- **HDMI devices**: PS5 on HDMI 2 and Switch on HDMI 4 by default (`InputSpec`). They are changed in the app, without touching the code (see *HDMI inputs*).
- A configuration already saved on the TV is not overwritten. Additions to the defaults go through a **versioned migration** (`ConfigStore.Normalize`, `LauncherConfig.CurrentVersion`).

## HDMI inputs

Settings → *HDMI inputs* lists ports HDMI 1 to 4. On each line, ◀ ▶ picks what is plugged in:

| Device | On the home screen |
|---|---|
| Nothing | no tile |
| PS5, Switch, Xbox | tile with the console's logo |
| Apple TV, Shield | tile with the brand's logo |
| TV box, Soundbar (with an HDMI input), PC, Blu-ray, Device | tile with its name and a pictogram |

The change is saved at once and the home row follows. It is called *Consoles* when it only holds consoles, *Devices* otherwise. The tiles are sorted by port number and show "HDMI n". OK on a tile switches the TV to that port.

The available types are defined in `HdmiPorts.Kinds` (`native/src/QuietHome.Core/HdmiPorts.cs`). Port n matches the source identifier `12 + n` passed to `connect_source` (13 = HDMI 1 on the tested models). To check: Diagnostics → *Test HDMI 1…4*.

## Automatic theme

By default, the home screen uses **Hinoki** (light) during the day and **Kuri** (walnut) in the evening, with a fade:
- with a place set (see [Weather](#weather)), it switches at sunrise and sunset, computed on the TV without any network;
- without a place, the day runs from 7 am to 7 pm.

The day and evening themes are `DayTheme` and `NightTheme` in `DefaultConfig.cs`. The ◐ button pins a theme. It goes back to *Automatic* after the fifth theme. The screensaver stays dark and keeps the wood wall, even with a light theme.

## Weather

A discreet strip at the bottom of the home screen shows the **next 24 hours**: the hour, a pictogram and the temperature. The forecasts come from [Open-Meteo](https://open-meteo.com), without any account or key. The TV makes at most one request per hour, and the last answer is cached: the strip shows up right at startup and lasts a day without network.

The weather is only turned on if you give a place, in `.local/quiethome.json`. This file stays on your PC: `.local/` is never published.

```json
{ "Place": "London", "Latitude": 51.51, "Longitude": -0.13 }
```

`tv-native.ps1 -Action deploy` packs it into the package (`res/local.json`). To remove the place, replace its contents with `{}`, then deploy again. A city's coordinates can be found for example with [Open-Meteo](https://open-meteo.com/en/docs/geocoding-api)'s search. The strip is hidden in Settings → *Weather*. It also disappears if the *Library* row is turned on, for lack of room.

## Brand logos and colors

- **Colors and label → logo matching**: `Themes.Brands`, in `native/src/QuietHome.Core/Themes.cs`.
- **Logos**: `native/tools/fetch_marks.py` downloads the SVGs from Simple Icons v13.21.0 (CC0) and Custom Brand Icons (MIT), then `native/tools/render_marks.py <chrome.exe>` turns them into white PNGs in `native/res/icons/`, at their exact display size: `<name>.png` for cards, `<name>-hdmi.png` for device tiles. The app colors them when drawing.
- A brand without an available logo keeps its color and shows its name. This is the case for Free TV, CANAL+ and ARTE.

## Themes, backgrounds, fonts

| | File / tool |
|---|---|
| Palettes of the 5 themes | `Themes.All`, in `Themes.cs` |
| 4K wood backgrounds (Kuri, Smoked oak) | `native/tools/make_wallpaper.py <chrome.exe>`: procedural grain, built-in readability veil |
| Pictograms (apps, settings, theme, weather, devices) and the app's logo | `native/tools/make_icons.py`, at their exact display size |
| Cursor sounds (Drop, Wood, Kalimba) | `native/tools/make_sounds.py`: pure sine waves, short (0.11 to 0.15 s), rounded attack, low level |
| DM Sans font (OFL) | `native/tools/fetch_fonts.py`, registered at startup through `FontClient.AddCustomFontDirectory` |
| Renders of the home screen (5 themes), the grid, the settings and the screensaver | `native/tools/make_preview_kuri.py [folder] [fr\|en]`, then a capture with headless Chrome |

All generated resources are already in `native/res/`. These tools are only needed to change them.

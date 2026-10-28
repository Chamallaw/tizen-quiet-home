# The Home button shortcut

**English** · [Français](../home-button.md)

## Behavior

| Gesture | Result |
|---|---|
| **Home** from an app (Netflix, YouTube, Jellyfin…) | Back to Quiet Home in under a second, with that app's tile selected. The Samsung bar shows for an instant. |
| **Home twice**: the second press less than 6 s after coming back | Samsung Home stays on screen (Smart Hub, native settings). |
| **Source** → HDMI (PS5, Switch…) | Never intercepted. |
| Settings → *Samsung Home* (or Recovery → *Open Samsung Home*) | Never intercepted. |
| Settings → *Home button*, value *Samsung Home* | Turns off the shortcut and the return at power-on completely. This choice is remembered. |
| **Turning the TV back on** | Back to Quiet Home if the TV resumes the last source, Samsung Home or the app from before standby. Powering on straight into an app (Netflix key, Prime…) is respected. |

### At power-on

In quick standby, the TV keeps Quiet Home in memory, but when it wakes up it resumes the last **source** (often Samsung TV Plus). Quiet Home detects standby in three ways (Power key, empty screen for more than 2 s, or monitoring frozen for more than 60 s) and comes back as soon as an app shows up again:

```
17:30:40 touche Power : mise en veille
17:30:59 premier plan → org.tizen.tv-viewer
17:31:00 retour à Quiet Home (bouton Home ou rallumage)
17:31:07 premier plan → QHLauncher.Home
```

(The log lines are in French: *touche Power : mise en veille* = Power key: standby; *premier plan* = foreground; *retour à Quiet Home* = back to Quiet Home.)

The TV takes a few seconds to wake up before showing Quiet Home: the resumed source stays visible meanwhile. To soften this, in *Settings → General → Smart Features*:
- turn off **Autorun Samsung TV Plus**;
- turn on **Autorun Last App**.

After a complete power cut, Quiet Home is no longer in memory: only *Autorun Last App* can start it again.

## How it works, without touching the system

A third-party app does not receive the Home key: the TV handles it first. Nor is it possible to change the home app without modifying system settings, and the `sdb` shell of recent TVs is closed anyway.

So Quiet Home **watches** the app in the foreground, through the public API `ApplicationManager.GetRunningApplicationsAsync()` polled every second, even while Quiet Home is in the background. The logs show a clear signal:

| What the user does | App in the foreground |
|---|---|
| Watches Netflix | `org.tizen.netflix-app` |
| Presses **Home** | `com.samsung.tv.csfs` (Samsung Home) |
| Switches to a console (**Source**) | `org.tizen.tv-viewer` |

When Samsung Home (`com.samsung.tv.csfs`) comes to the foreground, Quiet Home **relaunches itself** with an ordinary AppControl request. It does not close, kill or modify any other app.

Real log excerpt:

```
16:19:07.948 premier plan → org.tizen.netflix-app
16:19:27.179 premier plan → com.samsung.tv.csfs
16:19:27.383 bouton Home : retour à Quiet Home
16:19:28.051 premier plan → QHLauncher.Home
```

The decision logic (`native/src/QuietHome.Core/HomeRedirect.cs`) is a pure state machine, tested on PC (`HomeRedirectTests.cs`):
- **double press** within 6 s of a return: Samsung Home is allowed until another app comes to the foreground;
- **readings during the transition**: as long as Quiet Home is not back yet, it waits instead of taking it for a second press, then relaunches once after 3 s;
- **explicit permission** (Recovery): it lasts until Samsung Home has been shown and then left.

> **Important implementation detail**: the monitoring runs on a `System.Threading.Timer`. A first version used an NUI timer with `await`: when the app went to the background, the continuation never resumed and the shortcut did not work.

## Limits

- Quiet Home must be **running**, in the foreground or the background. If it is closed, Home is Samsung's again. *Autorun Last App* starts it at power-on.
- The Samsung bar is visible for a split second before the return.
- The shortcut keys handled by the TV (Samsung TV Plus, Netflix, Prime…) cannot be remapped. To check whether a key reaches the app: Settings → Recovery → Diagnostics → *Identify a remote key*.
- Tested on one TV (QN700B, Tizen 6.5). On other firmwares, Samsung Home's identifier might differ: the logs show it (`premier plan → …`).

# Troubleshooting and going back to the original state

**English** · [Français](../troubleshooting.md)

## Common problems

| Symptom | Likely cause | Fix |
|---|---|---|
| `sdb devices` is empty | TV in standby, IP changed or developer mode reset | Turn the TV on, `sdb connect <IP>`, check the PC's IP in developer mode |
| Certificate Manager: blank sign-in page | Windows Server, without Internet Explorer | Create the profile on Windows 10 or 11 and copy it |
| `Le paquet n'est pas signé` (the package is not signed) | Profile not found, or wrong name passed to `-CertProfile` | Check `C:\tizen-studio-data\profile\profiles.xml` |
| Install refused | Distributor certificate without the TV's DUID, or a different author certificate | Recreate the distributor with the right DUID; keep the same author certificate for updates |
| No logs on the PC | Windows firewall, or developer mode off | UDP 5140 rule (see [install.md](install.md)); developer mode pointing to this PC |
| Home opens Samsung Home | Quiet Home closed, or shortcut turned off | Start Quiet Home again; Settings → *Home button* |
| HDMI tile: "refused" or "not available" | Different source identifier on your model | Diagnostics → *Test HDMI*, then adjust `InputSpec` |
| Missing logo | Brand without a free logo | Expected: the tile keeps the brand's color and shows the name |
| Interface in the wrong language | *Automatic* follows the TV's language | Settings → *Language* (*Langue* in French): pick Français or English |

The **Settings → Recovery and diagnostics → Diagnostics** panel shows the configuration path, the language in use, every installed app and the log, even without a PC.

## Going back to the original state (from lightest to most complete)

1. **Home twice**: Samsung Home shows. Nothing was changed on Samsung's side.
2. **Settings → Home button**, value *Samsung Home*: the shortcut is turned off.
3. **Recovery → Reset all settings**: favorites and theme go back to their defaults.
4. **Uninstall the app**: `tv-native.ps1 -Action uninstall`, or from the Apps bar.
5. **TV settings**: set *Autorun Last App* and *Autorun Smart Hub* back to how they were.
6. **Leave developer mode**: Apps → `12345` → Off, then restart the TV.

A factory reset is never needed: Quiet Home changes nothing outside its own folder.

## Slightly soft picture

The TV has every app draw at 1920 × 1080, even on a 4K or 8K panel, then upscales the image. The startup log shows it ("fenêtre 1920x1080 … écran annoncé 1920x1080"). Quiet Home draws its logos, pictograms and text pixel-exact for that 1080p. A slight softening from the upscale remains unavoidable. The TV's picture *Sharpness* setting reduces it a little.

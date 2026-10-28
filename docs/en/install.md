# Step-by-step install

**English** · [Français](../install.md)

Allow about 30 minutes the first time. After that, an update takes one command.

## 0. What you need

- A **Samsung Tizen TV** on the same local network as the PC.
- A **Windows PC**, preferably **Windows 10 or 11**. On Windows Server, the Certificate Manager sign-in page stays blank (see step 3).
- A **Samsung account**, for the certificate.

## 1. Install the tools on the PC

1. [**Tizen Studio**](https://developer.tizen.org/development/tizen-studio/download) (CLI or full version), in `C:\tizen-studio`.
2. Run `C:\tizen-studio\package-manager\package-manager.exe` **as administrator**:
   - *Main SDK* tab: **Certificate Manager** (often already installed);
   - *Extension SDK* tab: **Samsung Certificate Extension** (required). *TV Extensions* is optional.
   - The mobile or watch platforms and the emulator are not needed.
3. **.NET 8 SDK**, for example locally, without administrator rights:

   ```powershell
   Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1; .\dotnet-install.ps1 -Channel 8.0 -InstallDir "$env:USERPROFILE\.dotnet"
   ```

4. **Python 3**, to assemble the package.

## 2. Put the TV in developer mode

1. Note the TV's IP address: *Settings → General → Network → Network Status*.
2. Open **Apps**, type **1 2 3 4 5** on the remote, set *Developer mode* to **On** and enter **the PC's IP address**.
3. Turn the TV off completely, by unplugging it or holding the power button, then turn it back on.
4. Check the connection from the PC:

   ```powershell
   C:\tizen-studio\tools\sdb.exe connect <TV_IP>
   ```

   The TV must appear in `C:\tizen-studio\tools\sdb.exe devices`.

5. *Optional*: `.\tools\tv-fiche.ps1 -TvIp <TV_IP>` writes the TV's sheet (model, Tizen version, architecture) into `.local/`. This script only reads.

## 3. Create the Samsung certificate

1. Open `C:\tizen-studio\tools\certificate-manager\certificate-manager.exe`.
2. Click **+**, choose **Samsung**, then **TV**, and name the profile, for example `QuietHomeTV`.
3. Create the **author certificate** by signing in to your Samsung account.
4. Create the **distributor certificate**:
   - privilege level **Public**;
   - the **TV's DUID** must be in the list. It is detected if the TV is connected through `sdb`; otherwise, add it by hand (`sdb shell 0 getduid`).
5. **Back up** `%USERPROFILE%\SamsungCertificate\<profile>\`: app updates require the same author certificate.

> **Blank sign-in page?** The browser built into the Certificate Manager relies on Internet Explorer, which Windows Server does not have. Create the certificate on a Windows 10 or 11 PC, then copy the profile folder.

> **The TV is not in the list?** It disconnected, for example after going to standby. Run `sdb connect <IP>` again, go back one step, or type the DUID by hand.

## 4. Build, sign, install

```powershell
.\native\tools\tv-native.ps1 -Action test
```

```powershell
.\native\tools\tv-native.ps1 -TvIp <TV_IP> -Action deploy -CertProfile QuietHomeTV
```

`deploy` runs, in order:
1. `dotnet build`;
2. the `.tpk` assembly;
3. the signature, then a check that the package is signed;
4. `sdb install`, checking the message returned by the TV;
5. the app launch.

To **update**, run `deploy` again with a new version number, for example `-Version 1.0.1`. The configuration is kept.

## 5. Start on Quiet Home

- **At power-on**: *Settings → General → Smart Features*:
  - turn on **Autorun Last App**;
  - turn off **Autorun Smart Hub**;
  - open Quiet Home, then turn the TV off.
- **Home button**: nothing to do, Quiet Home handles it as soon as it runs. See [home-button.md](home-button.md).
- **Language**: Quiet Home follows the TV's language (French if the TV is in French, English otherwise). To pick one: Settings → *Language*.

## 6. Watch the logs (recommended while testing)

The app sends its logs over UDP to the PC declared in developer mode, on port 5140. Allow this port **from the TV only**, in a PowerShell run as administrator:

```powershell
New-NetFirewallRule -DisplayName "Quiet Home logs (TV)" -Direction Inbound -Protocol UDP -LocalPort 5140 -RemoteAddress <TV_IP> -Action Allow
```

Then start listening:

```powershell
.\native\tools\tv-native.ps1 -Action logs
```

To remove the rule later: `Remove-NetFirewallRule -DisplayName "Quiet Home logs (TV)"`. The log lines are in French.

## 7. Uninstall

```powershell
.\native\tools\tv-native.ps1 -TvIp <TV_IP> -Action uninstall
```

You can also uninstall from the TV's Apps bar. Then set *Autorun Last App* and *Autorun Smart Hub* back to how they were. Nothing else was changed on the TV.

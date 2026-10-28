# Install and customize with a coding agent

**English** · [Français](../agent.md)

Quiet Home was developed with a coding agent (Claude Code) that drove the PC and the TV. You can do the same to install it, adapt it to your TV or make it evolve.

## What the agent does, and what is left to you

| The agent can | Only you can |
|---|---|
| Read the TV's sheet (`tools/tv-fiche.ps1`) | Turn on developer mode on the TV (remote control) |
| Install the .NET SDK locally, build, test | Install the Tizen extensions (administrator rights) |
| Sign, install, launch the app (`sdb`, Tizen CLI) | Create the Samsung certificate (sign-in to your account) |
| Listen to the logs and diagnose | Open the log port in the firewall (a PC security setting) |
| Adjust favorites, HDMI devices, logos, themes | Press the remote's buttons during tests |
| Generate renders of the interface | Look at the screen: the agent only sees the logs and the renders |

## Starting prompt

Open the repository in your agent, then paste:

```text
Read AGENTS.md then docs/en/install.md. My Samsung TV is in developer mode at <TV_IP>,
with this PC's IP declared. Goal: install Quiet Home on this TV.
1. Make the TV's sheet (tools/tv-fiche.ps1) and tell me whether the model is compatible.
2. Check the tools (Tizen Studio, .NET 8, Python) and install what is missing without admin rights.
3. Stop and guide me through the Samsung certificate and the firewall.
4. Run the tests, deploy, listen to the logs and confirm the app started.
5. Adapt the favorites to the apps actually installed (Diagnostics or logs), and the devices to my HDMI inputs.
Do not change any system setting of the TV or the PC without asking me.
```

AGENTS.md and the code comments are in French; agents read them without trouble.

## Useful prompts afterwards

- *"Add <app> to the favorites, with its official logo if Simple Icons has it."*
- *"My PS5 is on HDMI 3: check it with Diagnostics → Test HDMI and the logs."*
- *"Make me a render of the home screen in the Smoked oak theme, in English."*
- *"The Home button does not come back to Quiet Home: look at the "premier plan →" lines in the logs."*

## Tips

- **Logs first**: ask the agent to listen to the logs (`tv-native.ps1 -Action logs`, or `native/tools/logs_listen.py` in the background) before each test. Without them, it works blind.
- **Renders**: the agent cannot see the screen. `make_preview_kuri.py` (with `en` for English) lets it show you the interface, from the same values as the app.
- **Your data**: the TV's sheet and the logs stay in `.local/` (ignored by git). Do not publish the DUID, the certificates or your IP addresses.

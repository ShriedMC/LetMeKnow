# lmk (LetMeKnow)

A lightweight desktop notification tool for Windows designed for agentic coding tools (Claude Code, Cursor, Windsurf, Antigravity, Aider) and CLI scripts.

It fires native Windows notifications and a floating on-screen card so you get alerted when an agent needs input, credentials, or review.




## Options

```text
Usage:
  lmk <who> <what> [<details>]    Send notification
  lmk --urgent <who> <what>       Send high-priority notification
  lmk --config                    Open configuration wizard
  lmk --test                      Send test notification
  lmk --install                   Install globally (PATH & PowerShell cmdlet)
  lmk --uninstall                 Remove application and settings
```

## Installation

**YOUR AGENT NEEDS TO HAVE FULL ACSESS FOR THE TOOL TO WORK**
1. Download the latest release, or build it your self with the source code.
2. Run **lmk.exe** and follow the steps provided.
   *(If this dosen't work please run install.bat as a secondary measure)*
3. Paste the **PROMPT.md** file into your AI agent and it should install automatically. 
   *(If your agent dosent install it properly refer to the Agent Setup guide)*
4. If when your AI tries to run it and it runs into a error with running scripts, it should do the fallback automatically, but it likely to do with your permissions. Cited on Codex.

This does the following:
- Copies `lmk.exe` to `%LOCALAPPDATA%\LetMeKnow`.
- Adds the folder to your user `PATH`.
- Registers a Start Menu shortcut (`LetMeKnow.lnk`) so Windows Action Center correctly identifies the application.
- Adds the `lmk` cmdlet to your PowerShell profile.

### Configuration

Run `lmk --config` (or launch `lmk.exe` directly) to configure:
- Audio alerts (on / off)
- Urgent chime for errors / blockers
- Toast duration on screen (seconds or persistent)
- Windows Action Center logging

Settings are saved to `%LOCALAPPDATA%\LetMeKnow\config.ini`.

## Uninstallation

If you want to uninstall the program, we're sad to see you go, and we highly recommend leaving feedback in order to help us improve the feature. Run the command:

```bash
lmk --uninstall
```

This removes the binary folder, user PATH entry, PowerShell profile entry, and Start Menu shortcut.

## Agent Setup

To use this tool with **ALMOST** any AI agent (Including but not limited too, Claude Code, Codex, Antigravity, Cursor) If your AI agent can automatically add skills please paste **PROMPT.md** directly into your agent. If it can't please add **AGENTS.md** to your skills manually.

### Examples

```bash
lmk Claude "Need Stripe API key in .env"
lmk Antigravity "Review required for db migration"
lmk Cursor "Build failed" "TS2307: Cannot find module @types/node"
```

For critical blockers or build failures, pass `--urgent` to trigger high-priority styling and alert chime:

```bash
lmk --urgent Claude "Database connection timed out"
```

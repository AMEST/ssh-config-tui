# ssh-config-tui

A terminal UI for managing your SSH config. Organize hosts into groups, edit parameters, and keep your `~/.ssh/config` clean — all from the comfort of your terminal.

> **Notice:** This tool is designed for **Linux** and **macOS** only. Windows is not supported.

## Features

- **Browse and edit** all your SSH hosts in a clean TUI
- **Group hosts** (production, staging, personal) — groups stored as `# tui-group:` comments inside the config file, fully compatible with standard `ssh`; built-in groups: All, Ungrouped
- **Edit global** `Host *` settings
- **Test connection** with `ssh -G`
- **Copy** connection string to clipboard
- **Import/export** host blocks
- **Templates** for quick host creation
- **SSH key browser** — view and select keys in `~/.ssh/`
- **SSH key generator** — generate RSA, ECDSA, or ED25519 key pairs with optional passphrase

## Usage

```bash
ssh-config-tui
```

### Quick keys

| Key     | Action          |
|---------|-----------------|
| Ctrl+S  | Save config     |
| Ctrl+Q  | Quit            |
| Ctrl+N  | Add host        |
| Ctrl+T  | Test connection |
| F5      | Refresh         |
| Enter   | Edit host       |
| Del     | Delete host     |

## Groups

Groups are stored in-band as `# tui-group:` comments inside `~/.ssh/config`. OpenSSH ignores these comments, so your config stays fully compatible. Select a group in the left panel to filter hosts.

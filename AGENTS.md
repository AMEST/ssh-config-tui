# Project Instructions for AI Agents

## Project Overview

**ssh-config-tui** — .NET 9.0 TUI app (Terminal.Gui 2.0) for managing `~/.ssh/config`. Organize hosts into groups, edit parameters, with full roundtrip-preserving SSH config parsing/serialization.

- Language: C# (file-scoped namespaces, nullable enabled, implicit usings)
- Framework: .NET 9.0
- UI: Terminal.Gui 2.0
- DI: Microsoft.Extensions.DependencyInjection 9.0
- Tests: xUnit 2.9
- Packaging: dotnet tool (`dotnet tool install -g ssh-config-tui`), NuGet README at `src/README.md`

## Commands

| Action | Command |
|--------|---------|
| Build | `dotnet build src` |
| Run | `dotnet run --project src` |
| Run (debug logs) | `dotnet run --project src -- --debug` |
| Test | `dotnet test Tests/SshConfigTui.Tests.csproj` |
| Restore | `dotnet restore` |

## Architecture (Clean/Layered)

```
Domain/         — SshConfig, ConfigNode (HostSection, MatchSection, CommentLine, EmptyLine), HostEntry, Group
Infrastructure/ — SshConfigParser (Parse/Serialize), SshConfigRepository (file I/O), DebugLogger
Application/    — ConfigService, GroupService, ClipboardService, SessionService, TemplateService, ApplicationService
UI/             — MainWindow, GroupTreeView, HostListView
UI/Dialogs/     — HostDetailDialog, AddHostDialog, TestConnectionDialog, ImportDialog, ExportDialog,
                  SshKeyPickerDialog, GenerateSshKeyDialog, DialogHelper
Program.cs      — DI wiring, init, Application.Run
```

Tests: `SshConfigParserTests`, `SshConfigTests`, `HostEntryTests`.

## Code Conventions

- **File-scoped namespaces** — `namespace SshConfigTui.Domain;` (no braces)
- **Private fields** — `_camelCase` (e.g., `_parser`, `_log`, `_appService`)
- **Local variables / params** — `camelCase`
- **Public members** — PascalCase
- **Collection properties** — `= new()` initializer (e.g., `List<string> Groups { get; set; } = new()`)
- **String defaults** — `string.Empty` over `null` where possible
- **Constructor injection** — explicit constructors (no primary constructors)
- **Test naming** — `MethodName_Scenario_ExpectedBehavior` or `Should_...`
- **No `.editorconfig`** — rely on default .NET formatting
- **No doc comments** — minimal inline comments

## Key Design Decisions

1. **Groups stored in-band** — `# tui-group: group1, group2` comments inside `~/.ssh/config`. OpenSSH ignores them. The parser always attaches groups to the **next** `Host` section (comments go into `pendingGroups`, never attach to current section).
2. **Parser is roundtrip-safe** — preserves formatting, comments, empty lines, ordering for unmodified sections.
3. **Built-in groups** — `"All"` (all hosts), `"Ungrouped"` (hosts with no `# tui-group:`). These cannot be renamed/deleted.
4. **Session persistence** — last selected group saved to `~/.ssh/config-ui-session.json`.
5. **Backup before write** — `~/.ssh/config.bak` created on save.
6. **Effective config** — `HostEntry.FromHostSection()` maps a `HostSection` to a flat `HostEntry` with typed fields. `ConfigService.GetEffectiveConfig()` merges `Host *` defaults.
7. **AddHost inserts EmptyLine separator** — `ConfigService.AddHost()` adds a blank line before the new host block for readability.
8. **Dialogs expose `Saved`** — dialogs set `bool Saved` property; MainWindow checks it after `Application.Run()` to decide whether to refresh.
9. **Qualified Application.Run** — must be `Terminal.Gui.Application.Run(...)` in files that `using SshConfigTui.Application` (otherwise ambiguous).
10. **ListView.SetSource** — requires `ObservableCollection<T>`, not `List<T>`.
11. **DebugLogger** — constructor takes `bool` (debug mode flag), not a file path.

## Domain Model

```
ConfigNode (abstract)
├── HostSection — Pattern, Directives, LeadingComments, TrailingComments, Groups, StartLine
├── MatchSection — Criteria, Directives, LeadingComments, StartLine
├── CommentLine — Text, LineNumber
└── EmptyLine — LineNumber

SshDirective — Key, Value, LineNumber
HostEntry — Name, HostName, User, Port, IdentityFile, ProxyJump, ForwardAgent, Groups, ExtraDirectives
Group — Name (static helper for built-in groups: IsBuiltInGroup, IsBuiltIn)
SshConfig — Nodes (List<ConfigNode>), GetHosts(), GetHost(), GetGlobalConfig(), GetAllGroups(), GetHostsByGroup()
```

## NuGet Dependencies

- `Terminal.Gui` 2.0.0 — TUI framework
- `Microsoft.Extensions.DependencyInjection` 9.0.4 — DI container
- `CliWrap` 3.8.2 — process execution (ssh -G, ssh-keygen)

## Features (implemented)

- Browse/edit SSH hosts with full roundtrip parsing
- Group hosts via `# tui-group:` comments (All, Ungrouped, custom)
- Global `Host *` settings editor
- Test connection via `ssh -G` (TestConnectionDialog)
- Copy ssh string to clipboard
- Import/export host blocks (ImportDialog, ExportDialog)
- Templates for quick host creation (AddHostDialog)
- SSH key browser (SshKeyPickerDialog — scans `~/.ssh/`, shows only files with `PRIVATE KEY--` in first line)
- SSH key generator (GenerateSshKeyDialog — RSA/ECDSA/ED25519 with optional passphrase via ssh-keygen + CliWrap)
- Backup on save (`~/.ssh/config.bak`)
- Session persistence (last selected group)

## Design Documents

- `docs/design/completed/*.md` — for reference only; do NOT treat as actionable requirements.
- `docs/design/waiting/*.md` — waiting implementation.
- `docs/design/in_progress/*.md` — current implementing document.

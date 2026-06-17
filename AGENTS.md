# Project Instructions for AI Agents

## Project Overview

**ssh-config-tui** — .NET 9.0 TUI app (Terminal.Gui 2.0) for managing `~/.ssh/config`. Organize hosts into groups, edit parameters, with full roundtrip-preserving SSH config parsing/serialization.

- Language: C# (file-scoped namespaces, nullable enabled, implicit usings)
- Framework: .NET 9.0
- UI: Terminal.Gui 2.0
- DI: Microsoft.Extensions.DependencyInjection 9.0
- Tests: xUnit 2.9

## Commands

| Action | Command |
|--------|---------|
| Build | `dotnet build src` |
| Run | `dotnet run --project src` |
| Run (debug logs) | `dotnet run --project src -- --debug` |
| Test | `dotnet test` |
| Restore | `dotnet restore` |

## Architecture (Clean/Layered)

```
Domain/         — SshConfig, ConfigNode (HostSection, MatchSection, CommentLine, EmptyLine), HostEntry, Group
Infrastructure/ — SshConfigParser (Parse/Serialize), SshConfigRepository (file I/O), DebugLogger
Application/    — ConfigService, GroupService, ClipboardService, SessionService, TemplateService, ApplicationService
UI/             — MainWindow, GroupTreeView, HostListView (Terminal.Gui views)
Program.cs      — DI wiring, init, Application.Run
```

Tests mirror the layered structure: `SshConfigParserTests`, `SshConfigTests`, `HostEntryTests`.

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

1. **Groups stored in-band** — `# tui-group: group1, group2` comments inside `~/.ssh/config`. OpenSSH ignores them. The parser attaches groups to the nearest `Host` section (before or after, but not after an empty line).
2. **Parser is roundtrip-safe** — preserves formatting, comments, empty lines, ordering for unmodified sections.
3. **Built-in groups** — `"All"` (all hosts), `"Ungrouped"` (hosts with no `# tui-group:`). These cannot be renamed/deleted.
4. **Session persistence** — last selected group saved to `~/.ssh/config-ui-session.json`.
5. **Backup before write** — `~/.ssh/config.bak` created on save.
6. **Effective config** — `HostEntry.FromHostSection()` maps a `HostSection` to a flat `HostEntry` with typed fields. `ConfigService.GetEffectiveConfig()` merges `Host *` defaults.

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
- `CliWrap` 3.8.2 — process execution (ssh -G)

## Design Documents

- `docs/design/completed/*.md` — for reference only; do NOT treat as actionable requirements.
- `docs/design/waiting/*.md` - waiting implementation.
- `docs/design/in_progress/*.md` - current implementing document.

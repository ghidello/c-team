# C-Team — Production Runtime Requirements

These requirements apply to the production C-Team local companion and deployment model. They are architectural constraints, not requirements for disposable spike utilities.

## Runtime shape

C-Team should ship a small per-user companion executable:

```text
cteam.exe
```

The production runtime must:

- run as the current user;
- not require administrator privileges for normal observability;
- not install or run as a Windows Service;
- not require Python, PowerShell, or shell scripts at runtime;
- not require Codex sandbox escalation or per-operation approval for the normal read-only observation path;
- remain independent from Codex execution so a C-Team failure cannot stop an active Codex mission.

If persistent startup is ever useful, prefer a normal per-user auto-start process over a Windows Service.

## NativeAOT

NativeAOT is a first-class production requirement for the local companion.

Target initially:

```text
.NET 10
win-x64
NativeAOT
single native executable where practical
```

Keep the production dependency set AOT-friendly from the beginning. Avoid reflection-heavy or runtime-code-generation libraries when a straightforward alternative exists.

The production build should keep an equivalent of this green throughout development:

```powershell
dotnet publish -c Release -r win-x64 /p:PublishAot=true
```

Exact project/command paths may change as the production solution is designed.

## Plugin-bundled companion

The preferred deployment experience is:

```text
Install C-Team plugin
        ↓
plugin bundle contains cteam.exe
        ↓
plugin launches bundled cteam.exe in place as the current user
        ↓
cteam.exe exposes the local C-Team/MCP surface
```

Avoid requiring:

- MSI installation;
- PATH modification;
- a globally installed executable;
- administrator elevation;
- a Windows Service.

The plugin should reference the companion by a path relative to its own installation root when the plugin platform supports this cleanly.

## PF1 — Native companion packaging feasibility

Before production architecture is finalized, validate this narrowly and cheaply:

> Can an installed C-Team plugin bundle a platform-native `cteam.exe` and launch it in place as a per-user local companion without PATH installation, elevation, or recurring Codex sandbox approval?

Use a tiny hello-world NativeAOT executable. Do not use the full C-Team observer for this test.

PF1 should establish:

1. whether the plugin package may contain the native executable;
2. how the plugin resolves its own installation root;
3. whether the executable can be referenced with a relative/plugin-root path;
4. whether launch happens outside the Codex task sandbox or otherwise avoids recurring approval prompts;
5. whether Windows marks/download reputation or execution policy introduces friction;
6. how updates replace the bundled executable;
7. whether the same mechanism can support future platform-specific binaries without changing the core plugin contract.

Classify the result:

- **PF1-A — Direct bundle launch works:** use the plugin bundle as the installation mechanism.
- **PF1-B — Bundle is allowed but bootstrap/install is required:** design the smallest per-user bootstrap.
- **PF1-C — Native companion cannot be cleanly delivered by the plugin:** choose a separate per-user installer/distribution mechanism.

Do not build an installer during PF1.

## Development scripts

Scripts created during spikes may remain in `/scripts` or `/tools` for development, reproduction, sanitization, or CI.

They are not production runtime dependencies.

Any capability needed by end users must ultimately be implemented in the native C-Team companion or the plugin/app itself.

## Privilege boundary

Read-only C-Team observability should work entirely at normal user privilege.

Features added later that intentionally change another process or mission state, such as cancellation or steering, may require explicit user consent, but they should still avoid administrator elevation unless the operating system genuinely requires it.

## Lifecycle

Do not commit yet to a permanent daemon.

Preferred initial lifecycle:

```text
plugin/app needs C-Team
        ↓
start bundled cteam.exe if needed
        ↓
serve observation requests / watch missions
        ↓
exit after an appropriate idle period or host shutdown
```

The exact startup/idle protocol is deferred until the production MCP/app design.

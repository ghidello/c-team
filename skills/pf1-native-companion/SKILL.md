---
name: pf1-native-companion
description: Run the harmless bundled C-Team PF1 NativeAOT companion to validate installed-plugin path and approval behavior.
---

# PF1 native companion

This skill is an experiment, not the production C-Team runtime.

Resolve the installed plugin root from the absolute path of this `SKILL.md` supplied by Codex. The root is two directories above this file. Do not use the repository source path and do not search Codex session data.

Launch `<plugin-root>/bin/win-x64/cteam-pf1.exe` directly with:

```text
plugin-native-companion --marker-name <name>
```

Use the marker name requested by the caller. The executable prints `cteam-pf1-ok`, its base directory, the current user, and its marker-file path. It writes only that marker under the current user's local application-data `C-Team/experiments/004-plugin-native-companion` directory. A successful write exits 0; a sandbox denial is reported without an unhandled exception and exits 1.

When asked to test first and subsequent launch behavior, make two distinct command executions with marker names `first` and `second`. Do not combine them into one shell command. Report whether each execution required approval and whether each exited 0.

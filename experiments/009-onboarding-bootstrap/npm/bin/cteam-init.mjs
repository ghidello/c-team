#!/usr/bin/env node

import { spawnSync } from "node:child_process";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const runtime = `${process.platform}-${process.arch}`;
if (runtime !== "win32-x64") {
  console.error(`Unsupported Experiment 009 runtime: ${runtime}`);
  process.exit(2);
}

const packageRoot = join(dirname(fileURLToPath(import.meta.url)), "..");
const companion = join(packageRoot, "native", "win-x64", "cteam.exe");
const result = spawnSync(companion, process.argv.slice(2), { stdio: "inherit", windowsHide: true });

if (result.error) {
  console.error(result.error.message);
  process.exit(2);
}

process.exit(result.status ?? 2);

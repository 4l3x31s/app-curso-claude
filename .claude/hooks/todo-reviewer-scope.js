#!/usr/bin/env node
// PreToolUse hook for the todo-reviewer subagent.
// Blocks any tool call that is not confined to the linked git worktree the agent runs in.
// Fails closed: any unexpected error denies the call.

const { execFileSync } = require("node:child_process");
const fs = require("node:fs");
const path = require("node:path");

const READ_ONLY_GIT =
  /^git\s+(?:-C\s+(?:"([^"]+)"|(\S+))\s+)?(?:worktree\s+list|blame|log|status|diff|show|rev-parse|ls-files|grep)\b/;
const SHELL_METACHARACTERS = /[;&|<>`$\r\n]/;

function deny(reason) {
  process.stderr.write(`todo-reviewer scope: ${reason}\n`);
  process.exit(2);
}

function git(cwd, ...args) {
  return execFileSync("git", ["-C", cwd, ...args], { encoding: "utf8" }).trim();
}

function normalize(target) {
  let resolved = path.resolve(target);
  try {
    resolved = fs.realpathSync.native(resolved);
  } catch {
    // The path may not exist yet; compare the resolved form.
  }
  return process.platform === "win32" ? resolved.toLowerCase() : resolved;
}

function isInside(root, target) {
  const relative = path.relative(root, target);
  return relative === "" || (!relative.startsWith("..") && !path.isAbsolute(relative));
}

function main() {
  const input = JSON.parse(fs.readFileSync(0, "utf8"));
  const cwd = input.cwd;
  const toolInput = input.tool_input ?? {};

  const gitDir = normalize(path.resolve(cwd, git(cwd, "rev-parse", "--git-dir")));
  const commonDir = normalize(path.resolve(cwd, git(cwd, "rev-parse", "--git-common-dir")));
  if (gitDir === commonDir) {
    deny("the agent is running in the main working tree, not in a linked worktree.");
  }
  const worktreeRoot = normalize(git(cwd, "rev-parse", "--show-toplevel"));

  const requireInside = (target) => {
    if (!isInside(worktreeRoot, normalize(path.resolve(cwd, target)))) {
      deny(`'${target}' is outside the worktree ${worktreeRoot}.`);
    }
  };

  switch (input.tool_name) {
    case "Read":
    case "Edit":
      if (!toolInput.file_path) deny("missing file_path.");
      requireInside(toolInput.file_path);
      break;
    case "Grep":
    case "Glob":
      requireInside(toolInput.path ?? cwd);
      break;
    case "Bash": {
      const command = (toolInput.command ?? "").trim();
      const match = READ_ONLY_GIT.exec(command);
      if (!match || SHELL_METACHARACTERS.test(command)) {
        deny("only single read-only git commands are allowed (worktree list, blame, log, status, diff, show, rev-parse, ls-files, grep).");
      }
      requireInside(match[1] ?? match[2] ?? cwd);
      break;
    }
    default:
      deny(`tool '${input.tool_name}' is not allowed for this agent.`);
  }
}

try {
  main();
} catch (error) {
  deny(`could not validate the tool call (${error.message}).`);
}

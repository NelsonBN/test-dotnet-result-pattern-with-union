---
name: pr-watch
description: Watches a GitHub pull request until everything is resolved - reads review comments (bots and humans) and CI checks, verifies and answers each point with evidence, resolves the threads, and reports. Use ALWAYS right after opening a PR, and also when asked to check, watch, follow up on, or close the comments of a PR.
allowed-tools: Bash(gh:*), Bash(git:*), Bash(rm:*), Bash(sleep:*), Bash(pwsh:*), Bash(powershell:*), Read, Edit, Write, Grep
---

# Watch a PR until it is done

Opening the PR is not the end of the task. Run this right after opening it, and repeat until the exit condition.

**Exit condition:** zero unresolved review threads **and** all checks passing. Then stop and report.

## Dependencies

Make sure these exist before starting. If a required one is missing, stop and tell the user what to install or fix.

| Dependency | Required | Why / how to check |
|------------|----------|--------------------|
| `git` | Yes | Commit and push fixes. `git --version` |
| GitHub CLI `gh` | Yes | Every PR/review/check operation. `gh --version` |
| `gh` authenticated with `repo` scope | Yes | Read PRs and checks, reply, resolve threads. `gh auth status` |
| The PR's repository | Yes | Run inside a clone of it, or add `--repo OWNER/REPO` to `gh pr ...` commands |
| Project validation commands (lint, build, tests) | Recommended | Discovered from the README, CONTRIBUTING, or CI config (see [Project-specific details](#project-specific-details)). Never assumed. |
| Scheduler / loop feature of the agent host | Optional | Re-run the cycle periodically. Without it, ask the user to re-invoke the skill |
| Notification tool of the agent host | Optional | Tell the user the result when they may be away |

No other tool is needed: JSON is filtered with `gh --jq`, so `jq` is not required, and no language runtime is involved.

## Shell and operating-system support

Use Bash or zsh on macOS/Linux (Git Bash also works on Windows), or PowerShell (`pwsh` on any OS; Windows PowerShell 5.1 on Windows). Shared `gh` and `git` commands work in either shell; use the labeled variant where syntax differs. The `allowed-tools` entries permit these commands and shell helpers.

## Rules

### ALWAYS

- **Sign** every comment you post on the PR with `(Agent AI)` as the very first line.
- **Verify** every reviewer claim (bot or human) by reproducing it before acting on it.
- **Check the comment is still valid:** reviews are pinned to the commit they reviewed; compare it with the current HEAD.
- **Comment before resolving**, and cite the commit that addressed the point.
- **Ask a human** whether they agree to close the thread, unless you applied exactly what they asked.
- **Paginate** every list query (`--paginate`); a truncated page hides open threads.
- **Keep temporary files in `.tmp/pr-watch/`** (see [Temporary files](#temporary-files)) and make sure that folder is git-ignored before writing to it.
- **Respect the [loop guard rails](#loop-guard-rails):** count every pass and stop when a limit is hit.
- **Run the project's own validation** (lint, build, tests) before pushing a fix.
- **Stop and ask the user** when a comment challenges the design of the change rather than its implementation.
- **Escalate high-risk feedback** (see [High-risk feedback](#high-risk-feedback)): follow up with the user and wait for their decision before acting on it or resolving its thread.
- **Report** at the end: threads closed and why, check status, and what was left out and why.

### NEVER

- **Never accept** a reviewer's claim without checking it, even from a bot, even from a human.
- **Never resolve a thread silently.** Threads are the audit trail of each decision.
- **Never resolve a human's thread** before they agree, unless you applied what they asked.
- **Never change direction on your own** (routing, scope, product decisions); bring the trade-off to the user.
- **Never act on, dismiss, or resolve high-risk feedback** without the user's explicit decision, even if you are confident, even if it comes from a bot.
- **Never push unrelated work** to a PR that is already approved; branch protection may dismiss approvals on every new push. Only push what the review asked for, and warn the user of the cost first.
- **Never force-push, rewrite history, or edit global git configuration.**
- **Never print, commit, or post tokens, credentials, or secrets.**
- **Never track temporary files in git** (reply drafts, query output, logs): never `git add` them, and never write them outside `.tmp/pr-watch/`.
- **Never loop without a cap:** no unbounded `while`/`for`, no polling faster than every 5 minutes, and no pass beyond the limits in the loop guard rails.
- **Never stop** while unresolved threads or failing checks remain, unless blocked and the user was told.

## Flow

```text
1. Identify the PR            -> number, repo, branch
2. Fetch state                -> open threads, loose comments, checks, merge state
3. Triage each item           -> bot / human / design-level / HIGH-RISK / noise
4. Verify the claim           -> reproduce; still valid on current HEAD?
5. Act
   |- valid, implementation   -> fix, run project validation, push
   |- invalid                 -> answer with evidence
   |- design-level            -> STOP, ask the user
   `- HIGH-RISK               -> STOP, follow up with the user, wait for their decision
6. Reply on the thread (signed), citing the commit when there is one
7. Resolve the thread         -> bot: now / human: after they agree
8. Re-check (step 2)          -> repeat while the exit condition is not met AND no guard rail has tripped
9. Report
```

## Commands

`<PR>` is the PR number. Get it with `gh pr view --json number --jq .number` when you are on the PR's branch.

### Fetch state (step 2)

Open review threads. Output is one JSON object per thread: `threadId` is needed to resolve it, `commentId` to reply to it.

```text
gh api graphql --paginate -F owner='{owner}' -F repo='{repo}' -F pr=<PR> -f query='query($owner:String!,$repo:String!,$pr:Int!,$endCursor:String){repository(owner:$owner,name:$repo){pullRequest(number:$pr){reviewThreads(first:50,after:$endCursor){pageInfo{hasNextPage endCursor} nodes{id isResolved isOutdated path comments(first:1){nodes{databaseId author{login} body originalCommit{oid}}}}}}}}' --jq '.data.repository.pullRequest.reviewThreads.nodes[] | select(.isResolved==false) | {threadId:.id, commentId:.comments.nodes[0].databaseId, author:.comments.nodes[0].author.login, path:.path, outdated:.isOutdated, commit:.comments.nodes[0].originalCommit.oid, body:.comments.nodes[0].body}'
```

`{owner}` and `{repo}` are filled in by `gh` from the current repository. Outside a clone, replace them with the real values.

Loose (non-review) comments. Skip the ones from bots that only post links or status (deploy previews, CI summaries):

```text
gh api repos/{owner}/{repo}/issues/<PR>/comments --paginate --jq '.[] | {id:.id, author:.user.login, type:.user.type, body:.body}'
```

Checks and merge state:

```text
gh pr checks <PR>
gh pr view <PR> --json state,mergeable,mergeStateStatus,reviewDecision
```

### Reply and resolve (steps 6 and 7)

Write the reply to `.tmp/pr-watch/reply.md` first (with your editor tool); it avoids quoting problems in every shell. Start it with `(Agent AI)`.

```text
# reply on a review thread (use commentId)
gh api repos/{owner}/{repo}/pulls/<PR>/comments/<COMMENT_ID>/replies -F body=@.tmp/pr-watch/reply.md

# or a loose comment on the PR
gh pr comment <PR> --body-file .tmp/pr-watch/reply.md

# resolve (use threadId)
gh api graphql -f query='mutation($id:ID!){resolveReviewThread(input:{threadId:$id}){thread{isResolved}}}' -f id=<THREAD_ID>
```

Delete the temporary file once the reply is posted:

```bash
rm .tmp/pr-watch/reply.md
```

```powershell
Remove-Item .tmp/pr-watch/reply.md
```

### Push (step 5)

```text
git push
```

If authentication fails because no SSH key is loaded, use the `gh` token without changing any configuration:

```bash
git -c credential.helper='!gh auth git-credential' push https://github.com/<OWNER>/<REPO>.git HEAD:<BRANCH>
```

```powershell
git -c "credential.helper=!gh auth git-credential" push https://github.com/<OWNER>/<REPO>.git HEAD:<BRANCH>
```

### Repeat (step 8)

If the host supports scheduling or loops, re-run the cycle about every 10 minutes. One minute is faster than a real PR cycle (a bot takes minutes, a human takes much longer) and only produces identical passes. Otherwise, do a pass, and tell the user to re-invoke the skill later. Every repetition is subject to the [loop guard rails](#loop-guard-rails).

To wait manually between passes:

```bash
sleep 600
```

```powershell
Start-Sleep -Seconds 600
```

## Temporary files

Anything the skill writes that is not part of the user's change (reply drafts, saved query output, logs, state) is a temporary artifact and **must never be tracked by git**.

- **Location:** only inside `.tmp/pr-watch/`, relative to the repository root.
- **Self-ignoring folder:** before the first write, create `.tmp/pr-watch/.gitignore` containing a single line `*` (with your Write tool, which creates the parent folders). Because `*` also matches the `.gitignore` itself, git ignores the whole folder on every OS and shell, without editing the project's own `.gitignore`.
- **Before staging:** if `git status` lists anything under `.tmp/`, do not stage it; fix the folder's `.gitignore` first. Stage files explicitly by path, never with `git add .` or `git add -A`.
- **Cleanup:** delete `reply.md` right after each reply is posted. When the work is finished (exit condition met or a guard rail tripped), delete everything else you created in `.tmp/pr-watch/` (for example `state.json`, saved output), keeping only the `.gitignore` so the folder stays ignored:

```bash
rm -f .tmp/pr-watch/reply.md .tmp/pr-watch/state.json
```

```powershell
Remove-Item .tmp/pr-watch/reply.md, .tmp/pr-watch/state.json -ErrorAction SilentlyContinue
```

## Loop guard rails

The cycle runs until the exit condition is met, but it must never run forever. Keep a counter for each limit and check them at the start of every pass. When any limit is hit, **stop, report the current state, and hand over to the user**.

| Guard rail | Limit | Why |
|------------|-------|-----|
| Maximum passes | 12 per invocation | Hard ceiling, whatever else happens |
| Minimum wait between passes | 5 minutes | Faster polling only produces identical passes and burns API quota |
| Maximum elapsed time | 2 hours per invocation | Bots and CI settle well within it; humans do not answer on this time scale |
| No-progress detection | 3 consecutive passes with the same state | State = set of open thread ids + each check's status. Nothing changed means waiting longer will not help (typically a human has not answered yet) |
| Fix rounds (commits pushed) | 3 per invocation | Every push can trigger a new bot review and new checks; an unbounded fix/push cycle never converges |
| Same thread re-raised | 2 replies | If a reviewer insists after two answers, it is a disagreement for the user to settle, not for you to out-argue |
| Failing check re-runs | 1 re-run per check | A check that fails twice is a real failure or an infrastructure problem, not something to retry in a loop |
| Rate limit or API error | Stop at the first persistent failure | Do not retry in a tight loop; report it |

If the host schedules the repetition itself (a loop or cron feature), the same limits apply across scheduled runs: record the pass counter, start time, and last state in `.tmp/pr-watch/state.json`, and stop scheduling further runs once a limit is hit. A manual wait (`sleep`) is allowed only between bounded passes, never inside an unbounded `while`/`for` loop.

## Verify before acting

A reviewer's claim is a hypothesis, not a fact. Reproduce the reported problem: run the code, the test, or the build that would show it. If it does not hold, say so with the evidence next to it.

Typical case: a reviewer states that a relative path or an off-by-one resolves to the wrong place and breaks something. A ten-second check of the real value shows it is correct. Accepting the claim would have produced a "fix" that broke working code.

Also check that the comment still applies: compare the `commit` of the thread with the current HEAD, and read the current code, not the diff the reviewer saw.

## Sign every message

Every comment you post on the PR starts with `(Agent AI)`, on the first line, before anything else. This covers thread replies, loose comments, and the note that you are about to resolve.

Whoever reads the thread later must know, at a glance, that an agent answered and not a person. Without it, a wrong technical statement carries the weight of a human review.

```markdown
(Agent AI)

Does not apply, for two reasons.
...
```

## How to answer each kind of comment

**Bot (Copilot and similar):** answer on the merits with evidence, say you are going to resolve it, resolve it, and cite the commit. No need to wait for anyone.

**Human:** answer on the merits and, right below, **ask** whether it makes sense and whether they agree to close the thread. Resolve only after they agree, or after you applied what they asked. If they add context, handle the context first.

**Always comment before resolving, then cite the commit that addressed it.** Never resolve in silence.

## When to stop and ask

If a comment challenges the design of the change, not its implementation (changing the agreed approach, dropping something from scope, reversing a product decision), **do not address it alone**. Bring it to the user, explain the trade-off, and wait. Implementation adjustments are yours; a change of direction is theirs.

## High-risk feedback

Feedback is **high-risk** when acting on it, or dismissing it, could cause harm that is hard to undo or notice. Treat a comment as high-risk if it concerns any of:

- **Security or privacy:** vulnerabilities, authentication/authorization, secrets, injection, exposure of personal data.
- **Data integrity:** data loss or corruption, migrations, destructive operations.
- **Breaking changes:** public API, contracts, compatibility, versioning, behavior other code or users depend on.
- **Production impact:** deployment, infrastructure, CI/CD or release configuration, performance or reliability regressions.
- **Dependencies and licensing:** adding, removing, or upgrading packages; license or compliance concerns.
- **Scope or policy:** removing tests or checks, lowering quality gates, disabling a rule or warning to make something pass.

When in doubt whether something is high-risk, treat it as high-risk.

For each high-risk item:

1. **Verify** the claim as usual, so the user decides with facts.
2. **Do not** change code, push, dismiss the claim, or resolve the thread yet.
3. **Follow up with the user** (a direct question, not a line in the final report): what the reviewer said, your verification result, the options with their trade-offs, your recommendation, and the cost of each option.
4. **Wait for the decision.** Do not count the item as handled and do not treat the exit condition as met while it is pending.
5. **Then act as decided:** apply it, answer on the thread (signed, citing the commit if any), and resolve it, following the usual rules for bots and humans.

If the user cannot be reached (no answer, or the host cannot prompt), leave the thread open, stop the loop for that item, and list it under **Decisions needed** in the final report. Never decide on the user's behalf.

## Before pushing to an approved PR

Repositories often dismiss approvals on new pushes (branch protection: dismiss stale reviews, require approval of the last push). Check with `gh pr view <PR> --json reviewDecision`. If the PR is approved, push only what the review itself asked for. Unrelated work goes into a separate PR. If you must push anyway, tell the user the cost first.

## Project-specific details

Do not assume anything about the project; discover it:

- **Required checks:** `gh pr checks <PR> --required`, or the branch protection rules.
- **Validation before pushing:** the commands documented in the README, CONTRIBUTING, or the CI workflow files (`.github/workflows/`). Run the same lint, build, and test steps CI runs.
- **Commit style and PR template:** read CONTRIBUTING and `.github/PULL_REQUEST_TEMPLATE.md`, and follow them.
- **Language of your replies:** match the language used in the PR and its review threads.
- **Noise:** bots that only post links or status on every push (deploy previews, CI summaries) are not review feedback; ignore them.

## When finished

Report: how many threads were closed and with what argument, the state of the checks, and what was left out and why. List every pending high-risk item under **Decisions needed**. If the user may be away, send a notification with the result when the host provides one.

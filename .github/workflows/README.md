# Workflows

Every pipeline in this directory is **change-routed**: a job runs only when the commit
actually touches something it validates. A frontend-only PR never starts the .NET
toolchain; a `database/schema.sql` edit never starts either app.

## Where the rules live

**`detect-changes.yml` is the single source of truth.** It is a reusable workflow
(`workflow_call`) that classifies every changed file and emits one boolean per area.
`ci.yml` calls it and reads nothing else to decide what to run.

Do not add path logic to `ci.yml`. Add a `case` arm to `detect-changes.yml`.

Every run prints its own routing decision as a table in the run summary, so "why didn't
the backend job run?" is answered by opening the run, not by re-reading this file.

## Routing at a glance

| You changed | What runs |
| --- | --- |
| `src/frontend/**` | Frontend lint/typecheck/test/build → Playwright E2E → Lighthouse |
| `src/backend/**` | Backend build/format/test + EF drift gate |
| `src/backend/**/Migrations/**`, `Infrastructure/Data/**`, `Core/Entities/**` | Backend **and** database checks |
| `database/**` | Database checks only — no .NET build |
| `src/mobile/**` | `mobile-ci.yml` only (filtered at the trigger, so nothing else even starts) |
| `docker-compose.yml`, `.env.example`, `.gitattributes` | Everything (shared runtime contract) |
| `Dockerfile`, `.dockerignore` | Backend + API image |
| `docs/**`, any `*.md`, `.claude/**` | Nothing but the security scan |
| Anything unrecognised | Everything — see "Fail-safe" below |

## Fail-safe

An unclassified path forces every area on. A new top-level directory, a renamed config
file, an unreachable base commit after a force-push, or an empty diff all take this route.

The consequence is that a routing mistake can only ever cost extra minutes — it can never
silently skip a job that was needed. Keep it that way when editing the classification
table: cost optimisation is allowed to be wrong, correctness is not.

## The `CI` check

`ci.yml`'s final job is named `CI`. It always reports, whatever the routing decided, and
treats "skipped" as a pass. **That is the check branch protection should require** — a
per-job requirement would leave a frontend-only PR waiting forever on a backend check that
correctly never ran.

## Cost model

Actions bills the **sum of every job's minutes**, not wall-clock. Two consequences shape
these files:

- Splitting work into parallel jobs improves wall-clock but *costs more*, because each job
  repeats checkout/setup. Parallelise only when the jobs are genuinely independent and
  both would have run anyway.
- Repeating work across jobs is the expensive mistake. The Next.js app is built once in
  `frontend` and passed to `e2e` and `lighthouse` as an artifact; it used to be built three
  times.

## Deploy image reuse

`deploy.yml` does not use `detect-changes.yml` to decide what to build. It tags each image
with `src-<fingerprint>`, derived from the git **tree hash** of the source that goes into
it, and reuses the existing image whenever that fingerprint is already in ACR (a
registry-side manifest copy, no rebuild).

Content beats diffing here: it stays correct across force-pushes, reverts, and skipped
Dependabot merges, none of which a "did the last push touch it?" filter survives.

Build args that come from **secrets** (`SENTRY_DSN`, `VAPID_PUBLIC_KEY`) change no tracked
file, so rotating one does not move the fingerprint. Run the workflow with
`force-rebuild: true` after rotating one.

## Adding a new component

1. Add a `case` arm to `detect-changes.yml` mapping its paths to a new flag.
2. Declare the flag under `workflow_call.outputs` and in the `detect` job's `outputs:`.
3. Add it to the routing table printed to the step summary.
4. Gate the new job on `needs.changes.outputs.<flag>` and add it to the `ci` job's `needs:`.

Step 4 matters: a job missing from `ci`'s `needs:` list can fail without turning the
required check red.

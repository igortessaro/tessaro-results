# Retrospective 003 — NuGet release

**Date:** 2026-11-10 · **Spec:** [003](spec.md) (Implemented 2026-11-10) · **Tasks:** T00–T04, one PR each
**Evidence:** [verification.md](verification.md)

## Outcome

- `Tessaro.Results` is on nuget.org: `0.1.0-rc.1` (2026-11-09) and the first stable `0.1.0`
  (2026-11-10), each from one owner tag and one approval, with symbols, a GitHub Release, the
  README on the package page and a build-provenance attestation. No NuGet API key is stored.
- A plain `dotnet add package Tessaro.Results` installs `0.1.0` in a fresh consumer.
- All 21 acceptance criteria have evidence, the negative paths on the real pipeline included: a tag
  off `main` stops at the guard with every later job skipped (AC-1.3/1.4), a re-run of a published
  version fails with 409 and changes nothing (AC-1.5), and removing a public member without a minor
  bump fails packing with CP0002 against the real `0.1.0` (AC-3.5).
- The pipeline copied by `tessaro-bootstrap` needed **no change**: every failure was configuration
  or timing, not workflow code. That is the evidence spec 005 (a shared release workflow) needs.

## What went well

- **Reusing tessaro-optional's spec and AC IDs** made this spec short and the verification
  mechanical: the same rows, the same kinds of evidence.
- **The rc first** caught the only real setup mistake (the Trusted Publishing owner) on a version
  nobody depends on, and the stable `0.1.0` then ran clean on its first attempt.
- **Failures were safe as designed**: two runs failed at `NuGet login`, before anything was pushed,
  and re-running the failed job after the fix was enough.

## What we would change

| Problem | When it showed up | Change for next time |
|---|---|---|
| The nuget.org Trusted Publishing policy had *Repository Owner* `igor.tessaro` (the nuget.org user) instead of the GitHub owner `igortessaro`; two attempts failed with HTTP 401 "No matching trust policy" | T02 | Applied to `CONTRIBUTING.md` (failure table) and to the `tessaro-bootstrap` setup checklist: the policy's owner is the **GitHub** owner, and the owner reads it back before the first tag |
| The owner reported a tag as created when the push had not happened, so Claude almost waited on a run that didn't exist | T03 | Kept as practice (already in `sdd-task` §10): after any owner step, read the state (`git ls-remote --tags`, `gh run list`) before the next step |
| nuget.org indexes in stages: the page and the package index showed `0.1.0` minutes before the registration index that `dotnet add package` reads, so the first plain install still found only the rc | T02, T03 | Applied to `CONTRIBUTING.md` (*Verify*, with the registration-index check) and to `sdd-task`: wait on the registration index, not the page, before the consumer check |
| The probe tag of AC-1.3 was deleted right after the push, so the run failed at checkout instead of at the guard and had to be repeated | T04 | Applied to `CONTRIBUTING.md` (failure table) and to `sdd-task`: when the owner pushes a tag for a negative-path run, say explicitly to wait until Claude has read the run before deleting it |
| Re-running a published version's `publish` job (AC-1.5) adds one more attestation before the push fails | T04 | Kept as a note: harmless (each attestation is valid for the same digest); the attestation could move after the push in spec 005 |
| The guard's job log is not readable through `gh run view --log` while the run is still in progress, and the jobs API returns escape sequences | T03 | Applied to `sdd-task`: read a job's log with `gh api --allow-escape-sequences …/actions/jobs/<id>/logs` and strip the escapes |

## Follow-ups

- **Side-effect steps** (`Tap`/`OnSuccess`/`OnFailure`, sync and async) — candidate spec 004.
- **A reusable release workflow** shared by `Tessaro.Optional` and `Tessaro.Results` — candidate
  spec 005; this spec shows the copied workflow ran unchanged, so the shared one can start from it.
- Adopt the package in a FIAP-SOAT-Net service (in that repository; spec 001's migration table in
  the README is the guide). `1.0.0` waits for a service running it in production (spec resolved question 1).

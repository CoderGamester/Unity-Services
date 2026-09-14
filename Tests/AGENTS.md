# GameLovers Services Tests — Agent Guide

This guide adds test-only rules to the package and host guides.

## Shared test rules

<!-- BEGIN SHARED TEST RULES -->
### Admission

A new test is admitted only when all six answers are yes:

| Check | Requirement |
|---|---|
| **A1 Defect** | Name the production file/symbol and the incorrect behavior in one sentence. “It could break” is not a defect. |
| **A2 Red** | Name a plausible production edit that should make the assertion fail. Prefer one line/branch; shared-path integration mutations are allowed when isolation is dishonest. |
| **A3 Package-owned** | Every assertion must read behavior this package computes, not C# defaults, fresh-object non-nullness, or Unity guarantees. |
| **A4 Cheapest** | Use the cheapest honest tier: a test case before a new test, EditMode before PlayMode, and an existing fixture before a new fixture. |
| **A5 Unique** | Grep the symbol, derived/wrapper types, and paired setup fields. Do not add a test already reddened by the same narrow defect. |
| **A6 Environment** | Control ambient renderer, Addressables, sample, static, and project state, or branch the expectation on the state actually observed. |

Two additional rejects apply:

- **D1 Tautology:** a lone `DoesNotThrow`, freshly-created non-null assertion, language default, or input-derived substring match pins no package behavior unless used by a named harness sentinel.
- **D2 Name/body mismatch:** deleting or bypassing the behavior promised by the test name must not leave the test green. Strengthen the assertion or rename the test to its actual claim.

Fixtures under `Smoke/` are exempt from A1/A2 and may assert construction/bootstrap viability. The exemption is directory-scoped, not permission to use smoke assertions in Unit or Integration fixtures.

### Revert and Confirm Red (RCR)

Every new or strengthened behavioral test must be observed failing once against a plausible production mutation before commit:

1. Run the new test against normal production code and observe GREEN. The harness prints `baseline green: N test cases`; that line is the fixture count, not a reason to run it again.
2. Preserve the exact working patch or use an isolated worktree; then apply the A2 mutation. Never restore a dirty file from `HEAD`.
3. Run the smallest attributable filter. RED must come from the intended assertion with a diagnostic failure, not a compile error or unrelated `NullReferenceException`.
4. Restore the saved production state, confirm the mutation is gone without losing other edits, and observe GREEN again as one re-run of the same filtered probe.
5. Record the observation on the test using `file + symbol`, never a line number.
6. Correcting an existing `ADMIT`/`RCR` line is itself a claim about production: read the symbol it names, re-run the mutation when the claim is about RED, and run `python3 Tools/rcr-anchor-check.py` before committing.

Use this compact form, targeting four lines and never exceeding six:

```csharp
[Test]
// ADMIT: <owned defect naming production file and symbol>
// RCR: <file> <symbol> — <mutation> → RED (<assertion failure>). <YYYY-MM-DD>
public void Method_Condition_ExpectedResult()
```

Do not narrate investigation history in the test. A nearby mutation that looked valid but stayed green may be recorded when that negative result prevents repeated work.

When a test resists an isolated mutation, classify it before acting:

| Verdict | Meaning | Action |
|---|---|---|
| **A3 reject** | No package production behavior participates. | Delete the test. |
| **A5 duplicate** | The same narrow mutation already belongs to a sibling. | Delete it and name the surviving sibling in review/commit context. |
| **D2 overclaim** | The mutation implied by the name leaves the body green. | Strengthen or rename. |
| **UNFALSIFIABLE** | Real package behavior is double-guarded or cannot be broken by a safe isolated edit. | Keep only after attempted mutations are recorded with the specific reason. |
| **SHARED-PATH** | A broader mutation reddens this legitimate integration path together with siblings. | Keep, recording the observed mutation and blast radius. |

Unannotated tests have three possible histories: observed RED with lost write-back, collateral RED under another test's mutation, or never probed. Check `.test-all/rcr/` before probing and never write prepared annotation text without matching observed evidence. Mutation records stay under `.test-all/rcr/`, not `/tmp`.

Benchmarks use the inverted check: removing the workload from the measured body must materially change the result. Run the actual test assembly; a plain Unity open does not compile assemblies constrained by `UNITY_INCLUDE_TESTS`.
<!-- END SHARED TEST RULES -->

## Placement

- `EditMode/Unit/`: non-MonoBehaviour services and pure logic. `EditMode/Performance/`: benchmarks not requiring frames.
- `PlayMode/Unit/`: services and pools that create GameObjects or depend on Unity callbacks.
- `PlayMode/Integration/`: cross-service, bootstrap, Addressables, resource, and async workflows.
- `PlayMode/Performance/` and `PlayMode/Smoke/`: runtime benchmarks and bootstrap checks only.

## Package conventions

- Tests use namespace `GameLoversEditor.Services.Tests` with the existing namespace-suppression comment.
- EditMode may use NSubstitute. PlayMode uses concrete MonoBehaviour fakes because its asmdef does not reference NSubstitute.
- NSubstitute on Unity Mono cannot proxy self-referentially constrained generic interfaces such as `IObjectPool<IMockEntity>`. Use the real implementation or a hand-written fake.
- Prefer observable behavior or internal seams. The only documented private-reflection exception is the single-field storage assertion in `AssetResolverServiceTest.AddDebugConfigs_StoresAllProvided`; new exceptions require a named harness blocker and a narrow setter-storage assertion.
- Use NUnit classic assertions by default; constraints are appropriate for tolerance/range semantics.

## Cleanup

- Dispose every `TickService` and `CoroutineService`, destroy pool sample GameObjects or dispose them with sample ownership, and call `MainInstaller.Clean()` after bindings.
- Reset static `VersionServices` state in fixtures that change it. Never rely on domain reload between tests.

## Performance harness

- Performance fixtures use `PerformanceTestSetup` and `Measure.Method` with per-iteration state reset.
- `InitializePerformanceTestMetadata()` must prime both `PT_Run` and `PT_Settings`. `PT_Settings` uses `{"MeasurementCount":-1}` so fixture measurement counts remain authoritative.
- Keep the harness sentinel that runs a minimal `Measure.Method`; it detects missing metadata before service benchmarks fail obscurely.

## Verification

- Run EditMode for pure services and PlayMode for hosts, pools, lifecycle, resource loading, and integration behavior.
- Addressables and Editor importer changes require explicit catalog/`AssetDatabase` verification beyond the automated suite.
- Update this guide only when a stable assembly, placement, cleanup, helper, or performance-harness convention changes.

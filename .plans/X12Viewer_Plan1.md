# Plan: X12Viewer Suite — Phase 2 (835) + Phase 3 (276/277)

> Source PRD: docs/X12Viewer_PRD.md

## Architectural decisions

- **Solution name**: `X12Viewer` — all projects renamed from `X271Viewer.*` to `X12Viewer.*`; namespace `woliver13.X12Viewer.*`
- **Project structure**: single shared `X12Viewer.Domain` and `X12Viewer.Application`; transaction logic in sub-namespaces (e.g., `woliver13.X12Viewer.Domain.X835`)
- **Transaction routing**: ST01 element auto-detects transaction type in both CLI and WPF; `--type` flag available as override
- **Code set data**: embedded JSON in Domain project (`CodeTables/X12CodeTables.json`), same pattern as existing 271 code tables; CARC keyed as `CAS02`, RARC keyed as `MOA09`
- **Tree views**: purpose-built per transaction type; shared WPF shell (file open, toolbar, export)
- **Export formats**: CSV (per-service-line) for 835; JSON for all transactions (existing); PDF deferred to backlog
- **Test fixtures**: hand-authored `.edi` files in `tests/X12Viewer.Tests/Fixtures/`

---

## Phase 1: Solution Rename

**User stories**: All subsequent phases depend on this; no user-visible feature.

### What to build

Rename the solution, all five projects, all namespaces, all `using` directives, XAML `x:Class` and `clr-namespace` attributes, and the MSIX `Identity Name` from `X271Viewer` / `woliver13.X271Viewer` to `X12Viewer` / `woliver13.X12Viewer`. Update `<RootNamespace>` in every `.csproj`. The app must build with zero errors and all existing tests must pass after the rename.

### Acceptance criteria

- [ ] `dotnet build` produces zero errors and zero warnings — **AFK**: `dotnet build` exits 0
- [ ] `dotnet test` — all existing tests pass — **AFK**: `dotnet test` exits 0
- [ ] No remaining occurrences of `X271Viewer` in any hand-authored `.cs`, `.csproj`, `.xaml`, `.xml`, or `.sh` file — **AFK**: `grep -r "X271Viewer" src tests installer` returns empty
- [ ] MSIX `installer/AppxManifest.xml` `Identity Name` updated to `woliver13.X12Viewer` — **AFK**: grep confirms
- [ ] `installer/build-msix.sh` completes with `EXIT: 0` — **AFK**: script exits 0

---

## Phase 2: 835 Parser and Domain Model

**User stories**: Biller opens an 835 file; the app recognises it as an 835 and does not crash or show a 271-style error.

### What to build

Implement an `X835DocumentParser` in `X12Viewer.Domain` that reads an 835 file and produces a structured document model: envelope (ISA/GS/ST), one `X835Claim` per CLP segment (with Claim ID, patient name, billed, paid, claim status code), one `X835ServiceLine` per SVC segment per claim (proc code, billed, paid), and one `X835Adjustment` per CAS segment per service line (group code, CARC, amount). Wire ST01 auto-detection so the CLI `parse` command routes 835 files to this parser and emits JSON.

Author a representative 835 test fixture (`tests835.edi`) covering four claim scenarios: fully paid, partially paid with CARC adjustment, denied, and a claim with a RARC remark (MOA segment).

### Acceptance criteria

- [ ] `X835DocumentParser.ParseFile` returns a document with correct CLP count for the fixture — **AFK**: unit test asserts claim count
- [ ] Each `X835Claim` exposes ClaimId, PatientName, BilledAmount, PaidAmount, ClaimStatusCode — **AFK**: unit test asserts field values against fixture
- [ ] Each `X835ServiceLine` exposes ProcedureCode, BilledAmount, PaidAmount — **AFK**: unit test asserts against fixture
- [ ] Each `X835Adjustment` exposes GroupCode, ReasonCode (CARC), AdjustmentAmount — **AFK**: unit test asserts against fixture
- [ ] RARC remark from MOA segment is captured on the claim — **AFK**: unit test asserts RARC present on the denied-with-remark claim
- [ ] CLI `parse` auto-detects ST01=835 and routes to `X835DocumentParser` — **AFK**: `CliRunnerTests` asserts exit 0 and JSON output for 835 fixture
- [ ] CLI `parse` on a 271 file still routes to the 271 parser — **AFK**: existing `CliRunnerTests` pass unchanged
- [ ] Parsing an invalid/non-835 file returns a human-readable `Error:` on stderr — **AFK**: `CliRunnerTests` asserts `stderr.StartsWith("Error:")`

---

## Phase 3: 835 CARC/RARC Interpretation

**User stories**: Biller sees plain-English explanation of why a claim was adjusted or denied.

### What to build

Extend `X12CodeTables.json` with CARC codes (keyed `CAS02`) and RARC codes (keyed `MOA09`). Implement an `X835Interpreter` in `X12Viewer.Application` that enriches each `X835Adjustment` with a `ReasonDescription` and each claim's RARC with a `RemarkDescription`. Wire the CLI `interpret` command to route 835 files through the interpreter and emit the enriched JSON.

### Acceptance criteria

- [ ] `X12CodeTable.Resolve("CAS02", "45")` returns `"Charge exceeds fee schedule/maximum allowable"` — **AFK**: unit test
- [ ] `X12CodeTable.Resolve("CAS02", "4")` returns `"The service/equipment/drug is not covered by the plan"` — **AFK**: unit test
- [ ] `X12CodeTable.Resolve("MOA09", "MA01")` returns a non-empty plain-English string — **AFK**: unit test
- [ ] Unknown CARC falls back to `"<code> (unrecognized code)"` — **AFK**: unit test (mirrors existing 271 pattern)
- [ ] `X835Interpreter.Interpret` returns an enriched document where each adjustment's `ReasonDescription` is non-empty — **AFK**: unit test against fixture
- [ ] CLI `interpret` on an 835 fixture emits JSON with `ReasonDescription` populated — **AFK**: `CliRunnerTests` asserts property present and non-empty
- [ ] CLI `interpret` on a 271 file still works — **AFK**: existing tests pass

---

## Phase 4: 835 Tree View (WPF)

**User stories**: Biller opens an 835 in the WPF app and sees a claim-level tree with plain-English outcomes; EDI analyst can expand to segment detail.

### What to build

Implement `X835TreeBuilder` in `X12Viewer.Application` that produces an `X12Node` tree for 835 documents following the biller-first layout: root → GS payment summary → ST transaction → CLP claim nodes (showing patient name, billed→paid, status in plain English) → SVC service line nodes → CAS adjustment nodes (showing CARC description and adjustment amount). Wire the WPF file-open handler to detect ST01=835 and render the 835 tree in the existing tree view panel. The 271 tree view must continue to work unchanged.

### Acceptance criteria

- [ ] `X835TreeBuilder.Build` produces a root node whose children correspond to GS groups in the fixture — **AFK**: unit test asserts GS count
- [ ] Each CLP claim node label contains the patient name and paid amount — **AFK**: unit test asserts label content
- [ ] Each CAS adjustment node label contains the CARC description — **AFK**: unit test asserts label contains plain-English text
- [ ] RARC remark node appears as a child of the claim node when MOA is present — **AFK**: unit test asserts node present
- [ ] Opening an 835 file in WPF renders the claim tree without error — **HITL**: open `tests835.edi` in the app; confirm tree renders with at least one claim node visible
- [ ] Opening a 271 file in WPF after opening an 835 renders the 271 tree correctly — **HITL**: switch files; confirm 271 tree renders without residual 835 state
- [ ] Fully paid claim node is visually distinct from denied claim node (e.g., label text differs) — **HITL**: confirm "Paid" vs "Denied" status visible in tree

---

## Phase 5: 835 CSV Export

**User stories**: Biller exports an 835 to a spreadsheet for payment reconciliation.

### What to build

Implement `X835CsvExporter` in `X12Viewer.Application` that serialises an interpreted 835 document to CSV with one row per `X835ServiceLine`. Columns: Payer, ClaimId, PatientName, TotalBilled, TotalPaid, ClaimStatus, ProcedureCode, SvcBilled, SvcPaid, CARCCode, CARCDescription, AdjustmentAmount. Parent CLP fields repeat on each SVC row. Wire a "Export CSV" button in the WPF toolbar (visible and enabled when an 835 is loaded; hidden or disabled for 271). Wire the CLI `export` command (or a new `--format csv` flag) for 835 files.

### Acceptance criteria

- [ ] `X835CsvExporter.Export` produces a string with a header row and one data row per SVC segment in the fixture — **AFK**: unit test asserts row count equals SVC count + 1
- [ ] Each data row contains the parent CLP's ClaimId and PatientName — **AFK**: unit test asserts fields present
- [ ] The CARC description column is populated from the interpreted document — **AFK**: unit test asserts non-empty
- [ ] A denied claim with no SVC produces one row with zero paid amount — **AFK**: unit test asserts
- [ ] CLI `export` (or `interpret --format csv`) on an 835 file writes valid CSV to stdout — **AFK**: `CliRunnerTests` asserts row count and header
- [ ] Exported CSV opens in Excel without errors — **HITL**: open exported file in Excel; confirm column headers, data rows, no garbled characters
- [ ] WPF "Export CSV" button is enabled when 835 is loaded and disabled when 271 is loaded — **HITL**: verify button state changes on file switch

---

## Phase 6: 835 5010 Validation

**User stories**: EDI analyst identifies structural errors in an 835 before submitting it to a payer or posting it.

### What to build

Implement `X835Validator` in `X12Viewer.Application` that checks the 005010X221A1 structural rules: required segments present (BPR, TRN, DTM*097), CLP03/CLP04 are numeric, CAS segments have valid group codes (CO, OA, PI, PR), SVC line counts match CLP10. Surface validation results in the existing validation panel in WPF and via CLI `validate` command.

### Acceptance criteria

- [ ] `X835Validator.Validate` returns zero results for a well-formed 835 fixture — **AFK**: unit test
- [ ] Missing BPR segment produces a validation result with a human-readable message — **AFK**: unit test with a hand-trimmed fixture
- [ ] Invalid CAS group code produces a validation result — **AFK**: unit test with a hand-trimmed fixture
- [ ] Non-numeric CLP03 (billed amount) produces a validation result — **AFK**: unit test
- [ ] CLI `validate` on the 835 fixture exits 0 and emits no errors — **AFK**: `CliRunnerTests` asserts exit 0
- [ ] CLI `validate` on a malformed 835 exits non-zero and emits human-readable errors — **AFK**: `CliRunnerTests` asserts exit != 0 and `stderr.StartsWith("Error:")`  
- [ ] Validation results appear in the WPF validation panel when an 835 is loaded — **HITL**: open fixture; confirm panel shows "No issues found"

---

## Phase 7: 276/277 Claim Status Viewer

**User stories**: Biller checks claim status before the 835 remittance arrives; sees plain-English status (accepted, pending, rejected, additional information requested).

### What to build

Implement `X276DocumentParser` and `X277DocumentParser` in `X12Viewer.Domain`. Implement `X277Interpreter` in `X12Viewer.Application` that translates STC status category/status/action codes to plain English via `X12CodeTables.json` entries keyed `STC01-1`, `STC01-2`, `STC01-3`. Implement `X277TreeBuilder` producing a claim-per-node tree with plain-English status. Wire auto-detection for ST01=276 and ST01=277 in CLI and WPF. Author fixtures for the four status scenarios: accepted, pending, rejected, additional-info-requested.

### Acceptance criteria

- [ ] `X277DocumentParser.ParseFile` returns claims with ClaimId and STC status codes from the fixture — **AFK**: unit test
- [ ] `X277Interpreter.Interpret` resolves `STC01-1` category code `F1` to `"Finalized"` (or equivalent plain-English) — **AFK**: unit test
- [ ] All four fixture scenarios produce non-empty plain-English status descriptions — **AFK**: unit tests (one per scenario)
- [ ] CLI `parse` auto-detects ST01=277 and routes correctly — **AFK**: `CliRunnerTests` asserts JSON output
- [ ] CLI `interpret` on a 277 fixture emits JSON with plain-English status descriptions — **AFK**: `CliRunnerTests`
- [ ] Existing 271 and 835 CLI tests pass unchanged — **AFK**: full test suite
- [ ] Opening a 277 file in WPF renders the claim status tree — **HITL**: open fixture; confirm at least one claim node visible with plain-English status
- [ ] Pending and rejected claim nodes are visually distinguishable from accepted — **HITL**: confirm label text differs across the four fixture scenarios

---

## HITL summary

| Phase | HITL criteria | Total criteria |
|---|---|---|
| 1 — Rename | 0 | 5 |
| 2 — 835 Parser | 0 | 8 |
| 3 — CARC/RARC Interpretation | 0 | 7 |
| 4 — 835 Tree View | 3 | 7 |
| 5 — 835 CSV Export | 2 | 7 |
| 6 — 835 Validation | 1 | 7 |
| 7 — 276/277 Viewer | 2 | 8 |
| **Total** | **8** | **49** |

All HITL criteria are WPF rendering or Excel interop — they cannot be asserted without a running UI.

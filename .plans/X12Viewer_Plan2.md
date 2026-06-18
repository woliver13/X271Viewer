# Plan: X12Viewer Suite — Phases 8–11 (270, 837P, 837I, 999)

> Source PRD: https://github.com/woliver13/X271Viewer/issues/43

## Architectural decisions

- **Feature branch**: `feature/270-837-999`; phase branches merge into it, feature merges into `develop`
- **Transaction routing**: ST01 auto-detects transaction type in CLI and WPF; 837 subtype determined by GS08 substring (`X222` → 837P, `X223` → 837I)
- **File open dialog**: label updated to "Open X12 EDI File" — generic, future-proof
- **Domain models**: immutable records with public properties and no methods; one parser + model set per transaction type
- **Subscriber/patient**: both 270 and 837 models carry `SubscriberId`, `SubscriberName`, and `PatientName` as distinct fields; `PatientName` resolves from the dependent loop when present, otherwise from the subscriber loop
- **837 shared structure**: 837I domain model composes or extends 837P where loops are shared; SV2 replaces SV1, CL1 added to claim loop
- **Code tables**: all new lookup tables added to existing embedded `X12CodeTables.json`; resolved via `X12CodeTable.Resolve(tableName, code)` with `"{code} (unrecognized code)"` fallback
- **ICD-10-CM**: FY2026 full description set (~70K codes) embedded as a separate JSON resource; sourced from CMS
- **HCPCS Level II**: CMS-published codes embedded as a separate JSON resource; CPT codes displayed as-is with no description
- **Export**: CSV with one row per service line for 837P and 837I only; 270 and 999 are viewer-only
- **Validation**: light structural validation for 270 and 837P/I (required loops/segments); no validator for 999
- **Tree structure**: all transaction types use the existing `X271Node` tree; `X271Node.IsCollapsedByDefault` used where appropriate
- **Test fixtures**: hand-authored `.edi` files in `tests/X12Viewer.Tests/Fixtures/`; one file per transaction type covering happy path and acceptance-criteria edge cases

---

## Phase 8: 270 Eligibility Inquiry Viewer

**User stories**: 1, 2, 3, 4, 5, 20, 31, 32

### What to build

Implement an `X270DocumentParser` in `X12Viewer.Domain` that reads a 270 file and produces a structured document model: one `X270Subscriber` per 2000B loop (subscriber name, subscriber ID), an optional `X270Dependent` per 2000C loop (patient name, relationship), and one `X270ServiceTypeQuery` per EQ segment under each subscriber/dependent (service type code). Implement an `X270Interpreter` in `X12Viewer.Application` that resolves EQ service type codes to plain-English descriptions using the existing EB01 code table. Implement an `X270TreeBuilder` producing a tree: root → subscriber nodes → dependent node (when present) → EQ service type query nodes. Implement an `X270Validator` that checks the subscriber loop is present and at least one EQ segment exists. Wire auto-detection for ST01=270 in CLI and WPF. Update the WPF file open dialog label to "Open X12 EDI File". Author a fixture (`tests270.edi`) with two subscriber scenarios: one subscriber-only and one with a dependent present, each with multiple EQ service types.

### Acceptance criteria

- [ ] `X270DocumentParser.ParseFile` returns the correct subscriber count from the fixture — **AFK**: unit test asserts subscriber count
- [ ] Each `X270Subscriber` exposes `SubscriberId` and `SubscriberName` — **AFK**: unit test asserts field values against fixture
- [ ] Dependent loop populates `X270Dependent` with `PatientName` when present — **AFK**: unit test asserts dependent name on subscriber-with-dependent scenario
- [ ] Each `X270ServiceTypeQuery` exposes the raw EQ service type code — **AFK**: unit test asserts code values
- [ ] `X270Interpreter.Interpret` resolves EQ service type code `30` to a non-empty plain-English description — **AFK**: unit test
- [ ] Unknown EQ code falls back to `"{code} (unrecognized code)"` — **AFK**: unit test
- [ ] `X270Validator.Validate` returns zero errors for a well-formed fixture — **AFK**: unit test
- [ ] Missing subscriber loop produces a validation error — **AFK**: unit test with trimmed fixture
- [ ] No EQ segments produces a validation error — **AFK**: unit test with trimmed fixture
- [ ] `X270TreeBuilder.Build` produces subscriber nodes as children of root — **AFK**: unit test asserts subscriber node count
- [ ] Dependent node appears as child of subscriber node when dependent is present — **AFK**: unit test asserts dependent node present
- [ ] EQ service type nodes appear with plain-English description labels — **AFK**: unit test asserts label content
- [ ] CLI `parse` auto-detects ST01=270 and routes to `X270DocumentParser` — **AFK**: `CliRunnerTests` asserts exit 0 and JSON output
- [ ] CLI `interpret` on a 270 fixture emits JSON with plain-English service type descriptions — **AFK**: `CliRunnerTests` asserts property present and non-empty
- [ ] Existing 271, 835, and 277 CLI tests pass unchanged — **AFK**: full test suite
- [ ] Opening a 270 file in WPF renders the subscriber/EQ tree without error — **HITL**: open `tests270.edi`; confirm subscriber and EQ nodes visible
- [ ] Dependent node is visible when dependent present in fixture — **HITL**: confirm dependent name appears in tree
- [ ] WPF file open dialog label reads "Open X12 EDI File" — **HITL**: confirm updated label in dialog

---

## Phase 9: 837P Professional Claim Viewer

**User stories**: 6, 8, 9, 11, 12, 13, 14, 15, 16, 21, 23, 25, 31, 32

### What to build

Implement an `X837PDocumentParser` in `X12Viewer.Domain` that reads an 837P file and produces a structured document: `X837BillingProvider` (NPI, name) from the 2000A/2010AA loop, `X837Subscriber` (ID, name) from the 2000B/2010BA loop, optional `X837Patient` (name) from the 2000C/2010CA loop, one `X837Claim` per 2300 loop (CLM01 claim ID, place of service, total billed amount, diagnosis codes from HI loop), and one `X837PServiceLine` per 2400 loop (SV1 procedure code, modifier, units, billed amount). Detect GS08 substring `X222` to confirm 837P subtype.

Add ICD-10-CM FY2026 and HCPCS Level II code tables as embedded JSON resources. Implement an `X837PInterpreter` that enriches each claim's diagnosis codes with ICD-10 descriptions and each service line's procedure code with a HCPCS description (CPT codes displayed as-is). Implement an `X837PTreeBuilder` producing the hierarchy: root → billing provider → subscriber → patient (when dependent) → claim nodes (CLM01, total billed) → diagnosis nodes → SV1 service line nodes. Implement an `X837PValidator` checking CLM01 present, billing provider NPI present, at least one HI diagnosis, and at least one SV1 service line. Implement an `X837PCsvExporter` with columns: BillingProviderNPI, BillingProviderName, SubscriberID, PatientName, ClaimID, PlaceOfService, DiagnosisCodes, ProcedureCode, Modifier, Units, BilledAmount. Wire auto-detection for ST01=837 + GS08 `X222` in CLI and WPF. Enable CSV export menu item when 837P is loaded. Author a fixture (`tests837p.edi`) with two claims: one fully specified with a dependent patient and multiple diagnosis/service line combinations, one minimal with subscriber as patient.

### Acceptance criteria

- [ ] `X837PDocumentParser.ParseFile` returns the correct claim count from the fixture — **AFK**: unit test
- [ ] `X837BillingProvider` exposes NPI and Name — **AFK**: unit test asserts values against fixture
- [ ] `X837Subscriber` exposes SubscriberId and SubscriberName — **AFK**: unit test
- [ ] Dependent 2000C loop populates `X837Patient` with PatientName distinct from subscriber — **AFK**: unit test on dependent-present scenario
- [ ] Each `X837Claim` exposes ClaimId, PlaceOfService, BilledAmount, and at least one diagnosis code — **AFK**: unit test
- [ ] Each `X837PServiceLine` exposes ProcedureCode, Modifier, Units, BilledAmount — **AFK**: unit test
- [ ] `X12CodeTable.Resolve` for a known ICD-10-CM code returns a non-empty description — **AFK**: unit test
- [ ] `X12CodeTable.Resolve` for a known HCPCS Level II code returns a non-empty description — **AFK**: unit test
- [ ] Unknown ICD-10 code falls back to `"{code} (unrecognized code)"` — **AFK**: unit test
- [ ] `X837PInterpreter.Interpret` populates diagnosis descriptions on each claim — **AFK**: unit test asserts non-empty descriptions
- [ ] CPT procedure code is passed through as-is without a description lookup — **AFK**: unit test asserts no description field populated for CPT code range
- [ ] `X837PValidator.Validate` returns zero errors for the well-formed fixture — **AFK**: unit test
- [ ] Missing CLM01 produces a validation error — **AFK**: unit test
- [ ] Missing billing provider NPI produces a validation error — **AFK**: unit test
- [ ] Missing HI diagnosis segment produces a validation error — **AFK**: unit test
- [ ] Missing SV1 service line produces a validation error — **AFK**: unit test
- [ ] `X837PTreeBuilder.Build` produces billing provider → subscriber → claim hierarchy — **AFK**: unit test asserts node structure
- [ ] Patient node appears as child of subscriber when dependent present — **AFK**: unit test
- [ ] Claim node label contains ClaimId and total billed amount — **AFK**: unit test asserts label content
- [ ] Diagnosis node label contains ICD-10 code and description — **AFK**: unit test asserts label content
- [ ] SV1 service line node label contains procedure code and billed amount — **AFK**: unit test asserts label content
- [ ] `X837PCsvExporter.Export` produces header row plus one data row per SV1 service line — **AFK**: unit test asserts row count
- [ ] CSV rows contain correct BillingProviderNPI, SubscriberID, PatientName, and ProcedureCode values — **AFK**: unit test asserts field values
- [ ] DiagnosisCodes column contains all diagnosis codes for the claim (semicolon-delimited) — **AFK**: unit test
- [ ] CLI `parse` auto-detects ST01=837 + GS08 X222 and routes to `X837PDocumentParser` — **AFK**: `CliRunnerTests` asserts exit 0 and JSON output
- [ ] CLI `interpret` on an 837P fixture emits JSON with ICD-10 descriptions populated — **AFK**: `CliRunnerTests`
- [ ] Existing 271, 835, 277, and 270 CLI tests pass unchanged — **AFK**: full test suite
- [ ] Opening an 837P file in WPF renders the full billing provider → claim → service line tree — **HITL**: open `tests837p.edi`; confirm tree hierarchy visible
- [ ] Selecting a claim node shows plain-English summary in the interpretation pane — **HITL**: click a claim node; confirm patient, provider, billed amount, and diagnoses visible
- [ ] Selecting a service line node shows plain-English service line detail — **HITL**: click an SV1 node; confirm procedure, units, and billed amount visible
- [ ] WPF CSV export menu item is enabled when 837P is loaded — **HITL**: confirm menu item state
- [ ] Exported 837P CSV opens in Excel with correct columns and one row per service line — **HITL**: export and open in Excel

---

## Phase 10: 837I Institutional Claim Viewer

**User stories**: 7, 8, 10, 17, 18, 19, 22, 24, 26, 31, 32

### What to build

Implement an `X837IDocumentParser` in `X12Viewer.Domain` that reads an 837I file. It shares the billing provider, subscriber, and patient loop structure with 837P. Differences: SV2 (revenue code, procedure code, units, billed amount) replaces SV1 in the 2400 loop; CL1 (admission type, admission source, patient status) added to the 2300 claim loop; additional HI loop variants carry occurrence codes, value codes, and condition codes. Detect GS08 substring `X223` to confirm 837I subtype.

Add UB revenue codes, CL1 admission type/source/status codes, and occurrence/value/condition codes to `X12CodeTables.json`. Implement an `X837IInterpreter` enriching CL1 codes, revenue codes, and occurrence/value/condition codes with plain-English descriptions (ICD-10 diagnosis descriptions reuse the table added in Phase 9). Implement an `X837ITreeBuilder` following the same hierarchy as 837P with SV2 nodes instead of SV1 and a CL1 node under each claim. Implement an `X837IValidator` checking CLM01 present, billing provider NPI present, CL1 present, at least one HI diagnosis, and at least one SV2 service line. Implement an `X837ICsvExporter` with columns: BillingProviderNPI, BillingProviderName, SubscriberID, PatientName, ClaimID, AdmissionType, AdmissionSource, PatientStatus, RevenueCode, RevenueDescription, ProcedureCode, Units, BilledAmount. Wire auto-detection for ST01=837 + GS08 `X223` in CLI and WPF. Author a fixture (`tests837i.edi`) with two claims: one fully specified with CL1, dependent patient, occurrence/value/condition codes; one minimal.

### Acceptance criteria

- [ ] `X837IDocumentParser.ParseFile` returns the correct claim count from the fixture — **AFK**: unit test
- [ ] Each `X837Claim` exposes CL1 fields: AdmissionType, AdmissionSource, PatientStatus — **AFK**: unit test asserts values
- [ ] Each `X837IServiceLine` exposes RevenueCode, ProcedureCode, Units, BilledAmount — **AFK**: unit test
- [ ] Occurrence codes are captured on the claim when present — **AFK**: unit test
- [ ] Value codes are captured on the claim when present — **AFK**: unit test
- [ ] Condition codes are captured on the claim when present — **AFK**: unit test
- [ ] `X12CodeTable.Resolve` for a known UB revenue code returns a non-empty description — **AFK**: unit test
- [ ] `X12CodeTable.Resolve` for a known CL1 admission type code returns a non-empty description — **AFK**: unit test
- [ ] `X12CodeTable.Resolve` for a known occurrence code returns a non-empty description — **AFK**: unit test
- [ ] `X837IInterpreter.Interpret` populates CL1 descriptions and revenue code descriptions on each claim/service line — **AFK**: unit test
- [ ] `X837IValidator.Validate` returns zero errors for the well-formed fixture — **AFK**: unit test
- [ ] Missing CL1 segment produces a validation error — **AFK**: unit test
- [ ] Missing SV2 service line produces a validation error — **AFK**: unit test
- [ ] `X837ITreeBuilder.Build` produces billing provider → subscriber → claim hierarchy with CL1 node under claim — **AFK**: unit test asserts node structure
- [ ] SV2 service line node label contains revenue code description and billed amount — **AFK**: unit test asserts label content
- [ ] `X837ICsvExporter.Export` produces header row plus one data row per SV2 service line — **AFK**: unit test asserts row count
- [ ] CSV rows contain RevenueCode, RevenueDescription, AdmissionType, AdmissionSource, PatientStatus — **AFK**: unit test asserts field values
- [ ] CLI `parse` auto-detects ST01=837 + GS08 X223 and routes to `X837IDocumentParser` — **AFK**: `CliRunnerTests` asserts exit 0 and JSON output
- [ ] CLI `interpret` on an 837I fixture emits JSON with CL1 and revenue code descriptions — **AFK**: `CliRunnerTests`
- [ ] 837P detection (GS08 X222) still routes correctly alongside 837I detection — **AFK**: `CliRunnerTests` asserts both subtypes route correctly
- [ ] Existing all prior CLI tests pass unchanged — **AFK**: full test suite
- [ ] Opening an 837I file in WPF renders the institutional claim tree with CL1 node visible — **HITL**: open `tests837i.edi`; confirm CL1 node and SV2 service lines visible
- [ ] Selecting a claim node shows CL1 admission detail in the interpretation pane — **HITL**: click claim node; confirm admission type, source, and patient status visible in plain English
- [ ] Exported 837I CSV opens in Excel with correct columns including RevenueCode and RevenueDescription — **HITL**: export and open in Excel

---

## Phase 11: 999 Implementation Acknowledgment Viewer

**User stories**: 27, 28, 29, 30, 31, 32

### What to build

Implement an `X999DocumentParser` in `X12Viewer.Domain` that reads a 999 file and produces a structured document: one `X999FunctionalGroup` per AK1 segment (transaction set identifier, group control number), one `X999TransactionSet` per AK2 segment (transaction set ID, control number, acceptance status from AK5/AK9), and one `X999SegmentError` per IK3 segment (segment ID, position, error code) with one `X999ElementError` per IK4 segment (element position, error code). Add 999 AK/IK error codes to `X12CodeTables.json`. Implement an `X999Interpreter` that resolves IK3 and IK4 error codes to plain-English descriptions. Implement an `X999TreeBuilder` producing the hierarchy: root → AK1 functional group → AK2 transaction set nodes (label includes accepted/rejected status icon A/R) → IK3 segment error nodes → IK4 element error nodes. Wire auto-detection for ST01=999 in CLI and WPF. Author a fixture (`tests999.edi`) with one accepted transaction set and one rejected transaction set containing IK3 and IK4 errors.

### Acceptance criteria

- [ ] `X999DocumentParser.ParseFile` returns the correct functional group count from the fixture — **AFK**: unit test
- [ ] Each `X999TransactionSet` exposes TransactionSetId, ControlNumber, and AcceptanceStatus (Accepted/Rejected) — **AFK**: unit test asserts values
- [ ] Accepted transaction set has no child error nodes in the domain model — **AFK**: unit test
- [ ] Rejected transaction set exposes at least one `X999SegmentError` with SegmentId and ErrorCode — **AFK**: unit test
- [ ] Each `X999SegmentError` exposes child `X999ElementError` records when IK4 segments are present — **AFK**: unit test
- [ ] `X12CodeTable.Resolve` for a known IK3 error code returns a non-empty plain-English description — **AFK**: unit test
- [ ] `X12CodeTable.Resolve` for a known IK4 error code returns a non-empty plain-English description — **AFK**: unit test
- [ ] Unknown error code falls back to `"{code} (unrecognized code)"` — **AFK**: unit test
- [ ] `X999Interpreter.Interpret` populates error descriptions on all IK3 and IK4 nodes — **AFK**: unit test
- [ ] `X999TreeBuilder.Build` produces functional group → transaction set hierarchy — **AFK**: unit test asserts node structure
- [ ] Accepted transaction set node label contains an acceptance indicator — **AFK**: unit test asserts label content
- [ ] Rejected transaction set node label contains a rejection indicator — **AFK**: unit test asserts label content
- [ ] IK3 segment error node label contains segment ID and plain-English error description — **AFK**: unit test asserts label content
- [ ] IK4 element error node label contains element position and plain-English error description — **AFK**: unit test asserts label content
- [ ] CLI `parse` auto-detects ST01=999 and routes to `X999DocumentParser` — **AFK**: `CliRunnerTests` asserts exit 0 and JSON output
- [ ] CLI `interpret` on a 999 fixture emits JSON with error descriptions populated — **AFK**: `CliRunnerTests`
- [ ] All prior CLI tests pass unchanged — **AFK**: full test suite
- [ ] Opening a 999 file in WPF renders the functional group → transaction set tree — **HITL**: open `tests999.edi`; confirm AK1 and AK2 nodes visible
- [ ] Accepted and rejected transaction set nodes are visually distinguishable in the tree — **HITL**: confirm accepted vs rejected label indicators visible
- [ ] Selecting a rejected transaction set node shows IK3/IK4 error detail in the interpretation pane — **HITL**: click rejected node; confirm error descriptions visible in plain English

---

## HITL summary

| Phase | HITL criteria | AFK criteria | Total |
|---|---|---|---|
| 8 — 270 Eligibility Inquiry | 3 | 15 | 18 |
| 9 — 837P Professional Claim | 5 | 26 | 31 |
| 10 — 837I Institutional Claim | 3 | 20 | 23 |
| 11 — 999 Acknowledgment | 3 | 16 | 19 |
| **Total** | **14** | **77** | **91** |

All HITL criteria require a running WPF UI or Excel interop — they cannot be asserted programmatically.

# X12Viewer Suite — Product Requirements Document

**Version:** 1.0  
**Date:** June 16, 2026  
**Status:** Draft  
**Author:** Bill Oliver  

---

## 1. Product Overview

X12Viewer is a Windows desktop application (WPF, MSIX distribution) that transforms raw HIPAA X12 EDI files into human-readable views for healthcare billing and EDI operations staff. It is the successor to X271Viewer, renamed to reflect expansion beyond the 271 transaction.

**Primary persona:** Billers — staff who reconcile payments, investigate denials, and follow up on unpaid claims.  
**Secondary persona:** EDI analysts — technical staff who diagnose transaction-level issues.

**MVP success target:** 50 active users across 2 organizations within 6 months of Phase 2 GA.

---

## 2. Architectural Decisions

| Decision | Choice | Rationale |
|---|---|---|
| UI framework | WPF (existing) | No cross-platform requirement established |
| Project structure | Single shared Domain + Application | Avoids project explosion; transaction modules namespaced within |
| Transaction views | Purpose-built per transaction | Generic tree insufficient for biller persona |
| Code set data | JSON embedded in Domain project | Follows existing `X12CodeTables.json` pattern; low update frequency |
| CLI transaction routing | Auto-detect from ST01 | Unambiguous; explicit `--type` override available |
| Solution name | `X12Viewer` | Suite-level brand; namespace `woliver13.X12Viewer.*` |

---

## 3. Phase 2 — 835 Remittance Viewer

### 3.1 Goals

Enable a biller to open an 835 ERA file and immediately understand:
- Which claims were paid, partially paid, or denied
- Why adjustments were made (CARC plain-English descriptions)
- What the patient owes
- Export a per-service-line reconciliation spreadsheet

### 3.2 Scope

**In scope:**
- 835 file parsing via X12Net (ST01 = `835`, X12 005010X221A1)
- Purpose-built claim tree view:
  - Top level: ISA envelope → GS group → ST transaction
  - Second level: one node per CLP (claim), showing Claim ID, billed, paid, status in plain English
  - Third level: per-claim SVC service lines with billed/paid/adjusted amounts
  - Fourth level: CAS adjustment segments with CARC code + plain-English description
- CARC and RARC code set embedded in `X12CodeTables.json`
- Per-service-line CSV export (one row per SVC; CLP fields repeated for context: Claim ID, patient name, total billed, total paid)
- 5010 specification validation (X12 005010X221A1 structural rules)
- Auto-detection of 835 from ST01 in CLI and WPF file open
- Representative 835 test fixture covering: fully paid claim, partially paid claim with CARC adjustment, denied claim, claim with RARC remark

**Out of scope for Phase 2 (backlog):**
- Payer-specific validation rules
- Side-by-side 271 vs 835 comparison
- Role-based access control and HIPAA audit logging
- PDF export
- Patient responsibility calculator
- Payment posting summary

### 3.3 835 Tree View — Biller-First Layout

```
835 Remittance [ISA date / payer name]
└── Payment: $12,450.00 from BCBS IL [GS]
    └── Transaction 0001 [ST]
        ├── Claim: SMITH, ROBERT — $1,250.00 billed → $1,100.00 paid [CLP*PAID]
        │   ├── Service: 99213 — $250.00 billed → $220.00 paid
        │   │   └── Adjustment: CO-45 — Charge exceeds fee schedule ($30.00)
        │   └── Service: 93000 — $1,000.00 billed → $880.00 paid
        │       └── Adjustment: CO-45 — Charge exceeds fee schedule ($120.00)
        └── Claim: JONES, MARY — $800.00 billed → $0.00 paid [CLP*DENY]
            └── Service: 99214 — $800.00 billed → $0.00 paid
                └── Adjustment: CO-4 — Service not covered (denial)
```

### 3.4 CSV Export Format

| Payer | Claim ID | Patient | Billed | Paid | Claim Status | Proc Code | SVC Billed | SVC Paid | CARC | CARC Description | Adj Amount |
|---|---|---|---|---|---|---|---|---|---|---|---|
| BCBS IL | 1234567 | SMITH, ROBERT | 1250.00 | 1100.00 | Paid | 99213 | 250.00 | 220.00 | CO-45 | Charge exceeds fee schedule | 30.00 |

### 3.5 Acceptance Criteria

- [ ] 835 file opens from WPF file picker and renders claim tree within 3 seconds for a typical ERA (≤ 500 claims)
- [ ] Each CLP node displays claim ID, patient name, billed amount, paid amount, and claim status in plain English
- [ ] Each CAS segment displays the CARC code and its plain-English description from `X12CodeTables.json`
- [ ] RARC remarks (MOA segments) are displayed where present
- [ ] CSV export produces one row per SVC with all required columns
- [ ] CLI `x12viewer parse file.edi` auto-detects ST01=835 and routes to 835 parser
- [ ] CLI `x12viewer interpret file.edi` produces plain-English output for 835
- [ ] 5010 structural validation flags missing required segments
- [ ] All test fixture scenarios (paid, partial, denied, RARC) pass unit tests
- [ ] Existing 271 tests continue to pass (no regression)

---

## 4. Phase 3 — 276/277 Claim Status Viewer

### 4.1 Goals

Enable a biller to check the status of a submitted claim before the 835 remittance arrives, closing the gap between submission and payment.

### 4.2 Scope (outline — detail in Phase 3 PRD)

- 276 Claim Status Request: parse and display submitted claim inquiry details
- 277 Claim Status Response: parse and display payer status response in plain English (accepted, pending, rejected, additional information requested)
- Status codes (STC segments) translated to plain-English using embedded code table
- CLI auto-detection of 276/277 from ST01
- Test fixtures for: accepted, pending, rejected, and additional-info-requested responses

**Deferred to Phase 4+:**
- 837 Claims (Professional, Institutional, Dental) — different primary persona (coders, front-end revenue cycle)

---

## 5. Backlog (Post-Phase 3)

| Feature | Original Phase | Reason Deferred |
|---|---|---|
| Side-by-side 271 vs 835 comparison | 2 | Requires cross-file correlation model; revisit when both viewers are mature |
| Payer-specific validation rules | 2 | No rule source available; enterprise add-on candidate |
| Role-based access & HIPAA audit logging | 2 | Requires server/cloud tier; no central log store in local-first model |
| PDF export | 2 | Lower biller priority vs CSV; add after CSV validated |
| 837 Claims viewer | 3 | Different persona (coders); deferred until 276/277 complete |
| AI summarization & anomaly detection | 4 | Differentiation feature; premature before viewer suite is complete |
| Cloud sync | 4 | Requires server infrastructure decision |
| Mobile companion app | 5 | No established demand |
| FHIR integration | 5 | Enterprise tier prerequisite |

---

## 6. Solution Rename

The solution and all projects will be renamed from `X271Viewer` to `X12Viewer` as the first task of Phase 2:

| Old | New |
|---|---|
| `X271Viewer.sln` | `X12Viewer.sln` |
| `X271Viewer.Domain` | `X12Viewer.Domain` |
| `X271Viewer.Application` | `X12Viewer.Application` |
| `X271Viewer.Wpf` | `X12Viewer.Wpf` |
| `X271Viewer.Cli` | `X12Viewer.Cli` |
| `X271Viewer.Tests` | `X12Viewer.Tests` |
| `woliver13.X271Viewer.*` | `woliver13.X12Viewer.*` |

---

## 7. Out of Scope (All Phases)

- Building or transmitting X12 EDI files (viewer only)
- Clearinghouse connectivity
- Direct payer API integration
- EHR/PM system integration (Phase 3+ consideration only)

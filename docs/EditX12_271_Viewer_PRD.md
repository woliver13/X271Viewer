# Product Requirements Document (PRD)  
**EditX12 271 Eligibility & Benefits Response Document Interpreting Viewer**

**Version:** 1.1  
**Date:** June 13, 2026  
**Author:** Grok (xAI) / Bill Oliver  
**Status:** Draft — Architecture Decisions Incorporated

## 1. Executive Summary
The **EditX12 271 Viewer** is a specialized Windows desktop application for EDI specialists, payer analysts, and billing vendors. It enables users to **view, parse, interpret, and validate** X12 271 (Eligibility and Benefits Response) transaction documents.

**Core Value Proposition:**  
Transform complex, hard-to-read EDI 271 files into human-friendly, actionable information with full X12 code set interpretation. Reduce errors in benefits verification workflows and support EDI debugging and integration workflows.

**Primary Persona:** Priya (EDI Specialist) — debugs complex 271 files at payers and clearinghouses, needs power features, works on Windows in corporate environments.

**Secondary Personas (Post-MVP):**
- Marcus (Payer Analyst) — reviews and corrects high-volume 271 responses
- Emma (Provider Biller) — needs quick readable summary of patient coverage
- Carlos (Customer Service) — interprets benefits for members over the phone

**Key Differentiators:**  
- Full X12 005010X279A1 code set interpretation (every valid code, not just common ones)
- Three-pane UI: hierarchical tree, plain-English interpretation, raw segment view simultaneously
- CLI interface with JSON output for AI tool-use and shell pipelines
- Built on X12Net NuGet package — no raw EDI parsing in the viewer itself

**MVP Launch Target:** Q4 2026

## 2. Problem Statement
- X12 271 files are dense, segment-based EDI text that is difficult for non-technical users to understand.  
- Manual interpretation leads to errors in patient eligibility, coverage details, benefits, co-pays, deductibles, and limitations.  
- Existing tools are either too technical (for developers) or too rigid (read-only viewers).  
- Poor support for troubleshooting rejections or partial responses.
- No good CLI-accessible tools for integrating 271 interpretation into automated pipelines or AI workflows.

## 3. Objectives & Success Metrics
**Business Objectives:**  
- Reduce eligibility verification time by 60%.  
- Decrease benefits-related claim denials by 25%.  
- Achieve 10,000+ active users in Year 1 (payers + providers).

**KPIs:**  
- Interpretation accuracy: ≥ 98% on standard test files.  
- User satisfaction: NPS ≥ 60.  
- Average session time for a 271 file: < 90 seconds.

## 4. Target Audience & Personas
- **Priya (EDI Specialist) [PRIMARY]:** Debugs complex 271 files, compares versions, validates against X12 spec. Needs full code coverage, raw segment access, and CLI for automation.
- **Marcus (Payer Analyst):** Reviews and corrects high-volume 271 responses, validates against business rules.
- **Emma (Provider Biller):** Needs quick readable summary of patient coverage during check-in.
- **Carlos (Customer Service):** Interprets benefits for members over the phone.

## 5. Core Features & Requirements

### MVP Features
1. **Document Ingestion & Parsing**
   - Load single .edi / .txt / .x12 files via file dialog or CLI argument.
   - Parsing delegated entirely to X12Net NuGet package.
   - Automatic detection of ISA/GS/ST envelopes and version (5010+).
   - Support for 271 responses (including 270/271 paired workflows).

2. **Smart Interpretation Viewer (Three-Pane WPF UI)**
   - **Left pane:** Hierarchical tree view of the full ISA → GS → ST → HL loop structure.
     - ISA/GS envelope nodes collapsed by default; expand on demand.
     - HL hierarchy: Information Source (HL 20) → Information Receiver (HL 21) → Subscriber (HL 22) → Dependent (HL 23).
     - EB segments grouped by Service Type code under each Subscriber/Dependent node.
   - **Top-right pane:** Plain-English interpretation of the selected node.
     - Full X12 005010X279A1 code set coverage — every valid code interpreted, not just common ones.
     - Code tables stored as JSON embedded resources in Domain assembly, loaded into memory at startup.
     - Example: "Deductible met: $450 of $2,000 (Individual, In-Network, Calendar Year)".
   - **Bottom-right pane:** Raw EDI segment(s) for the selected node, syntax-highlighted.
   - Search/filter across all loops and elements.

3. **Validation**
   - X12 syntax and code-set validation via X12Net.
   - Error highlighting in the tree and raw pane with plain-English explanations.

4. **Export**
   - Export parsed + interpreted structure to JSON.

### CLI Interface (MVP)
Entry point: `x271` console executable, shares Domain and Application logic with WPF.

| Command | Output | Description |
|---|---|---|
| `x271 parse <file>` | JSON to stdout | Raw parsed segment tree |
| `x271 interpret <file>` | JSON to stdout | Parsed tree with plain-English interpretations |
| `x271 validate <file>` | JSON to stdout | Validation errors and warnings |
| `x271 view <file>` | Launches WPF | Opens viewer with file pre-loaded |

All commands output structured JSON suitable for AI tool-use and shell pipelines.

### Additional Features (Post-MVP)
- In-place editor with syntax validation and undo/redo.
- Side-by-side 271 comparison.
- Batch processing (500+ files).
- Export to PDF, Excel.
- Role-based access and audit logging (HIPAA compliant).
- Custom business rules engine (configurable per payer/contract).
- AI-assisted anomaly detection and benefit summarization.
- Template-based response generation.
- Web version.

## 6. Technical Architecture

### Solution Structure
```
X271Viewer.sln
├── X271Viewer.Domain          — interpretation logic, X12 code tables, EB mapping (no UI deps)
├── X271Viewer.Application     — parse/interpret/validate use cases (shared by CLI and WPF)
├── X271Viewer.Wpf             — WPF three-pane UI
└── X271Viewer.Cli             — console entry point, wraps Application use cases
```

**Dependency rules:**
- `Domain` and `Application` have no WPF references — testable in isolation.
- Both `Wpf` and `Cli` reference `Application`.
- X12Net NuGet package referenced only by `Domain`/`Application`, never by UI layer.
- X271Viewer never manipulates raw X12 bytes directly — all EDI operations go through X12Net.

### X12 Code Tables
- Source: ASC X12 005010X279A1 reference data.
- Storage: JSON files as embedded resources in `X271Viewer.Domain`.
- Runtime: Loaded once at startup into `IReadOnlyDictionary<string, string>`.
- Covers: Service Type codes, Eligibility/Benefit codes, Coverage Level codes, Time Period qualifiers, and all other EB-related code sets.

### Dependencies
- **X12Net** (NuGet, versioned) — all X12 parsing, validation, and EDI manipulation.
- **.NET / WPF** — Windows desktop UI.
- **System.CommandLine or Spectre.Console** — CLI argument parsing.

## 7. Non-Functional Requirements
- **Platform:** Windows desktop (primary). macOS/web post-MVP.
- **Performance:** Open and parse 1MB+ 271 files in < 3 seconds.
- **Security:** Local processing only (no PHI leaves the machine). HIPAA / HITRUST aligned.
- **Accessibility:** WCAG 2.2 AA compliant.
- **Offline Support:** Full functionality without internet.
- **Standards:** X12 005010X279A1 (and newer), ASC X12.

## 8. User Stories (Prioritized)

**Must Have (MVP)**
- As Priya, I can load a 271 file and see the full HL loop hierarchy in a tree.
- As Priya, I can click any node and see a plain-English interpretation of every segment element.
- As Priya, I can see the raw EDI for any node simultaneously with its interpretation.
- As Priya, I can run `x271 validate <file>` and get structured JSON errors I can pipe to other tools.
- As Priya, I can run `x271 interpret <file>` and get a full JSON interpretation for use in AI workflows.

**Should Have (Post-MVP)**
- Export clean PDF for patient or internal records.
- In-place editing with undo/redo.
- Validate against payer-specific custom rules.

**Could Have**
- Dark mode, customizable layouts, integration with common PM/EHR systems.
- Side-by-side file comparison.

## 9. Risks & Dependencies
- **Risks:** Frequent X12 version changes, payer-specific variations, HIPAA compliance overhead, completeness of embedded code tables.
- **Mitigations:** Modular code table design (JSON files, easy to update), versioned X12Net dependency, local-only processing for HIPAA.
- **Dependencies:** X12Net NuGet package stability, access to comprehensive test 271 files from multiple payers.

## 10. Roadmap & Timeline
- **Phase 1 (MVP — 10 weeks):** Ingestion, three-pane viewer, full code table interpretation, validation, JSON export, CLI (parse/interpret/validate).
- **Phase 2 (6 weeks):** In-place editor, PDF/Excel export, custom validation rules, batch processing.
- **Phase 3:** Web version, AI enhancements, role-based access, audit logging.

## 11. Appendices
- Glossary (ISA, GS, ST, NM1, HL loops, EB segment, etc.)
- Sample 271 file before/after interpretation (to be included).
- Competitor Landscape: Edifecs, Change Healthcare tools, simple online 271 parsers.

---

*v1.1 — Architecture decisions incorporated from design review session, June 13, 2026.*

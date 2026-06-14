# X271Viewer

A Windows desktop application for viewing, parsing, interpreting, and validating X12 271 Eligibility and Benefits Response documents. Built for EDI specialists, payer analysts, and billing vendors who need to understand complex EDI files quickly.

## What It Does

X12 271 files are dense, segment-based EDI text that is difficult to read and error-prone to interpret manually. X271Viewer transforms them into human-readable information while giving power users direct access to the underlying structure.

**Core capabilities (MVP):**
- Load `.edi` / `.txt` / `.x12` files and parse the full ISA → HL loop hierarchy
- Three-pane UI: tree navigation, plain-English interpretation, raw segment view — all visible simultaneously
- Full X12 005010X279A1 code set coverage — every valid Service Type, Eligibility/Benefit, and Coverage Level code interpreted
- X12 syntax and code-set validation with plain-English error explanations
- JSON export of the parsed and interpreted structure
- CLI interface for AI tool-use and shell pipeline integration

## CLI Usage

```
x271 parse <file>        Parse file and emit segment tree as JSON
x271 interpret <file>    Parse + interpret and emit full annotated JSON
x271 validate <file>     Run validation and emit errors/warnings as JSON
x271 view <file>         Open the WPF viewer with the file pre-loaded
```

All commands write structured JSON to stdout.

## Solution Structure

```
X271Viewer.sln
├── X271Viewer.Domain          # Interpretation logic, X12 code tables, EB mapping
├── X271Viewer.Application     # Use cases shared by CLI and WPF
├── X271Viewer.Wpf             # Three-pane WPF desktop UI
└── X271Viewer.Cli             # Console entry point
```

- `Domain` and `Application` have no WPF dependencies and are fully testable in isolation.
- All X12 parsing and EDI manipulation is handled by the [X12Net](https://github.com/your-org/X12Net) NuGet package — the viewer never touches raw EDI bytes directly.

## Requirements

- Windows 10/11
- .NET 8+
- X12Net NuGet package (referenced automatically via restore)

## Status

**Pre-release — active development.** See [docs/EditX12_271_Viewer_PRD.md](docs/EditX12_271_Viewer_PRD.md) for the full product requirements and roadmap.

Target: MVP Q4 2026.

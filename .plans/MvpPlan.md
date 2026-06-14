# Plan: X271Viewer MVP

> Source PRD: [docs/EditX12_271_Viewer_PRD.md](../docs/EditX12_271_Viewer_PRD.md)

## Architectural Decisions

Durable decisions that apply across all phases:

- **Solution structure:** Four projects — `X271Viewer.Domain`, `X271Viewer.Application`, `X271Viewer.Wpf`, `X271Viewer.Cli`. Domain and Application have no WPF references and are fully testable in isolation.
- **X12 boundary:** All X12 parsing, validation, and EDI manipulation is delegated to the X12Net NuGet package. X271Viewer never manipulates raw EDI bytes directly.
- **Key models:** `X271Document` (parsed interchange tree), `X271Node` (a single tree node wrapping an HL loop or segment), `X271Interpretation` (plain-English annotation for a node), `X271ValidationResult` (structured error list).
- **Code tables:** Full X12 005010X279A1 reference data stored as JSON embedded resources in `Domain`, loaded once at startup into `IReadOnlyDictionary<string, string>` lookup tables.
- **JSON schema:** A single canonical JSON schema used by both the WPF export (Phase 5) and CLI stdout (Phase 6). Defined in `Application`.
- **UI layout:** Three-pane WPF window — left: HL loop tree, top-right: plain-English interpretation, bottom-right: raw segment view. Panes separated by `GridSplitter`.
- **CLI entry point:** `x271` executable in `X271Viewer.Cli`. Commands: `parse`, `interpret`, `validate`, `view`.
- **Target framework:** .NET 8, Windows desktop.

**Acceptance criteria key:**
- `[AFK]` — Away From Keyboard: verifiable by automated test (unit, integration, or CLI output assertion).
- `[HITL]` — Human In The Loop: requires manual visual or interactive verification.

---

## Phase 1: Solution Scaffold + File Load

**User stories:**
- As Priya, I can load a 271 file and have it recognized by the application.

### What to build

Create the four-project solution, reference X12Net NuGet in `Domain` and `Application`, and wire a file-open dialog in the WPF shell. On file selection, parse the file via X12Net and display the raw ISA header segment text in the bottom-right pane. The top-right and left panes are empty placeholders at this stage. This phase proves the full compile-to-display stack works end-to-end.

### Acceptance criteria

- [ ] `[AFK]` Solution builds cleanly with no errors or warnings across all four projects.
- [ ] `[AFK]` X12Net NuGet package restores successfully on a clean `dotnet restore`.
- [ ] `[AFK]` `X271Viewer.Domain` and `X271Viewer.Application` have zero direct or transitive references to WPF assemblies.
- [ ] `[HITL]` File-open dialog accepts `.edi`, `.txt`, and `.x12` extensions and rejects others.
- [ ] `[HITL]` After selecting a valid 271 file, the ISA header segment text appears in the bottom-right pane.
- [ ] `[HITL]` Selecting a file that is not a valid X12 interchange shows a user-facing error message (not an unhandled exception).

---

## Phase 2: HL Tree Navigation

**User stories:**
- As Priya, I can load a 271 file and see the full HL loop hierarchy in a tree.
- As Priya, I can click any node and see its raw EDI segment(s).

### What to build

Populate the left tree pane with the complete ISA → GS → ST → HL node hierarchy derived from the parsed `X271Document`. ISA and GS envelope nodes are collapsed by default. EB segments appear as child nodes under their Subscriber or Dependent HL node, grouped by Service Type code. Clicking any node updates the bottom-right pane with the raw segment text for that node. The top-right interpretation pane remains empty.

### Acceptance criteria

- [ ] `[AFK]` Tree model construction (mapping X12Net DOM to `X271Node` hierarchy) is covered by unit tests using a sample 271 fixture.
- [ ] `[AFK]` EB segments are correctly grouped by Service Type code in the domain model.
- [ ] `[HITL]` ISA and GS envelope nodes render collapsed by default; they expand on click.
- [ ] `[HITL]` HL hierarchy (Information Source → Information Receiver → Subscriber → Dependent) renders with correct nesting and human-readable node labels.
- [ ] `[HITL]` EB child nodes are visually grouped under their Service Type heading within a Subscriber or Dependent node.
- [ ] `[HITL]` Clicking any tree node updates the bottom-right pane with the raw segment text for that node.
- [ ] `[HITL]` A 271 file with multiple Subscriber loops renders all of them in the tree without truncation.

---

## Phase 3: Plain-English Interpretation

**User stories:**
- As Priya, I can click any node and see a plain-English interpretation of every segment element.

### What to build

Embed the full X12 005010X279A1 code tables (Service Type codes, Eligibility/Benefit codes, Coverage Level codes, Time Period qualifiers, and all EB-related code sets) as JSON embedded resources in `X271Viewer.Domain`. Wire the interpretation engine in `Application` so that selecting a tree node populates the top-right pane with a structured plain-English breakdown of that node's elements. Dollar amounts, dates, time period qualifiers, and code descriptions all render in human-readable form. Unknown codes degrade gracefully with the raw code shown alongside a "unrecognized code" label rather than crashing or showing nothing.

### Acceptance criteria

- [ ] `[AFK]` All Service Type codes defined in X12 005010X279A1 resolve to a non-empty plain-English label in unit tests.
- [ ] `[AFK]` All Eligibility/Benefit Information codes (EB01) resolve correctly.
- [ ] `[AFK]` All Coverage Level codes (EB02) resolve correctly.
- [ ] `[AFK]` Dollar amount formatting (EB06/07) produces correctly formatted currency strings.
- [ ] `[AFK]` An unrecognized code value degrades gracefully — returns the raw code with a "unrecognized code" indicator rather than throwing.
- [ ] `[HITL]` Clicking a Subscriber HL node populates the top-right pane with a readable member identity summary.
- [ ] `[HITL]` Clicking an EB node shows a plain-English benefit description matching the example format: "Deductible: $450 of $2,000 (Individual, In-Network, Calendar Year)".
- [ ] `[HITL]` All three panes are visible and populated simultaneously when a node is selected.

---

## Phase 4: Validation Display

**User stories:**
- As Priya, I can see validation errors on a 271 file with plain-English explanations.

### What to build

Run X12Net validation on file load and surface the results throughout the UI. Tree nodes that contain validation errors are visually highlighted. Selecting an errored node shows the plain-English error explanation in the bottom-right pane alongside the raw segment. Valid files show a clean status indicator. Validation results are held in the `X271ValidationResult` model in `Application`.

### Acceptance criteria

- [ ] `[AFK]` Validation runs automatically on file load without requiring a user action.
- [ ] `[AFK]` A deliberately malformed 271 fixture (missing required element, invalid code value) produces at least one `X271ValidationResult` error in unit tests.
- [ ] `[AFK]` A well-formed 271 fixture produces zero errors.
- [ ] `[HITL]` Tree nodes with validation errors display a distinct visual indicator (e.g., red icon or highlight).
- [ ] `[HITL]` Selecting an errored node shows the plain-English error explanation in the bottom-right pane.
- [ ] `[HITL]` A valid file shows a "No validation errors" status indicator visible without selecting any node.

---

## Phase 5: JSON Export

**User stories:**
- As Priya, I can export the interpreted structure to JSON for use in downstream tools.

### What to build

Add an Export action in the WPF menu that writes the full interpreted and validated structure to a `.json` file chosen via a save dialog. The output follows the canonical JSON schema defined in `Application` — this same schema will be used by the CLI in Phase 6. The export includes: the HL loop hierarchy, plain-English interpretations for every node, element-level values, and the full validation result list.

### Acceptance criteria

- [ ] `[AFK]` The canonical JSON schema is defined in `Application` with no dependency on WPF.
- [ ] `[AFK]` Serializing a parsed + interpreted `X271Document` to JSON and deserializing it round-trips without data loss.
- [ ] `[AFK]` The exported JSON is valid per the canonical schema for a set of sample 271 fixtures.
- [ ] `[AFK]` Validation errors are included in the exported JSON under a top-level `validationResults` array.
- [ ] `[HITL]` The Export menu item is discoverable and triggers a save-file dialog defaulting to `.json` extension.
- [ ] `[HITL]` The exported file opens correctly in a text editor and contains readable, indented JSON.

---

## Phase 6: CLI Interface

**User stories:**
- As Priya, I can run `x271 parse`, `x271 interpret`, and `x271 validate` from the command line and get JSON output for use in AI workflows and shell pipelines.

### What to build

Wire `X271Viewer.Cli` with four commands using the Application use cases built in prior phases. All commands accept a file path as their primary argument. `parse`, `interpret`, and `validate` write structured JSON to stdout. `view` launches the WPF process with the file pre-loaded. Commands exit with a non-zero exit code on failure. The JSON output of `interpret` and `validate` uses the same canonical schema as the Phase 5 export.

### Acceptance criteria

- [ ] `[AFK]` `x271 parse <file>` outputs valid JSON containing the raw segment tree to stdout.
- [ ] `[AFK]` `x271 interpret <file>` outputs valid JSON with plain-English interpretations for every node.
- [ ] `[AFK]` `x271 validate <file>` outputs valid JSON with a structured list of validation errors and warnings.
- [ ] `[AFK]` All three JSON-output commands exit with code `0` on a valid file and non-zero on an invalid or missing file.
- [ ] `[AFK]` The JSON schema of `x271 interpret` output matches the Phase 5 WPF export schema exactly.
- [ ] `[AFK]` `x271 --help` lists all four commands with a brief description.
- [ ] `[HITL]` `x271 view <file>` launches the WPF viewer with the specified file pre-loaded in the tree.
- [ ] `[HITL]` CLI error messages (missing file, parse failure) are human-readable on stderr and do not bleed into stdout JSON output.

---

## Phase 7: Search and Filter

**User stories:**
- As Priya, I can search for a specific service type or element value and see only the matching nodes.

### What to build

Add a search/filter input above the left tree pane. As Priya types, the tree filters to show only nodes whose segment ID, element values, or plain-English interpretation labels match the query (case-insensitive). Non-matching nodes are hidden or dimmed. Matching text is highlighted within the visible node labels. Clearing the search input restores the full tree. An empty-results state is shown when no nodes match.

### Acceptance criteria

- [ ] `[AFK]` Filter logic (matching a query string against `X271Node` properties) is covered by unit tests.
- [ ] `[AFK]` Filter matches on segment ID (e.g., "EB"), raw element values, and plain-English interpretation labels.
- [ ] `[AFK]` Filter is case-insensitive.
- [ ] `[HITL]` Typing in the search box updates the tree in real time without a submit action.
- [ ] `[HITL]` Non-matching nodes are hidden (not merely dimmed) so the result set is easy to scan.
- [ ] `[HITL]` Matching text is visually highlighted within node labels.
- [ ] `[HITL]` Clearing the search input fully restores the unfiltered tree.
- [ ] `[HITL]` When no nodes match, a visible "No results" message appears in the tree pane.

# X271Viewer — Manual Regression Test Plan

This document covers every `[HITL]` acceptance criterion from `.plans/MvpPlan.md` that cannot be verified by the automated test suite. Criteria that have been fully automated are noted inline with the test name that covers them — run those as part of CI, not this plan.

---

## Prerequisites

### Environment
- Windows 10 or 11
- .NET 9 SDK (for the WPF app) and .NET 8 SDK (for the CLI)
- Visual Studio 2022 or VS Code with C# extension (for IDE checks only)

### Build
```
dotnet build
```
Must complete with **0 errors, 0 warnings** across all projects.

### Sample files

| File | Path | Purpose |
|---|---|---|
| Full 271 with dependent | `tests/X271Viewer.Tests/Fixtures/full271.edi` | Primary regression file — subscriber + dependent + EB data |
| Subscriber-as-patient | `tests/X271Viewer.Tests/Fixtures/subscriber271.edi` | 3-level hierarchy, no dependent |
| Multi-subscriber | `tests/X271Viewer.Tests/Fixtures/multi_subscriber271.edi` | Two subscriber HL loops under one payer |
| Malformed (missing EB01) | `tests/X271Viewer.Tests/Fixtures/malformed271.edi` | Triggers validation errors |
| Non-X12 content | `tests/X271Viewer.Tests/Fixtures/not_x12.edi` | Triggers parse exception |
| Valid, no errors | `tests/X271Viewer.Tests/Fixtures/valid271.edi` | Clean file, zero validation errors |

### Launch the WPF app
```
dotnet run --project src/X271Viewer.Wpf
```

### Launch the CLI
```
dotnet run --project src/X271Viewer.Cli -- <command> <file>
```

---

## Automated coverage summary

The following HITL criteria are **fully covered by automated tests** and do not require manual verification on every regression run. They are listed here for traceability only.

| Criterion | Covering test(s) |
|---|---|
| P2H1 — ISA and GS nodes are collapsed by default | `TreeBuilder_root_is_ISA_node_collapsed_by_default`, `TreeBuilder_ISA_has_GS_child_collapsed` |
| P2H2 — HL hierarchy has correct nesting and labels | `TreeBuilder_ST_has_HL_nodes_in_correct_hierarchy` |
| P2H3 — EB segments are grouped by Service Type | `TreeBuilder_EB_segments_grouped_by_service_type` |
| P2H5 — Multiple subscriber loops all render | `TreeBuilder_multiple_subscriber_loops_all_render` |
| P3H1 — Subscriber node shows member identity summary | `InterpretationEngine_interprets_NM1_subscriber_with_DMG_segments` |
| P3H2 — EB node shows plain-English benefit description | `InterpretationEngine_interprets_EB_copayment_node`, `InterpretationEngine_interprets_EB_deductible_node_with_amount` |
| P5H2 — Exported JSON is indented and human-readable | `Export_produces_indented_JSON` |
| P6H2 — CLI error messages are human-readable (no stack traces) | `Error_message_on_missing_file_is_human_readable`, `Error_message_on_parse_failure_is_human_readable` |
| P7H4 — Clearing search restores the full tree | `Filter_returns_full_tree_when_query_is_empty_or_whitespace` |
| P7H5 — No-match state returns empty result | `Filter_returns_empty_when_no_nodes_match` |

---

## Phase 1 — Solution Scaffold and File Load

### TC-1.1 — File-open dialog filters extensions

**Precondition:** WPF app is running.

**Steps:**
1. Click **File → Open…** (or press Ctrl+O).
2. In the file-open dialog, inspect the file-type filter dropdown.
3. Attempt to open `full271.edi` (valid `.edi` file).
4. Open a second file with a `.txt` extension (rename `full271.edi` to `full271.txt` if needed).
5. Open a second file with a `.x12` extension (rename if needed).
6. Attempt to navigate to a `.pdf` or `.docx` file in the dialog.

**Expected result:**
- [ ] The filter shows `.edi`, `.txt`, and `.x12` as accepted extensions.
- [ ] All three accepted extensions open without error.
- [ ] Files with other extensions are either greyed out in the dialog or produce a clear rejection message.

---

### TC-1.2 — ISA header appears in bottom-right pane after file load

**Precondition:** WPF app is running.

**Steps:**
1. Open `full271.edi` via File → Open….

**Expected result:**
- [ ] The bottom-right pane displays the raw ISA segment text (beginning with `ISA*00*`).
- [ ] The text appears within 2 seconds of file selection.

---

### TC-1.3 — Invalid X12 content shows user-facing error message

**Precondition:** WPF app is running.

**Steps:**
1. Open `not_x12.edi` via File → Open….

**Expected result:**
- [ ] A user-facing error dialog or status message appears explaining the file could not be parsed.
- [ ] The application does **not** show an unhandled exception dialog or crash.
- [ ] The app remains usable after dismissing the error.

---

## Phase 2 — HL Tree Navigation

### TC-2.1 — Clicking a tree node updates the bottom-right pane

**Precondition:** `full271.edi` is loaded. The tree is visible in the left pane.

**Steps:**
1. Expand the ISA node.
2. Expand the GS node.
3. Expand the ST node.
4. Click the **Information Source** HL node.
5. Click the **Information Receiver** HL node.
6. Click the **Subscriber** HL node.
7. Click an **EB** leaf node.

**Expected result:**
- [ ] Each click updates the bottom-right (raw segment) pane with the raw EDI segment(s) for that node.
- [ ] The pane content changes with every node selection; stale content from the previous selection does not persist.

---

## Phase 3 — Plain-English Interpretation

### TC-3.1 — All three panes are populated simultaneously

**Precondition:** `full271.edi` is loaded.

**Steps:**
1. Expand the tree to the Subscriber HL node and click it.

**Expected result:**
- [ ] The left tree pane shows the node hierarchy.
- [ ] The top-right pane shows a plain-English interpretation of the selected node.
- [ ] The bottom-right pane shows the raw EDI segment text.
- [ ] All three panes are simultaneously visible without needing to scroll or resize.

---

## Phase 4 — Validation Display

### TC-4.1 — Errored nodes have a visual indicator

**Precondition:** `malformed271.edi` is loaded.

**Steps:**
1. Observe the tree after file load.

**Expected result:**
- [ ] At least one tree node displays a distinct visual indicator (e.g., red icon, coloured highlight, or error badge) without requiring any click.

---

### TC-4.2 — Selecting an errored node shows the error explanation

**Precondition:** `malformed271.edi` is loaded. At least one node has a visual error indicator (see TC-4.1).

**Steps:**
1. Click a node that has the error indicator.

**Expected result:**
- [ ] The bottom-right pane displays a plain-English error explanation for that node.
- [ ] The error text is human-readable (e.g., "EB01 is missing" rather than an error code alone).

---

### TC-4.3 — Valid file shows a "No validation errors" status

**Precondition:** WPF app is running.

**Steps:**
1. Open `valid271.edi` via File → Open….

**Expected result:**
- [ ] A "No validation errors" (or equivalent) status indicator is visible in the UI without selecting any node.
- [ ] No nodes display error indicators.

---

## Phase 5 — JSON Export

### TC-5.1 — Export menu triggers a save-file dialog defaulting to .json

**Precondition:** `full271.edi` is loaded.

**Steps:**
1. Click **File → Export JSON…** (or the equivalent export menu item).

**Expected result:**
- [ ] A save-file dialog opens.
- [ ] The default file extension in the dialog is `.json`.
- [ ] Saving to a chosen path produces a `.json` file on disk.

---

## Phase 6 — CLI Interface

### TC-6.1 — `x271 view` launches the WPF viewer with the file pre-loaded

**Precondition:** The solution is built (`dotnet build`).

**Steps:**
1. Run: `dotnet run --project src/X271Viewer.Cli -- view tests/X271Viewer.Tests/Fixtures/full271.edi`

**Expected result:**
- [ ] The WPF viewer window opens.
- [ ] `full271.edi` is pre-loaded — the tree is populated and the ISA segment text appears in the bottom-right pane without any manual file-open action.

---

## Phase 7 — Search and Filter

### TC-7.1 — Typing updates the tree in real time

**Precondition:** `full271.edi` is loaded. The tree is fully expanded.

**Steps:**
1. Click into the search box above the left tree pane.
2. Type `EB` one character at a time, pausing briefly between keystrokes.

**Expected result:**
- [ ] The tree updates after each keystroke without requiring Enter or a search button.
- [ ] The response is immediate (no perceptible lag on the test fixture files).

---

### TC-7.2 — Non-matching nodes are hidden (not merely dimmed)

**Precondition:** `full271.edi` is loaded. The tree is visible.

**Steps:**
1. Type `Deductible` in the search box.

**Expected result:**
- [ ] Only nodes whose label, raw segment, or interpretation matches "Deductible" are visible.
- [ ] Non-matching nodes are completely absent from the tree, not just greyed out or dimmed.

---

### TC-7.3 — Matching text is visually highlighted within node labels

**Precondition:** `full271.edi` is loaded.

**Steps:**
1. Type `EB` in the search box.
2. Observe the visible node labels.

**Expected result:**
- [ ] The substring "EB" (or "eb" / any case match) is visually distinguished within the node labels — e.g., bold, underlined, or highlighted in a contrasting colour.

---

## Test Run Log

| Date | Tester | Phase(s) | Result | Notes |
|---|---|---|---|---|
| | | | | |

# HITL Acceptance — Issue 5: JSON Export

## Setup

Build and launch the app in Release or Debug mode.

The sample EDI file to use is:

```
tests/X271Viewer.Tests/Fixtures/full271.edi
```

A pre-generated reference JSON export is at:

```
docs/hitl/issue-5-sample-export.json
```

Open that file in a text editor before starting so you can compare structure.

---

## AC1 — Export menu item is discoverable and disabled before opening a file

1. Launch the app without opening any file.
2. Click the **File** menu.
3. **Expected:** The menu shows two items — **Open…** and **Export JSON…**
4. **Expected:** **Export JSON…** is greyed out (disabled) — it cannot be clicked.

---

## AC2 — Export JSON… becomes enabled after opening a file

1. Click **File → Open…** and select `tests/X271Viewer.Tests/Fixtures/full271.edi`.
2. Wait for the tree to populate.
3. Click the **File** menu again.
4. **Expected:** **Export JSON…** is now enabled (not greyed out).

---

## AC3 — Export triggers a save-file dialog defaulting to `.json`

1. With `full271.edi` loaded, click **File → Export JSON…**
2. **Expected:** A **Save File** dialog opens.
3. **Expected:** The dialog's default file name is `export.json`.
4. **Expected:** The **Save as type** filter defaults to `JSON files (*.json)`.
5. Cancel the dialog. No error should appear.

---

## AC4 — Exported file is valid, indented JSON

1. Click **File → Export JSON…**
2. Choose a save location (e.g., your Desktop) and click **Save**.
3. **Expected:** A success message box appears: _"Exported to: …"_
4. Open the exported file in a text editor (Notepad, VS Code, etc.).
5. **Expected:** The file contains indented, human-readable JSON — not a single line.
6. **Expected:** The top-level keys present are `IsaRawText`, `Root`, and `ValidationResults`.
7. Compare against `docs/hitl/issue-5-sample-export.json`:
   - `IsaRawText` starts with `ISA*00*`
   - `Root.Label` is `ISA — Interchange 000000271`
   - `ValidationResults` contains one entry: `{ "Code": "MissingRequiredSegment", "Message": "Required segment 'IEA' is missing…" }`

---

## AC5 — Exported JSON contains readable EB interpretations

1. In the exported JSON, find the `Root.Children[0].Children[0].Children[0]` path
   (ISA → GS → ST → Information Source → Information Receiver → Subscriber → Dependent).
2. Drill into the first `EB` leaf node under the Dependent.
3. **Expected:** The `Interpretation` field contains plain-English text such as
   `"Benefit: Active Coverage"`, `"Coverage Level: Family"`, etc.
4. **Expected:** Dollar amounts appear formatted (e.g., `"$2,000.00"` not `"2000"`).

---

## AC6 — Keyboard shortcut Ctrl+E works

1. With `full271.edi` loaded (tree visible), press **Ctrl+E**.
2. **Expected:** The Save File dialog opens (same as AC3), identical to clicking **File → Export JSON…**.

Note: Ctrl+E is wired via a WPF `RoutedUICommand` with a `KeyGesture`, not just a display hint.
The shortcut is inactive when no file is loaded (CanExecute = false).

# HITL Acceptance — Issue 6: CLI Interface

## Setup

Build the CLI in Release mode and add it to your PATH, or invoke it directly via `dotnet run`:

```
dotnet run --project src/X271Viewer.Cli/X271Viewer.Cli.csproj -- <command> [file]
```

All examples below use `dotnet run --project src/X271Viewer.Cli/X271Viewer.Cli.csproj --` as the invocation prefix. Substitute `x271` if the published exe is on PATH.

Reference sample files are in `docs/hitl/`:
- `issue-6-sample-parse.json`
- `issue-6-sample-interpret.json`
- `issue-6-sample-validate.json`

EDI fixture: `tests/X271Viewer.Tests/Fixtures/full271.edi`

---

## AC1 — `--help` lists all four commands

```
dotnet run --project src/X271Viewer.Cli/X271Viewer.Cli.csproj -- --help
```

**Expected output (stdout):**
```
Usage: x271 <command> [file]

Commands:
  parse      <file>   Output raw segment tree as JSON
  interpret  <file>   Output interpreted node tree as JSON (Phase 5 schema)
  validate   <file>   Output validation results as JSON
  view       <file>   Launch the WPF viewer with the file pre-loaded
```

**Expected:** exit code 0, nothing on stderr.

---

## AC2 — `parse` outputs a JSON segments array

```
dotnet run --project src/X271Viewer.Cli/X271Viewer.Cli.csproj -- parse tests/X271Viewer.Tests/Fixtures/full271.edi
```

**Expected:** Valid indented JSON with a top-level `segments` array.  
**Expected:** Each element is a raw EDI segment string starting with the segment ID (e.g. `"ISA*00*…~"`, `"GS*HB*…~"`).  
**Expected:** Compare structure against `docs/hitl/issue-6-sample-parse.json`.  
**Expected:** exit code 0, nothing on stderr.

---

## AC3 — `interpret` output matches Phase 5 export schema

```
dotnet run --project src/X271Viewer.Cli/X271Viewer.Cli.csproj -- interpret tests/X271Viewer.Tests/Fixtures/full271.edi
```

**Expected:** Valid indented JSON with top-level keys `IsaRawText`, `Root`, and `ValidationResults`.  
**Expected:** `Root` is a nested object with `Label`, `Interpretation`, `RawSegments`, `ValidationErrors`, `Children`.  
**Expected:** EB leaf nodes contain plain-English `Interpretation` values (e.g. `"Benefit: Active Coverage"`).  
**Expected:** Output is byte-for-byte identical in schema to `docs/hitl/issue-6-sample-interpret.json` (values may differ only if the fixture changes).  
**Expected:** exit code 0, nothing on stderr.

---

## AC4 — `validate` outputs structured validation results

```
dotnet run --project src/X271Viewer.Cli/X271Viewer.Cli.csproj -- validate tests/X271Viewer.Tests/Fixtures/full271.edi
```

**Expected:** Valid indented JSON with `isValid` (boolean) and `errors` (array).  
**Expected:** `isValid` is `false` and `errors` contains one entry with `code: "MissingRequiredSegment"` (full271.edi is missing the IEA trailer).  
**Expected:** Compare against `docs/hitl/issue-6-sample-validate.json`.  
**Expected:** exit code 0, nothing on stderr.

---

## AC5 — Missing file error goes to stderr, stdout is empty

```
dotnet run --project src/X271Viewer.Cli/X271Viewer.Cli.csproj -- parse no_such_file.edi
```

**Expected:** stderr contains a human-readable error message (e.g. `Error: file not found: no_such_file.edi`).  
**Expected:** stdout is empty — no partial JSON output.  
**Expected:** exit code 1.

Repeat for `interpret` and `validate` with a missing file — same behaviour.

---

## AC6 — Unparseable file error goes to stderr, stdout is empty

```
dotnet run --project src/X271Viewer.Cli/X271Viewer.Cli.csproj -- parse tests/X271Viewer.Tests/Fixtures/not_x12.edi
```

**Expected:** stderr contains a human-readable parse error.  
**Expected:** stdout is empty.  
**Expected:** exit code 2.

---

## AC7 — `view` launches WPF viewer with file pre-loaded (HITL only)

`dotnet run` will not work here — the CLI needs `X271Viewer.Wpf.exe` in the same folder. Publish both projects to a shared output directory first:

```
dotnet publish src/X271Viewer.Cli/X271Viewer.Cli.csproj -o publish/ -p:GenerateDocumentationFile=false
dotnet publish src/X271Viewer.Wpf/X271Viewer.Wpf.csproj  -o publish/ -p:GenerateDocumentationFile=false
```

Then run the view command using the published exe:

```
publish/X271Viewer.Cli.exe view tests/X271Viewer.Tests/Fixtures/full271.edi
```

**Expected:** The WPF window opens with the HL loop tree already populated from `full271.edi`.  
**Expected:** The CLI process returns immediately (exit code 0) — it does not wait for the WPF window to close.  
**Expected:** No output on stdout or stderr.

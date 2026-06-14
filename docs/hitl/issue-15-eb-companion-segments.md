# HITL Acceptance — Issue 15: EB Loop Companion Segments

## Setup

Build and launch the app. Use the file at:

```
tests/X271Viewer.Tests/Fixtures/eb_with_companions.edi
```

This fixture has a single Subscriber (HL 3) with three EB groups. The first two
have companion segments (HSD, MSG, REF, DTP); the third (EB B) has none.

---

## AC1 — Raw segment pane includes all companion segments

1. Expand: **ISA → GS → ST → Information Source → Information Receiver → Subscriber (HL 3)**
2. Expand the **EB — Service Type 1** group and click the **EB 1/IND** leaf node.
3. **Expected — bottom-right raw pane shows all of:**

```
EB*1*IND*30*PR~
HSD*VS*30***22**~
MSG*Patient is eligible for up to 30 outpatient visits per calendar year.~
REF*18*GRP-12345~
DTP*291*RD8*20260101-20261231~
```

---

## AC2 — Interpretation pane shows HSD delivery detail

Still with the **EB 1/IND** leaf selected.

**Expected — top-right interpretation pane includes:**

```
Benefit: Active Coverage
  Service Type:   Health Benefit Plan Coverage
  Coverage Level: Individual
  Delivery: 30 Visits per Calendar Year
  Note: Patient is eligible for up to 30 outpatient visits per calendar year.
  Reference: 18 GRP-12345
  Date/Period: 291 20260101-20261231
```

---

## AC3 — MSG free-text appears for EB C node

1. Expand **EB — Service Type C** and click the **EB C/IND** leaf.
2. **Expected — interpretation pane includes:**
   - `Benefit: Deductible`
   - `Delivery: 90 Days per Calendar Year`
   - `Note: Inpatient deductible applies to first 90 days.`

---

## AC4 — EB node with no companions shows no companion lines

1. Expand **EB — Service Type B** and click the **EB B/IND** leaf.
2. **Expected — interpretation pane shows benefit info only:**
   - `Benefit: Co-Payment`
   - No `Delivery:` line
   - No `Note:` line
3. **Expected — raw pane shows only one segment:**
   ```
   EB*B*IND*96*PR*40~
   ```

---

## AC5 — Regression: full271.edi unaffected

Open `tests/X271Viewer.Tests/Fixtures/full271.edi` and repeat the Phase 3 HITL
checks. Clicking the Subscriber (HL 3) and Dependent (HL 4) EB nodes should
behave identically to before this change.

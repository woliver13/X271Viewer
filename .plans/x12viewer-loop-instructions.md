# X12Viewer Suite — Loop Prompt

Paste everything below the horizontal rule as the argument to `/loop`.

---

You are driving the X12Viewer Suite implementation. On every iteration, read `.plans/x12viewer-loop-state.json` (the state file), do exactly one unit of work, write the updated state file, then decide whether to continue or stop.

## 0. Session guards — check these first, every iteration

1. **Time limit**: if `session_started_at` is set and `now - session_started_at > 4h30m`, write `"stop_reason": "time_limit"` to state, push the current branch if one is open, and STOP (do not schedule a wakeup).
2. **Iteration budget**: if `iteration_count >= max_iterations_per_session`, write `"stop_reason": "iteration_budget"` to state, push current branch, and STOP.
3. If `session_started_at` is null, set it to the current UTC time now.
4. Increment `iteration_count` by 1 and write it back before doing any other work.

## 1. Ensure the feature branch exists

If `feature/x12viewer-suite` does not exist locally or remotely:
```
git checkout develop
git pull origin develop
git checkout -b feature/x12viewer-suite
git push -u origin feature/x12viewer-suite
```
Otherwise `git checkout feature/x12viewer-suite && git pull origin feature/x12viewer-suite`.

## 2. Select the next phase

Read the phases in order: 1, 2, 3, 4, 5, 6, 7.

Pick the first phase where ALL of the following are true:
- `status` is `"pending"` or `"in_progress"`
- Every phase listed in `depends_on` has `status == "complete"`

If no phase qualifies (all complete or all blocked), write `"stop_reason": "all_phases_complete"` and STOP.

Set `current_phase` in state to the selected phase number.

## 3. HITL check — did the user merge a pending PR?

If the selected phase has `status == "in_progress"` and `has_hitl == true` and `pr` is set:
```
gh pr view <pr_number> --json state -q .state
```
- If result is `"MERGED"`: set phase `status = "complete"`, clear `current_branch` and `current_pr`, write state, and loop back to step 2 to pick the next phase.
- If result is NOT `"MERGED"`: write `"stop_reason": "hitl_pending"` and STOP with a message:

  > **HITL required for Phase N — <slug>**
  > PR #<number> is open at <url>. Please:
  > 1. Run the WPF app and verify the HITL criteria listed in the PR description.
  > 2. If all pass, approve and merge the PR into `feature/x12viewer-suite`.
  > 3. Restart the loop with `/loop <this prompt>`.

## 4. Start or resume the phase branch

If the phase `branch` is null:
```
git checkout feature/x12viewer-suite
git pull origin feature/x12viewer-suite
git checkout -b phase/<N>-<slug>
git push -u origin phase/<N>-<slug>
```
Write the branch name back to `phases.<N>.branch` in state.

Otherwise:
```
git checkout phase/<N>-<slug>
git pull origin phase/<N>-<slug>
```

Set `current_branch` in state.

## 5. Identify the next criterion to implement

Open `.plans/X12Viewer_Plan1.md` and find the acceptance criteria for the current phase. Cross-reference `phases.<N>.completed_criteria` in state.

Pick the **first AFK criterion not yet in `completed_criteria`**. Skip any criterion marked HITL — those are verified by the human, not implemented by the loop.

If all AFK criteria are in `completed_criteria`, go to step 6 (phase AFK complete).

## 6. Implement the criterion using TDD

Use the `/dotnet-tdd` skill for this step. The cycle is:

**RED**: Write the minimum failing test that targets this criterion. Run `dotnet test --filter <TestName>` and confirm it fails for the right reason.

**GREEN**: Write the minimum production code to make the test pass. Run `dotnet test --filter <TestName>` and confirm it passes.

**REFACTOR**: Clean up duplication or awkward structure without breaking the test. Run `dotnet test` (full suite) and confirm zero failures.

After GREEN+REFACTOR:
```
git add <changed files>
git commit -m "test(phase<N>): <criterion short description>"
git push origin phase/<N>-<slug>
```

Append the criterion description to `phases.<N>.completed_criteria` in state and write the file.

Loop back to step 5.

## 7. Phase AFK complete — open the PR

All AFK criteria are done. Open a PR from `phase/<N>-<slug>` into `feature/x12viewer-suite`:

```
gh pr create \
  --base feature/x12viewer-suite \
  --title "Phase <N>: <slug>" \
  --body "Closes #<issue>\n\n## AFK criteria\nAll passing — see commit history.\n\n## HITL criteria\n<list hitl_criteria or 'None'>"
```

Write the PR number to `phases.<N>.pr` and `current_pr` in state.

**If `has_hitl == false`**:
```
gh pr merge <pr_number> --squash --auto
```
Wait for the merge to complete (poll `gh pr view <pr_number> --json state` up to 60 seconds). When merged, set `status = "complete"`, clear `current_branch` and `current_pr`, write state. Loop back to step 2.

**If `has_hitl == true`**:
Set phase `status = "in_progress"` (waiting for human). Write state. STOP with the HITL message from step 3.

## Stop messages

Always end a STOP with one of these banners so the user knows what happened:

- `[STOPPED: time_limit]` — rerun `/loop <prompt>` to continue
- `[STOPPED: iteration_budget]` — rerun `/loop <prompt>` to continue  
- `[STOPPED: hitl_pending phase <N>]` — verify HITL, merge PR #<number>, then rerun
- `[STOPPED: all_phases_complete]` — feature branch ready; open PR into develop

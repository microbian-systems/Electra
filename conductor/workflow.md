# Conductor Workflow Configuration

## Test Coverage

- **Required Coverage:** 80%
- All new code should include corresponding tests
- Coverage is verified before committing

---

## Commit Strategy

- **Commit Frequency:** After every task
- Small, focused commits that are easy to review and revert
- Each commit should represent a single logical change

---

## Task Summaries

- **Summary Storage:** Git Notes
- Task summaries are recorded in git notes for traceability
- Can be viewed with `git notes show <commit>`

---

## Development Workflow

### For Each Task

1. **Implement** the task
2. **Write tests** for the implementation
3. **Verify** all tests pass
4. **Check** coverage meets threshold
5. **Commit** changes with descriptive message
6. **Record** task summary in git notes

### For Each Phase

1. Complete all tasks in the phase
2. Verify integration between tasks
3. Run full test suite
4. Create phase checkpoint commit

---

## Phase Completion Verification and Checkpointing Protocol

At the end of each phase, before moving to the next:

1. **User Manual Verification**
   - Present completed work to the user
   - User confirms phase is complete and satisfactory
   - Any issues are addressed before proceeding

2. **Checkpoint**
   - All tests passing
   - Coverage threshold met
   - Git state is clean (all changes committed)
   - Phase summary documented

3. **Proceed** to next phase only after user approval

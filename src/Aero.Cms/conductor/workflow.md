# Conductor Workflow

## Development Workflow

### Test-Driven Development
- Write tests before implementation
- Each feature task includes "Write Tests" sub-task followed by "Implement Feature" sub-task
- All tests must pass before phase completion

### Test Coverage Requirements
- Minimum 80% code coverage required
- Coverage targets by module:
  - Core/Shared: 90%
  - Content: 85%
  - Membership: 85%
  - SEO: 80%
  - Media: 80%
  - Plugins: 75%

### Commit Strategy
- Commit changes after each task completion
- Commit message format: `phase(n): task description`
- Use conventional commits style

### Task Summaries
- Use commit message to record task summary
- Include test results in commit body when relevant

## Phase Completion Verification and Checkpointing Protocol

After completing all tasks in a phase, the following verification must be performed:

1. **Build Verification:**
   ```bash
   dotnet build Aero.CMS.sln
   ```
   - Must complete with 0 errors, 0 warnings

2. **Test Verification:**
   ```bash
   dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~<PhaseFilter>"
   dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~<PhaseFilter>"
   ```
   - All tests must pass
   - No skipped or ignored tests

3. **User Manual Verification:**
   - Present phase completion summary to user
   - Request explicit approval before proceeding
   - Document any deviations from spec

## Gate Requirements

### Phase Gates
Each phase has specific gate commands that must pass:
- `dotnet test` with phase-specific filters
- Zero failures permitted
- No `NotImplementedException` or TODO placeholders

### Final Phase Gate (Phase 16)
```bash
dotnet test Aero.CMS.Tests.Unit
dotnet test Aero.CMS.Tests.Integration
dotnet test --collect:"XPlat Code Coverage"
```
- Aero.CMS.Core line coverage MUST be >= 80%

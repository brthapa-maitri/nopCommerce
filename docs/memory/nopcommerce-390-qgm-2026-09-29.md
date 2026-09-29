# Task: nopcommerce-390-qgm (Story 1.1: Expose customer last-login timestamp)

**Status:** IN_PROGRESS (PR open, awaiting review)
**Last Updated:** 2026-09-29T00:00:00Z (PR opened)
**Bead / Branch:** nopcommerce-390-qgm / feat/AT-264-customer-last-login
**Jira:** AT-264
**PR:** https://github.com/brthapa-maitri/nopCommerce/pull/3 (base: mad/retarget-net48)

## Objective
Add an `ICustomerService` method that returns a customer's last-login time
converted to the store timezone via `IDateTimeHelper`, returning null for a
customer who has never logged in. Implementation in
`Nop.Services/Customers/CustomerService.cs`. No EF access from controllers.
Add a `Nop.Services.Tests` case for the never-logged-in path. Unbuildable on
this machine (no MSBuild/dotnet) — code must be correct by inspection.

## Current Step
About to implement:
- `ICustomerService.GetCustomerLastLoginDate(Customer customer)` -> `DateTime?`
- `CustomerService` gains `IDateTimeHelper _dateTimeHelper` field + ctor param
  (appended at end of ctor param list to minimize churn) and the method impl:
  returns null immediately if `customer.LastLoginDateUtc` is null; otherwise
  converts UTC -> `_dateTimeHelper.DefaultStoreTimeZone` via
  `ConvertToUserTime(dt, TimeZoneInfo.Utc, DefaultStoreTimeZone)` (AC says
  "store's timezone", which is `DefaultStoreTimeZone`, not `CurrentTimeZone`
  which is the browsing customer's zone — those can differ from the target
  customer being queried).
- New test file `Nop.Services.Tests/Customers/CustomerServiceTests.cs` covering
  the never-logged-in-returns-null case (plus a happy-path conversion case).
- MUST update existing `CustomerRegistrationServiceTests.cs` — it constructs
  `new CustomerService(...)` positionally with 19 args; adding the 20th ctor
  param breaks that call site unless updated (append `, null` for the new
  `IDateTimeHelper` arg there since that test doesn't exercise the new method).

## Pending Tasks
- [x] Edit `src/Libraries/Nop.Services/Customers/ICustomerService.cs`
- [x] Edit `src/Libraries/Nop.Services/Customers/CustomerService.cs`
- [x] Update `src/Tests/Nop.Services.Tests/Customers/CustomerRegistrationServiceTests.cs`
      ctor call site (add dateTimeHelper arg)
- [x] Create `src/Tests/Nop.Services.Tests/Customers/CustomerServiceTests.cs`
- [x] Added new test file to `Nop.Services.Tests.csproj` (old-style csproj,
      explicit `<Compile Include>` list — easy to miss)
- [x] Commit, push, open PR (base `mad/retarget-net48`), noted unbuilt/unverified
- [ ] Await lead review; merge + close bead (steps 8-11) once approved

## Files Modified
| File | Change | Description |
|------|--------|--------------|
| (not yet written) | | |

## Discoveries & Notes
- No project-local `mad-project-context`/`mad-backend-standards` skills exist
  in `.claude/skills/` — followed generic backend standards + repo's
  `docs/ARCHITECTURE_REFERENCE.md`.
- `CustomerService` ctor already takes ~19 params (13 repos + several
  services/settings); none of them are needed to test the new method, so the
  new test constructs `CustomerService` with `null` for everything except
  `NopNullCache` and a Rhino-mocks `IDateTimeHelper` stub.
- Coordination mode active: claimed via Jira gate
  (`sync_bead.py claim nopcommerce-390-qgm --assignee backend-1`) — got
  CLAIMED (exit 0), AT-264 transitioned to In Progress.
- This is on the critical path for nopcommerce-390-4gs (frontend, Story 1.2).
- Cannot build (no MSBuild/dotnet on this machine) — PR says explicitly
  code is unbuilt/unverified locally, correct by inspection only.
- `gh pr create` defaults to the `upstream` remote (`nopSolutions/nopCommerce`,
  default branch `develop`) instead of `origin` (`brthapa-maitri/nopCommerce`
  fork) even though `origin` is the tracking remote for the pushed branch —
  it failed with "No commits between nopSolutions:mad/retarget-net48 and
  brthapa-maitri:...". Fix: pass `--repo brthapa-maitri/nopCommerce`
  explicitly on `gh pr create` (and presumably other `gh pr`/`gh repo`
  commands) in this checkout. Worth a note for other teammates on this repo.
- PR opened: https://github.com/brthapa-maitri/nopCommerce/pull/3

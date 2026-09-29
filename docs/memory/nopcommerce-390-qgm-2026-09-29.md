# Task: nopcommerce-390-qgm (Story 1.1: Expose customer last-login timestamp)

**Status:** IN_PROGRESS
**Last Updated:** 2026-09-29T00:00:00Z (session start)
**Bead / Branch:** nopcommerce-390-qgm / feat/AT-264-customer-last-login
**Jira:** AT-264

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
- [ ] Edit `src/Libraries/Nop.Services/Customers/ICustomerService.cs`
- [ ] Edit `src/Libraries/Nop.Services/Customers/CustomerService.cs`
- [ ] Update `src/Tests/Nop.Services.Tests/Customers/CustomerRegistrationServiceTests.cs`
      ctor call site (add dateTimeHelper arg)
- [ ] Create `src/Tests/Nop.Services.Tests/Customers/CustomerServiceTests.cs`
- [ ] Commit, push, open PR (base `mad/retarget-net48`), note unbuilt/unverified
- [ ] Notify lead with [PR READY]

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
- Cannot build (no MSBuild/dotnet on this machine) — PR will say explicitly
  code is unbuilt/unverified locally, correct by inspection only.

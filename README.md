# Gym Management System – Assignment 7 (Functional Error Handling)

Builds on Assignments 5 and 6 (pipeline, policies, validated `Client`, explicit `MembershipState`). The earlier pipeline and safe transitions are still active. Build, run and test:
```
dotnet build
dotnet run --project GymSystem
dotnet test
```
.NET SDK 8.0 or newer, working directory = folder with `GymSystem.sln`.

## 1. Types and what they mean
| Type | Meaning |
|---|---|
| `ClientValidation` (Valid / Invalid) | outcome of **checking raw data**; `Invalid` lists all reasons. `ToResult()` converts it to a Result. |
| `MembershipState` (Pending / Confirmed) | **workflow state** of a membership |
| `Result<T,E>` (Success / Failure) | outcome of an **operation** that can fail; replaces the A6 `TransitionOutcome` |
| `Maybe<T>` (Some / None) | a lookup that may find nothing |
| `AppError(Kind, Code, Message)` | the error in every Result; `Kind` = Domain or Operational |

`Result` has a private base constructor (only Success/Failure exist) and `Ok`/`Fail` reject a missing (null) payload. A failure has no `Value`, so the value can not be used by mistake.

## 2. Domain failures (Task 1)
| Situation | Code | Expected success | Error message example |
|---|---|---|---|
| Client name not in the list | `client_not_found` | quote for the client | `No client named 'Zed' was found.` |
| Client younger than 16 | `not_eligible` | Pending membership | `Boris is not eligible: minimum age is 16.` |
| Payment differs from quote | `payment_mismatch` | Confirmed membership | `Payment 29 does not match the quoted total 30.` |
| Confirm without / twice | `invalid_state` | Confirmed membership | `Membership is already confirmed.` |
| Invalid client data (factory) | `invalid_client` | validated `Client` | `Client data is invalid: Name must not be empty. ...` |

Business failures never use exceptions.

## 3. Missing values (Task 2)
`ClientLookup.FindByName` returns `Maybe<Client>`; `FindTrainerSessions` returns `Maybe<int>`. A client with **0 sessions** gives `Some(0)`; only an unknown client gives `None`. `ToResult(error)` turns `None` into `client_not_found` because the client is required for enrollment. No `null`, no `default(T)`, no `!` is used.

## 4. Map and Bind (Task 3) – `Result.cs`, chain in `Enrollment.cs`
```
FindByName            Maybe<Client>
 .ToResult(NotFound)  Result<Client>
 .Bind(Start)         Result<MembershipState>    Start can fail (not_eligible)   -> returns a Result
 .Bind(Confirm)       Result<MembershipState>    Confirm can fail                -> returns a Result
 .Map(Describe)       Result<string>             Describe can not fail           -> returns a plain value
```
`Map` takes `T -> U`, `Bind` takes `T -> Result<U,E>`; that is why their return types differ. Both return the failure unchanged without calling the function. `Program.cs` handles the final result once with `switch` on Success/Failure.

## 5. Exception boundary (Task 4) – `ClientFileLoader.cs`
`Load(path)` is the only place that catches exceptions (`FileNotFoundException`, `DirectoryNotFoundException`, `IOException`, `UnauthorizedAccessException`). It returns:
- `file_not_found`, `file_unreadable` (Operational) with the path in the message,
- `file_malformed` for a wrong/missing header or any invalid line (all bad lines are listed, e.g. `Line 3: ...`).
It returns the complete list of clients or an error, never a partial list. A file with only the header is a **valid empty dataset**. No fallback is used. Domain data is not touched on failure.

## 6. Demonstrations (`dotnet run --project GymSystem`)
Normal path, 4 domain failures, present/absent lookup, missing file, malformed file (`data/clients_malformed.csv`). To see a file failure at start-up: `dotnet run --project GymSystem -- missing.csv`.

## 7. Tests
| Required case | Test(s) |
|---|---|
| Complete success | `Enrollment_CompleteChain_ProducesConfirmedText` |
| Domain failures | `Enrollment_DomainFailures_ReportTheRule` (5 cases), `DomainFailure_UnderMinimumAge_...`, `DomainFailure_ConfirmWithoutMembership_...`, `Boundary_PaymentMustEqualQuotedTotal` |
| Lookup present/absent | `Lookup_FindsPresentClient_AndReportsAbsentOne`, `Lookup_ZeroIsALegitimateValue_NotAbsence` |
| Map success | `Map_Success_TransformsValue` |
| Bind success | `Bind_Success_ReturnsResultOfNextStep` |
| Failure propagation (counter) | `MapAndBind_OnFailure_KeepTheFailure_AndDoNotCallTheFunctions`, `Chain_StopsAtFirstFailure_...`, `MissingLookup_IsConvertedToResultFailure_...` |
| Missing file | `MissingFile_ReportsOperationalFailure_AndCreatesNothing`, `MissingPath_IsReportedAsFailure` |
| Malformed file | `MalformedLines_BecomeFailure_WithoutPartialResult`, `MissingHeader_IsMalformed` |
| Success / empty file | `ValidFile_LoadsAllClients`, `HeaderOnlyFile_IsAValidEmptyDataset` |
| Earlier tests | `PipelineTests`, `ClientFactoryTests`, `MembershipWorkflowTests` (A5/A6) |

File tests use a `TempDir` helper (unique temp folder, deleted in `Dispose`). Latest test outcome: Latest test outcome: Passed! - Failed: 0, Passed: 59, Skipped: 0, Total: 59.

## 8. Team
| Name | Group | Implemented logic | Tests | Documentation |
|---|---|---|---|---|
| Zhubanysh Daniyar | IT-2505 | Result, Map, Bind, File processing, Enrollment | ResultTests, ClientFactoryTests, MembershipWorkflowTests, EnrollmentAndLookupTests, FileBoundaryTests | README.md (before/after example, test cases, team section) |

# Gym Management System – Assignment 8 (Extensions, LINQ, Currying, Lazy Processing, Native C)

Builds on Assignments 5–7: the pipeline, safe states, `Result`/`Maybe` and the file boundary are still active.

## 0. Build, run, test
Prerequisites: .NET SDK 8.0 or newer; a C compiler (gcc, or MSVC `cl` on Windows). Working directory = folder with `GymSystem.sln`.
```
# 1. build the native library FIRST (Linux/macOS/MinGW shell):
sh native/build.sh
#    Windows, "x64 Native Tools Command Prompt for VS":   native\build.bat
# 2. then
dotnet build
dotnet run --project GymSystem
dotnet test
```
A Linux x86-64 build is already included in `native/out/libgymnative.so`. On Windows run `native\build.bat` to create `gymnative.dll`. The binary must have the **same bitness as .NET (64-bit)**.

## 1. Custom extension methods (Task 1) – `QueryExtensions.cs`
| Method | Receiver -> return | Why on this type | Used in |
|---|---|---|---|
| `WhereEligible` | `IEnumerable<Client>` -> `IEnumerable<Client>` | "eligible" (age >= 16) is a rule about clients, reused everywhere | `QuotePipeline.SelectEligible`, `LazyQuotes`, `ReportWorkflow.Build`, `Program` |
| `OrderForReport` | `IEnumerable<Quote>` -> `IOrderedEnumerable<Quote>` | one standard order of quotes (total desc, name) | `QuotePipeline.BuildReport` |
| `CollectAll` | `IEnumerable<Result<T,E>>` -> `Result<ImmutableArray<T>,E>` | combines many Results; first failure wins, later items are not evaluated | `ReportWorkflow.Build` |
None of them changes its source.

## 2. LINQ reports (Task 2) – `Reports.cs`
Both work on the quotes calculated from the CSV file (`data/clients.csv`) and are pure.
| | Report 1: High-value clients | Report 2: Revenue by tier |
|---|---|---|
| User | gym manager | accountant |
| Input | quotes, minimum total (100) | quotes |
| Operators | `Where`, `OrderByDescending`, `ThenBy`, `Select` | `GroupBy`, `Select` (`Count`, `Sum`, `Aggregate`), `OrderByDescending`, `ThenBy` |
| Output | ClientName, Tier, TotalCost | Tier, Clients, Revenue, HighestTotal |
Expected (Standard pricing): Report 1 = Dana 936, Erik 342, Anna 190, Fiona 100 (exactly 100 is included, Chen 30 is not). Report 2 = Premium (1 client, 936, highest 936), Basic (2, 372, 342), Standard (2, 290, 190). Report 2 is exported to `out/revenue_by_tier.csv`.

## 3. Currying (Task 3) – `LoyaltyScoring.cs`
`Score : monthWeight -> sessionWeight -> client -> int`. Partial application gives `Balanced() = Score(2)(3)` and `TrainerFocused() = Score(1)(10)`. The configured function is passed into `ReportWorkflow.Build` (no global setting).
Anna (3 m, 2 s): 12 vs 23; Erik (12 m, 1 s): 27 vs 22 -> the ranking of Anna and Erik flips.

## 4. Deferred execution (Task 4) – `LazyQuotes.cs`
`Generate` validates arguments immediately and returns the iterator `GenerateCore` (`yield return`, one quote per eligible client). Creating it prices nothing; each iteration step prices one client. Before materialisation every new enumeration repeats the work (policy calls: 5, then 10); after `ToArray()` the snapshot is reused without new work. Test probe = a counting policy delegate (not part of production code). `ReportWorkflow.Build` materialises the eligible clients once with `ToImmutableArray()`.

## 5. Resource ownership (Task 5)
| Resource | Owner | Released |
|---|---|---|
| `StreamReader` in `ClientFileReader` | the iterator `ReadCore` (`using`) | when iteration finishes, fails or stops early (enumerator Dispose) |
| `StreamWriter` in `ReportExporter` | `WriteLines` (`using` block) | end of the block, also when an exception is thrown |
`ClientFileLoader.Load` calls `ReadLines(path).ToArray()` inside its `try`: the file is opened and fully read there, the reader is disposed, and only a snapshot array leaves the boundary. File exceptions of the lazy reader appear at enumeration, so they must be caught around the materialisation. The exporter writes `report.csv.tmp` and moves it over the target at the end, so a failure keeps the old file. Garbage collection is not relied on.

## 6. Resource note (Task 6)
- **Materialised data:** file lines (`string[]` in the loader), `ImmutableArray<Client>`, the quote report and report arrays.
- **Deferred queries / closures:** `LazyQuotes` keeps references to the client list and the policy delegate only while the enumerable is alive; it is only used locally in `Program`. Reports are materialised with `ToImmutableArray()` so they do not keep the source sequences.
- **Caches, static collections, event subscriptions:** none are used.
- **Design decision:** the file is read as a stream and released immediately; only the parsed snapshot is kept, so no file handle is retained and the data can be reused without re-reading.
- **Streaming vs snapshot:** streaming needs little memory but the source must stay open and each enumeration repeats the work; a snapshot uses memory but is stable and can be reused. Our files are small, so we use a snapshot for the data and streaming only inside the loader.

## 7. Native C function (Tasks 7–9)
- **Files:** `native/gym_native.h`, `native/gym_native.c`, `native/build.sh`, `native/build.bat`. C# binding: `GymSystem/NativeBonus.cs` (`NativeMethods` with `[DllImport("gymnative")]`, adapter `NativeBonus`).
- **Contract:** `int gym_bonus_points(int months, int trainer_sessions)`; both `int` (32-bit). Valid: months 1–36, sessions 0–1000. Result = `months*10 + sessions*5`, plus 50 if `months >= 12`. Invalid input returns `-1`. Deterministic, no memory allocated, nothing to release. Cdecl calling convention.
- **Manual example:** months 12, sessions 4 -> 120 + 20 + 50 = **190**.
- **Adapter:** validates the domain input first (`invalid_input`), calls the native function, converts `DllNotFoundException`, `EntryPointNotFoundException`, `BadImageFormatException` into `native_unavailable`, and `-1` into `native_rejected`.
- **Used in the workflow:** `ReportWorkflow.Build` calls the adapter for every eligible client; the result is `LoyaltyRow.BonusPoints` in the output.
- **Platform / compiler used for the included binary:** Ubuntu 24.04, x86-64, gcc 13.3.0, `gcc -shared -fPIC -O2 -o native/out/libgymnative.so native/gym_native.c`. Windows: `cl /LD /O2 gym_native.c /Fe:out\gymnative.dll` (MSVC x64) or `gcc -shared -O2 -o out\gymnative.dll gym_native.c` (MinGW-w64).
- **Loading:** `GymSystem.csproj` copies everything in `native/out` next to `GymSystem.dll` (and, through the project reference, to the test output folder). .NET maps the name `gymnative` to `gymnative.dll` / `libgymnative.so` / `libgymnative.dylib`.
- **Integration difficulty (write your own real experience here; typical examples):** (1) `BadImageFormatException` when a 32-bit DLL is loaded by 64-bit .NET -> rebuild with an x64 compiler; (2) `DllNotFoundException` in `dotnet test` because the DLL was not in the test output folder -> copy via `CopyToOutputDirectory` / build the native library before `dotnet test`.
- **Failure test:** an injected function that throws `DllNotFoundException` (a library that is already loaded can not be unloaded by deleting its file).

## 8. Tests (cumulative; 10+ test files/classes, 60+ test cases)
| Area | Tests |
|---|---|
| Reports | `HighValue_NormalResult_...`, `HighValue_EmptyInput_AndNoMatches_...`, `HighValue_TiedTotals_...`, `RevenueByTier_Groups...`, `RevenueByTier_EmptyInput_...` |
| Extensions | `WhereEligible_...`, `OrderForReport_...`, `CollectAll_...` |
| Configured functions | `CurriedScore_TwoPartialApplications_...` |
| Deferred execution | `LazyQuotes_NothingHappensUntilEnumerated_...`, `LazyQuotes_StoppingEarly_...`, `LazyQuotes_ArgumentChecksRunImmediately` |
| Resource lifetime | `Reader_IsNotOpenedBeforeIteration_...`, `Reader_IsDisposed_WhenIterationStopsEarly`, `Reader_IsDisposed_WhenTheConsumerFails`, `FileReader_ReadsRealFile_...`, export tests |
| Native | `RealNativeCall_ReturnsManuallyCalculatedValue`, `InvalidDomainInput_IsRejectedBeforeTheNativeCall`, `LoadingFailure_BecomesOperationalResultFailure`, `NativeErrorValue_...` |
| Complete workflows | `CompleteWorkflow_FileToReportsWithNativeCall_Succeeds`, `..._NativeFailure_...`, `..._MissingFile_...`, `Build_EmptyDataset_...` |
| File boundaries | `FileBoundaryTests` (missing file, malformed content, header only) |
| Earlier tests | A5 `PipelineTests`, A6 `ClientFactoryTests`, A7 `ResultTests`, `EnrollmentAndLookupTests`, `MembershipWorkflowTests` |
All file tests use `TempDir` (unique temp folder, deleted afterwards). Latest test outcome: Latest test outcome: Passed! - Failed: 0, Passed: 93, Skipped: 0, Total: 93.

## 9. Demonstration (`dotnet run --project GymSystem`)
1 load data, 2 both reports + native bonus + export, 3 two configured scores, 4 deferred execution (the policy prints "pricing X" only when iterated), 5 failures: simulated native failure, missing file, malformed file.

## 10. Team
| Name | Group | Implemented logic | Tests | Documentation |
|---|---|---|---|---|
| Zhubanysh Daniyar | IT-2505 | reports, native adapter, LINQ, lazy processing | ReportsAndExtensionsTests, LazyAndResourceTests, NativeAndWorkflowTests | README.md (before/after example, test cases, team section) |
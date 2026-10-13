# Gym Management System – Assignment 5 (Functional Core and Pipeline)

## 1. Workflow (Task 1)
- **Topic:** calculating membership quotes for a fitness gym.
- **Users:** gym administrators.
- **Input (CSV):** `Name,Age,Tier,Months,TrainerSessions` (see `GymSystem/data/clients.csv`).
- **Rules:**
  - Age must be 16 or more (age 16 accepted, 15 rejected).
  - Monthly fee comes from a pricing policy; trainer session costs 20.
  - `gross = fee * months + sessions * 20`
  - 12 months or more -> 10% long-term discount (integer division).
  - Result: quotes sorted by total (highest first, then name) and the total revenue.
- **Policies for the same rule "monthly fee":**
  - `Standard`: Basic 30, Standard 50, Premium 80.
  - `Student`: same, but clients aged 25 or younger get 20% off the fee.

### Documented expected results (written before running)
| Client | Notes | Standard total | Student total |
|---|---|---|---|
| Anna (30, Standard, 3 m, 2 s) | normal | 190 | 190 |
| Boris (15) | too young -> not eligible | – | – |
| Chen (16, Basic, 1 m, 0 s) | boundary age, 0 sessions | 30 | 24 |
| Dana (22, Premium, 12 m, 4 s) | boundary months | 936 | 764 |
| Erik (45, Basic, 12 m, 1 s) | long-term | 342 | 342 |
| Fiona (25, Standard, 2 m, 0 s) | boundary for Student | 100 | 80 |
| Gina `abc` age, Hank tier `Gold` | invalid lines, rejected at parsing | – | – |

Revenue: Standard **1598**, Student **1400**.
Dana by hand (Standard): fee 80, 80*12 + 4*20 = 1040, discount 104, total 936.

## 2. Types and validation (Task 2)
- `Models.cs`: immutable records `Client`, `PricedClient`, `Quote`, `QuoteReport`, `ParsedDataset`. The report uses `ImmutableArray`, so nobody can modify it later.
- `ClientParser.cs`: `TryParse` validates raw text at the boundary (field count, number format, ranges, tier). `0` sessions is valid. Bad lines are reported with line number, never replaced by defaults.

## 3. Pipeline (Task 3) – `QuotePipeline.cs`
| Stage | Method | Input -> Output | Rule | Example |
|---|---|---|---|---|
| 1 | `SelectEligible` | `IEnumerable<Client>` -> `ImmutableArray<Client>` | Age >= 16 | [Boris 15, Chen 16] -> [Chen] |
| 2 | `PriceClients` | clients + `Func<Client,int>` -> `ImmutableArray<PricedClient>` | gross and discount | Dana: fee 80, gross 1040, discount 104 |
| 3 | `BuildReport` | `IEnumerable<PricedClient>` -> `QuoteReport` | quotes, sorting, revenue | Dana 936, Erik 342, ... revenue 1598 |

`QuotePipeline.Run` connects them with ordinary function calls.

## 4. Configurable rule (Task 4)
`PriceClients` is the **higher-order function**; it calls `monthlyFeePolicy(client)` (inside `Price`). `Program.cs` chooses `PricingPolicies.Standard` or `PricingPolicies.Student` and passes it as an argument. Fiona: 100 vs 80 with the same pipeline.

## 5. Purity (Task 5)
Stages have no Console, file, clock or global state, and never modify their input. Console and file access are only in `Program.cs`.

## 6. Tests (`GymSystem.Tests/PipelineTests.cs`)
| Required case | Test |
|---|---|
| Normal workflow | `NormalWorkflow_StandardPolicy_GivesDocumentedResult` |
| Empty dataset | `EmptyDataset_GivesEmptyReport` |
| Boundary | `Boundary_Age16IsEligible_Age15IsNot`, `Boundary_DiscountStartsAtTwelveMonths` |
| Invalid / missing | `InvalidOrMissingLine_IsRejectedWithMessage` (10 cases), `NullInput_IsRejected_NoFabricatedResult`, `ZeroSessionsIsValid_NotTreatedAsMissing`, `ParseLines_SeparatesValidClientsFromErrors` |
| Two policies | `TwoPolicies_SameInput_DifferInDocumentedCases`, `HigherOrderFunction_ActuallyCallsThePolicy_OncePerEligibleClient` |
| Purity and stages | `Stage2_PriceClients_ComputesDocumentedValues`, `Stages_DoNotChangeCallerData_AndGiveSameResultTwice` |

Latest test outcome: _fill in after running `dotnet test` on your machine_.

## 7. How to build, run, test
- .NET SDK 8.0 or newer (`dotnet --version`). Projects target `net8.0` with `RollForward=LatestMajor`.
- Working directory: the folder containing `GymSystem.sln`.
```
dotnet build
dotnet run --project GymSystem
dotnet test
```
Failure demo: the two invalid lines in `clients.csv` are printed as REJECTED; `dotnet run --project GymSystem -- missing.csv` prints "Data file not found".

## 8. Team
| Name | Group | Implemented logic | Tests | Documentation |
|---|---|---|---|---|
| Zhubanysh Daniyar | IT-2505 | Models, ClientParser, PricingPolicies, QuotePipeline, Program | PipelineTests (all 6 required test categories) | README.md (workflow, rules, stage table, build instructions) |

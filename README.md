# Gym Management System – Assignment 6 (Temporal Coupling, Factories, Explicit States)

Builds on Assignment 5 (pipeline, policies, immutable records). Run/build/test commands and the .NET version are the same as in Assignment 5 (see section 8).

## 1. The hidden dependency (Task 1) – UNSAFE example, NOT used by the application
```csharp

class UnsafeMembership
{
    public string ClientName = "";
    public int Age;
    public int Total;
    public bool Validated;
    public string Status = "New";

    public void Validate()  { Validated = Age >= 16 && ClientName != ""; }
    public void Calculate() { Total = 30 * 3; }                
    public void Confirm()   { Status = "Confirmed"; }          
}

var m = new UnsafeMembership { ClientName = "", Age = 5 };
m.Confirm();      
m.Confirm();      
m.Calculate();    
```
- **Prerequisite:** the client must be validated and eligible, a quote must exist, and only then can payment confirm.
- **Required order:** `Validate -> Calculate -> Confirm`.
- **What goes wrong:** skipping or reordering gives an invalid confirmation or a wrong amount, and nothing warns the caller.

## 2. Safe design (AFTER)
```
Client.Create(...)  ->  ClientValidation (Valid | Invalid)
MembershipWorkflow.Start(validation, policy)   ->  TransitionOutcome (Moved(Pending) | Rejected)
MembershipWorkflow.Confirm(state, payment)     ->  TransitionOutcome (Moved(Confirmed) | Rejected)
```
Every step takes the result of the previous step as an argument, so the order is visible in the method signatures and can not be forgotten.

## 3. Validated factory (Task 2) – `Client.cs`
`Client.Create` checks: non-empty name, age 1–120, defined tier, months 1–36, sessions >= 0 (5 rules, all reasons are collected).
**Access restriction:** the constructor is `private` and the properties are get-only (no `init`/`set`), so no unchecked `new Client(...)` and no `with` modification is possible from outside. The factory returns `Valid(client)` or `Invalid(reasons)`, never a half-built object. It is used by `ClientParser.Parse`, `MembershipWorkflow` and `Program`.

## 4. Distinct cases (Task 3)
| Model | Case | Meaning / payload |
|---|---|---|
| `ClientValidation` | `Valid` | validated `Client` |
| | `Invalid` | list of reasons |
| `MembershipState` | `Pending` | quote calculated, waiting for payment (`Quote`) |
| | `Confirmed` | exact payment received (`Quote`, `PaidAmount`) |
| `TransitionOutcome` | `Moved` | the new `MembershipState` |
| | `Rejected` | the reason (text) |

The base records have private constructors, so no other cases can exist. `Pending`/`Confirmed` have `internal` constructors, so `Confirmed` can only be reached through `Confirm`.

## 5. Pattern matching (Task 4)
- `MembershipWorkflow.Start`: switch on `ClientValidation` (Valid -> pipeline, Invalid -> rejection with reasons, anything else -> rejection).
- `MembershipWorkflow.Confirm`: switch on `MembershipState` (Pending + exact payment, Pending + wrong payment, Confirmed, null/unknown -> rejection).
- `MembershipWorkflow.Describe`, `ClientParser.ParseLines`, and `Program.Apply` also match the cases. No casts and no `!` are used in production code.

## 6. Safe operations (Task 5)
- All methods are pure and return new values; states are immutable, so a failed call can not leave a partial update. `Program` replaces its state variable only for `Moved`.
- **Successful:** Anna -> Pending (190) -> Confirmed (paid 190).
- **Invalid cases:** (a) invalid client (empty name, age 0) -> rejected with reasons; (b) Boris is valid but age 15 -> "not eligible"; (c) confirm without a membership (`null`) -> rejected (missing prerequisite); (d) wrong payment -> rejected, state still Pending.
- **Repeated operation rule:** confirming a `Confirmed` membership is **rejected** and the existing state stays unchanged.
- Payment rule: must equal the quoted total exactly (189 and 191 are rejected for 190).
- Calls that are impossible by design: creating `new Client(...)` or `new MembershipState.Confirmed(...)` from outside the project does not compile (constructors are private/internal).

## 7. Tests (cumulative: 25 test methods, more with Theory cases)
| Required case | Test |
|---|---|
| Factory success | `Factory_ValidInput_CreatesClientWithExpectedData` |
| Factory rejection | `Factory_InvalidInput_IsRejectedWithReason` (8 cases), `Factory_NullName_...` |
| Distinct cases | `Parse_ReturnsValidCase_...`, `Parse_ReturnsInvalidCase_ForBadLine`, `DistinctCases_PatternMatchingGivesCaseSpecificResult` |
| Normal operation | `NormalOperation_ValidClient_BecomesPending_ThenConfirmed` |
| Missing prerequisite | `MissingPrerequisite_InvalidClient_...`, `..._UnderMinimumAge_...`, `..._ConfirmWithoutMembership_...` |
| Repeated operation | `RepeatedConfirmation_IsRejected_AndConfirmedStateStaysTheSame` |
| Boundary | `Factory_ExactLimits_AreAccepted`, `Boundary_PaymentMustEqualQuotedTotal` |
| State preservation | `FailedTransition_LeavesOriginalStateUnchanged`, `FailedConstruction_...` |
| Assignment 5 tests (kept) | `PipelineTests.cs` (normal, empty, boundary, two policies, purity, delegate call count) |

Latest test outcome: Latest test outcome: Passed! - Failed: 0, Passed: 40, Skipped: 0, Total: 40.

## 8. Build, run, test
.NET SDK 8.0 or newer. From the folder containing `GymSystem.sln`:
```
dotnet build
dotnet run --project GymSystem      # pipeline reports + membership workflow demo
dotnet test
```

## 9. Team
| Name | Group | Implemented logic | Tests | Documentation |
|---|---|---|---|---|
| Zhubanysh Daniyar | IT-2505 | Client validation, membership states, factory, workflow | ClientFactoryTests, MembershipWorkflowTests | README.md (before/after example, test cases, team section) |

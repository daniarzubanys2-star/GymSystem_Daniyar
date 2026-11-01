using System.Collections.Immutable;
using GymSystem;
using Xunit;

namespace GymSystem.Tests;

// NOTE: the real-call tests need the native library: run native/build.sh (or build.bat) first.
public class NativeAndWorkflowTests
{
    private static readonly string[] CsvLines =
    {
        ClientFileLoader.ExpectedHeader,
        "Anna,30,Standard,3,2", "Boris,15,Basic,6,0", "Chen,16,Basic,1,0",
        "Dana,22,Premium,12,4", "Erik,45,Basic,12,1", "Fiona,25,Standard,2,0"
    };

    // ---------- NATIVE INTEGRATION ----------
    [Theory]
    [InlineData(12, 4, 190)]    // 12*10 + 4*5 + 50 (long-term bonus), calculated by hand
    [InlineData(3, 2, 40)]      // 30 + 10
    [InlineData(11, 0, 110)]    // just below 12 months: no bonus
    [InlineData(36, 1000, 5410)]// upper limits: 360 + 5000 + 50
    public void RealNativeCall_ReturnsManuallyCalculatedValue(int months, int sessions, int expected)
    {
        var result = NativeBonus.Calculate(months, sessions);   // calls the C function through P/Invoke

        Assert.Equal(expected, Assert.IsType<Result<int, AppError>.Success>(result).Value);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(37, 0)]
    [InlineData(5, -1)]
    [InlineData(5, 1001)]
    public void InvalidDomainInput_IsRejectedBeforeTheNativeCall(int months, int sessions)
    {
        int nativeCalls = 0;

        var result = NativeBonus.Calculate(months, sessions, (m, s) => { nativeCalls++; return 0; });

        Assert.Equal("invalid_input", Assert.IsType<Result<int, AppError>.Failure>(result).Error.Code);
        Assert.Equal(0, nativeCalls);
    }

    [Fact]
    public void LoadingFailure_BecomesOperationalResultFailure()
    {
        var result = NativeBonus.Calculate(12, 4, (_, _) => throw new DllNotFoundException("gymnative"));

        var failure = Assert.IsType<Result<int, AppError>.Failure>(result);
        Assert.Equal("native_unavailable", failure.Error.Code);
        Assert.Equal(ErrorKind.Operational, failure.Error.Kind);
        Assert.Contains("DllNotFoundException", failure.Error.Message);
    }

    [Fact]
    public void NativeErrorValue_BecomesDomainFailure()
    {
        var result = NativeBonus.Calculate(12, 4, (_, _) => -1);

        Assert.Equal("native_rejected", Assert.IsType<Result<int, AppError>.Failure>(result).Error.Code);
    }

    // ---------- COMPLETE WORKFLOWS ----------
    [Fact]
    public void CompleteWorkflow_FileToReportsWithNativeCall_Succeeds()
    {
        using var dir = new TempDir();
        string path = dir.Write("clients.csv", CsvLines);

        var result = ReportWorkflow.Run(path, PricingPolicies.Standard, LoyaltyScoring.Balanced(), 100);

        ReportBundle bundle = Assert.IsType<Result<ReportBundle, AppError>.Success>(result).Value;
        Assert.Equal(1598, bundle.Quotes.TotalRevenue);
        Assert.Equal(4, bundle.HighValue.Length);
        Assert.Equal(MembershipTier.Premium, bundle.ByTier[0].Tier);

        LoyaltyRow top = bundle.Loyalty[0];                  // ordered by balanced score
        Assert.Equal("Dana", top.ClientName);
        Assert.Equal(36, top.Score);                         // 12*2 + 4*3
        Assert.Equal(190, top.BonusPoints);                  // from the native C function
        Assert.Equal(new[] { "Dana", "Erik", "Anna", "Fiona", "Chen" }, bundle.Loyalty.Select(r => r.ClientName));
    }

    [Fact]
    public void CompleteWorkflow_NativeFailure_ReportsTheProblem_AndInputStaysUnchanged()
    {
        using var dir = new TempDir();
        string path = dir.Write("clients.csv", CsvLines);
        string before = File.ReadAllText(path);

        var result = ReportWorkflow.Run(path, PricingPolicies.Standard, LoyaltyScoring.Balanced(), 100,
            (m, s) => NativeBonus.Calculate(m, s, (_, _) => throw new DllNotFoundException("gymnative")));

        Assert.Equal("native_unavailable", Assert.IsType<Result<ReportBundle, AppError>.Failure>(result).Error.Code);
        Assert.Equal(before, File.ReadAllText(path));
    }

    [Fact]
    public void CompleteWorkflow_MissingFile_IsAFailureAndTheNativeFunctionIsNeverCalled()
    {
        using var dir = new TempDir();
        int nativeCalls = 0;

        var result = ReportWorkflow.Run(dir.PathOf("nope.csv"), PricingPolicies.Standard, LoyaltyScoring.Balanced(), 100,
            (m, s) => { nativeCalls++; return Result<int, AppError>.Ok(1); });

        Assert.Equal("file_not_found", Assert.IsType<Result<ReportBundle, AppError>.Failure>(result).Error.Code);
        Assert.Equal(0, nativeCalls);
    }

    [Fact]
    public void Build_EmptyDataset_GivesEmptyReports()
    {
        var result = ReportWorkflow.Build(new List<Client>(), PricingPolicies.Standard, LoyaltyScoring.Balanced(), 100,
            NativeBonus.Calculate);

        ReportBundle bundle = Assert.IsType<Result<ReportBundle, AppError>.Success>(result).Value;
        Assert.True(bundle.Quotes.Quotes.IsEmpty);
        Assert.True(bundle.HighValue.IsEmpty);
        Assert.True(bundle.ByTier.IsEmpty);
        Assert.True(bundle.Loyalty.IsEmpty);
    }
}

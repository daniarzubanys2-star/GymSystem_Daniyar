using GymSystem;
using Xunit;

namespace GymSystem.Tests;

public class LazyAndResourceTests
{
    private sealed class TrackingReader : StringReader
    {
        public bool IsDisposed { get; private set; }
        public TrackingReader(string text) : base(text) { }
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    // ---------- DEFERRED EXECUTION ----------
    [Fact]
    public void LazyQuotes_NothingHappensUntilEnumerated_AndEachEnumerationRepeatsWork()
    {
        int policyCalls = 0;
        Func<Client, int> probe = c => { policyCalls++; return PricingPolicies.Standard(c); };

        IEnumerable<Quote> lazy = LazyQuotes.Generate(TestData.Sample(), probe);
        Assert.Equal(0, policyCalls);                    // creating the enumerable did not run the body

        Quote[] snapshot = lazy.ToArray();               // deliberate materialisation point
        Assert.Equal(5, policyCalls);                    // 5 eligible clients priced during iteration
        Assert.Equal(5, snapshot.Length);

        _ = snapshot.Sum(q => q.TotalCost);              // reuse of the snapshot: no more work
        Assert.Equal(5, policyCalls);

        _ = lazy.ToArray();                              // enumerating the lazy sequence again repeats the work
        Assert.Equal(10, policyCalls);
    }

    [Fact]
    public void LazyQuotes_StoppingEarly_PricesOnlyWhatWasRequested_AndMatchesTheEagerPipeline()
    {
        int policyCalls = 0;
        Func<Client, int> probe = c => { policyCalls++; return PricingPolicies.Standard(c); };

        Quote first = LazyQuotes.Generate(TestData.Sample(), probe).First();

        Assert.Equal(1, policyCalls);
        Assert.Equal("Anna", first.ClientName);          // first eligible client in input order

        var eager = QuotePipeline.Run(TestData.Sample(), PricingPolicies.Standard).Quotes.OrderBy(q => q.ClientName);
        var lazyAll = LazyQuotes.Generate(TestData.Sample(), PricingPolicies.Standard).OrderBy(q => q.ClientName);
        Assert.True(eager.SequenceEqual(lazyAll));
    }

    [Fact]
    public void LazyQuotes_ArgumentChecksRunImmediately()
    {
        Assert.Throws<ArgumentNullException>(() => LazyQuotes.Generate(null!, PricingPolicies.Standard));
        Assert.Throws<ArgumentNullException>(() => LazyQuotes.Generate(TestData.Sample(), null!));
    }

    // ---------- RESOURCE LIFETIME ----------
    [Fact]
    public void Reader_IsNotOpenedBeforeIteration_AndDisposedAfterFullIteration()
    {
        var reader = new TrackingReader("a\nb\nc");
        int opened = 0;

        IEnumerable<string> lines = ClientFileReader.ReadLines(() => { opened++; return reader; });
        Assert.Equal(0, opened);

        Assert.Equal(new[] { "a", "b", "c" }, lines.ToArray());
        Assert.Equal(1, opened);
        Assert.True(reader.IsDisposed);
    }

    [Fact]
    public void Reader_IsDisposed_WhenIterationStopsEarly()
    {
        var reader = new TrackingReader("a\nb\nc");

        string first = ClientFileReader.ReadLines(() => reader).First();   // stops after the first line

        Assert.Equal("a", first);
        Assert.True(reader.IsDisposed);
    }

    [Fact]
    public void Reader_IsDisposed_WhenTheConsumerFails()
    {
        var reader = new TrackingReader("a\nb");

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (string line in ClientFileReader.ReadLines(() => reader))
                throw new InvalidOperationException("consumer failed on " + line);
        });

        Assert.True(reader.IsDisposed);
    }

    [Fact]
    public void FileReader_ReadsRealFile_AndLeavesItFreeAfterwards()
    {
        using var dir = new TempDir();
        string path = dir.Write("lines.txt", "x", "y");

        string[] lines = ClientFileReader.ReadLines(path).ToArray();
        File.Delete(path);                                  // would fail if the handle were still open on Windows

        Assert.Equal(new[] { "x", "y" }, lines);
        Assert.False(File.Exists(path));
    }

    // ---------- EXPORT (StreamWriter owner) ----------
    [Fact]
    public void Export_WritesAllLines_AndLeavesNoTemporaryFile()
    {
        using var dir = new TempDir();
        string target = dir.PathOf("report.csv");

        var result = ReportExporter.WriteLines(target, ReportFormatter.TierSummaryLines(
            Reports.RevenueByTier(QuotePipeline.Run(TestData.Sample(), PricingPolicies.Standard).Quotes)));

        Assert.IsType<Result<string, AppError>.Success>(result);
        string[] written = File.ReadAllLines(target);
        Assert.Equal("Tier,Clients,Revenue,HighestTotal", written[0]);
        Assert.Equal("Premium,1,936,936", written[1]);
        Assert.False(File.Exists(target + ".tmp"));
    }

    [Fact]
    public void Export_ToMissingFolder_IsAFailure()
    {
        using var dir = new TempDir();
        string target = Path.Combine(dir.Folder, "missing_folder", "report.csv");

        var failure = Assert.IsType<Result<string, AppError>.Failure>(ReportExporter.WriteLines(target, new[] { "a" }));

        Assert.Equal("export_failed", failure.Error.Code);
        Assert.Equal(ErrorKind.Operational, failure.Error.Kind);
    }

    [Fact]
    public void Export_FailingInTheMiddle_KeepsOldFileAndRemovesTemporaryFile()
    {
        using var dir = new TempDir();
        string target = dir.Write("report.csv", "old content");

        static IEnumerable<string> BrokenLines()
        {
            yield return "new line";
            throw new IOException("disk problem");
        }

        var failure = Assert.IsType<Result<string, AppError>.Failure>(ReportExporter.WriteLines(target, BrokenLines()));

        Assert.Equal("export_failed", failure.Error.Code);
        Assert.Equal(new[] { "old content" }, File.ReadAllLines(target));   // consistent state
        Assert.False(File.Exists(target + ".tmp"));
    }
}

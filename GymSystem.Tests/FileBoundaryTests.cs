using System.Collections.Immutable;
using GymSystem;
using Xunit;

namespace GymSystem.Tests;

public class FileBoundaryTests
{
    private const string Header = ClientFileLoader.ExpectedHeader;

    // MISSING FILE
    [Fact]
    public void MissingFile_ReportsOperationalFailure_AndCreatesNothing()
    {
        using var dir = new TempDir();
        string path = dir.PathOf("nope.csv");

        var result = ClientFileLoader.Load(path);

        var failure = Assert.IsType<Result<ImmutableArray<Client>, AppError>.Failure>(result);
        Assert.Equal("file_not_found", failure.Error.Code);
        Assert.Equal(ErrorKind.Operational, failure.Error.Kind);
        Assert.Contains(path, failure.Error.Message);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void MissingPath_IsReportedAsFailure()
    {
        var failure = Assert.IsType<Result<ImmutableArray<Client>, AppError>.Failure>(ClientFileLoader.Load(null));
        Assert.Equal("file_not_found", failure.Error.Code);
    }

    // MALFORMED FILE
    [Fact]
    public void MalformedLines_BecomeFailure_WithoutPartialResult()
    {
        using var dir = new TempDir();
        string path = dir.Write("bad.csv", Header, "Anna,30,Standard,3,2", "Gina,abc,Basic,3,0");

        var result = ClientFileLoader.Load(path);

        var failure = Assert.IsType<Result<ImmutableArray<Client>, AppError>.Failure>(result);
        Assert.Equal("file_malformed", failure.Error.Code);
        Assert.Contains("Line 3", failure.Error.Message);   // the valid Anna line is NOT returned
    }

    [Fact]
    public void MissingHeader_IsMalformed()
    {
        using var dir = new TempDir();
        string path = dir.Write("noheader.csv", "Anna,30,Standard,3,2");

        var failure = Assert.IsType<Result<ImmutableArray<Client>, AppError>.Failure>(ClientFileLoader.Load(path));
        Assert.Equal("file_malformed", failure.Error.Code);
    }

    // SUCCESS and VALID EMPTY DATASET
    [Fact]
    public void ValidFile_LoadsAllClients()
    {
        using var dir = new TempDir();
        string path = dir.Write("ok.csv", Header, "Anna,30,Standard,3,2", "Chen,16,Basic,1,0");

        var success = Assert.IsType<Result<ImmutableArray<Client>, AppError>.Success>(ClientFileLoader.Load(path));

        Assert.Equal(2, success.Value.Length);
        Assert.Equal(0, success.Value[1].TrainerSessions);
    }

    [Fact]
    public void HeaderOnlyFile_IsAValidEmptyDataset()
    {
        using var dir = new TempDir();
        string path = dir.Write("empty.csv", Header);

        var success = Assert.IsType<Result<ImmutableArray<Client>, AppError>.Success>(ClientFileLoader.Load(path));

        Assert.True(success.Value.IsEmpty);
    }
}

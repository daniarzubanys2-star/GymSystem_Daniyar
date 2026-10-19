using System.Collections.Immutable;
using ClientsResult = GymSystem.Result<System.Collections.Immutable.ImmutableArray<GymSystem.Client>, GymSystem.AppError>;

namespace GymSystem;

// EXCEPTION BOUNDARY: the only place where file exceptions are caught.
// File access stays outside the pure domain functions.
public static class ClientFileLoader
{
    public const string ExpectedHeader = "Name,Age,Tier,Months,TrainerSessions";

    // Success: all lines are valid (a file with only the header is a valid EMPTY dataset).
    // Failure: missing/unreadable file or any malformed line. Nothing is returned partially:
    // either the complete list of clients or an error.
    public static ClientsResult Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ClientsResult.Fail(AppError.FileNotFound("(no path given)"));

        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (FileNotFoundException)
        {
            return ClientsResult.Fail(AppError.FileNotFound(path));
        }
        catch (DirectoryNotFoundException)
        {
            return ClientsResult.Fail(AppError.FileNotFound(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ClientsResult.Fail(AppError.FileUnreadable(path, ex.Message));
        }

        if (lines.Length == 0 || lines[0].Trim() != ExpectedHeader)
            return ClientsResult.Fail(AppError.FileMalformed(path, $"the first line must be the header '{ExpectedHeader}'."));

        ParsedDataset parsed = ClientParser.ParseLines(lines.Skip(1), firstLineNumber: 2);

        if (!parsed.Errors.IsEmpty)
            return ClientsResult.Fail(AppError.FileMalformed(path, string.Join(" | ", parsed.Errors)));

        return ClientsResult.Ok(parsed.Clients);
    }
}

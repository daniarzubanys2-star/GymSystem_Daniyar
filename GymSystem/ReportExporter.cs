namespace GymSystem;

// Boundary: writes a file. Resource OWNER: this method (the StreamWriter).
// The data is written to a temporary file first and moved over the target only after
// everything was written, so a failure never leaves a half-written or damaged report.
public static class ReportExporter
{
    public static Result<string, AppError> WriteLines(string? path, IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (string.IsNullOrWhiteSpace(path))
            return Result<string, AppError>.Fail(AppError.ExportFailed("(no path given)", "path is missing."));

        string tempPath = path + ".tmp";
        try
        {
            using (var writer = new StreamWriter(tempPath))   // disposed at the end of the block, also on exception
            {
                foreach (string line in lines)
                    writer.WriteLine(line);
            }
            File.Move(tempPath, path, overwrite: true);
            return Result<string, AppError>.Ok(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result<string, AppError>.Fail(AppError.ExportFailed(path, ex.Message + RemoveTemp(tempPath)));
        }
    }

    // Cleanup after a failure. A problem while cleaning up is reported too, not hidden.
    private static string RemoveTemp(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            return "";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $" (temporary file {tempPath} could not be removed: {ex.Message})";
        }
    }
}

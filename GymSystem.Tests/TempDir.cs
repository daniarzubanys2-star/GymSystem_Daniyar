namespace GymSystem.Tests;

// Controlled temporary folder for file tests; everything is deleted in Dispose.
public sealed class TempDir : IDisposable
{
    public string Folder { get; } = Path.Combine(Path.GetTempPath(), "gymtests_" + Guid.NewGuid().ToString("N"));

    public TempDir() => Directory.CreateDirectory(Folder);

    public string PathOf(string fileName) => Path.Combine(Folder, fileName);

    public string Write(string fileName, params string[] lines)
    {
        string path = PathOf(fileName);
        File.WriteAllLines(path, lines);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(Folder))
            Directory.Delete(Folder, recursive: true);
    }
}

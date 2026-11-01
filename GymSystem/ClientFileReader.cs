namespace GymSystem;

// Streams the lines of a file. Resource OWNER: the iterator below.
// The reader is created at the first MoveNext and disposed when the iteration
// finishes, throws, or is stopped early (foreach/First() dispose the enumerator, which runs "using").
public static class ClientFileReader
{
    public static IEnumerable<string> ReadLines(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return ReadLines(() => new StreamReader(path));
    }

    // The reader factory is a parameter, so tests can pass a reader that records its disposal.
    public static IEnumerable<string> ReadLines(Func<TextReader> openReader)
    {
        ArgumentNullException.ThrowIfNull(openReader);
        return ReadCore(openReader);
    }

    private static IEnumerable<string> ReadCore(Func<TextReader> openReader)
    {
        using TextReader reader = openReader();
        string? line;
        while ((line = reader.ReadLine()) != null)
            yield return line;
    }
}

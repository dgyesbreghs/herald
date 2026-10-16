namespace Herald.Tests;

/// <summary>Records the order in which handlers and behaviors run.</summary>
public sealed class CallLog
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries;

    public void Add(string entry) => _entries.Add(entry);
}

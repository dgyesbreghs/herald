namespace Herald;

/// <summary>
/// Represents a void response.
/// </summary>
public readonly record struct Unit
{
    /// <summary>
    /// The single value of <see cref="Unit"/>.
    /// </summary>
    public static readonly Unit Value;

    /// <summary>
    /// A completed task returning <see cref="Value"/>.
    /// </summary>
    public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(Value);
}

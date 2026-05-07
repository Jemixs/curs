namespace SportClub.Domain;

// ─────────────────────────────────────────────────────────────
//  Result<T>  —  standardised service response wrapper
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Represents the outcome of a service operation.
/// <para>
/// On success: <see cref="IsSuccess"/> is true and <see cref="Value"/> holds the result.
/// On failure: <see cref="IsSuccess"/> is false and <see cref="Errors"/> contains user-facing messages.
/// </para>
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();

    // ── Factory methods ──────────────────────────────────────

    public static Result<T> Ok(T value) =>
        new() { IsSuccess = true, Value = value };

    public static Result<T> Fail(string error) =>
        new() { IsSuccess = false, Errors = new[] { error } };

    public static Result<T> Fail(IEnumerable<string> errors) =>
        new() { IsSuccess = false, Errors = errors.ToArray() };

    public static Result<T> Fail(Exception ex) =>
        Fail(ex.Message);

    // ── Implicit conversion from value (syntactic sugar) ─────
    public static implicit operator Result<T>(T value) => Ok(value);

    // ── Helpers ──────────────────────────────────────────────
    public string FirstError => Errors.Count > 0 ? Errors[0] : string.Empty;

    /// <summary>Throws <see cref="InvalidOperationException"/> if the result is a failure.</summary>
    public T GetValueOrThrow() =>
        IsSuccess && Value is not null
            ? Value
            : throw new InvalidOperationException($"Result is a failure: {FirstError}");

    public override string ToString() =>
        IsSuccess ? $"Ok({Value})" : $"Fail([{string.Join(", ", Errors)}])";
}

/// <summary>
/// Non-generic variant for operations that return no data (e.g., update, delete).
/// </summary>
public sealed class Result
{
    public bool IsSuccess { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();

    public static Result Ok() => new() { IsSuccess = true };

    public static Result Fail(string error) =>
        new() { IsSuccess = false, Errors = new[] { error } };

    public static Result Fail(IEnumerable<string> errors) =>
        new() { IsSuccess = false, Errors = errors.ToArray() };

    public static Result Fail(Exception ex) => Fail(ex.Message);

    public string FirstError => Errors.Count > 0 ? Errors[0] : string.Empty;

    public override string ToString() =>
        IsSuccess ? "Ok" : $"Fail([{string.Join(", ", Errors)}])";
}

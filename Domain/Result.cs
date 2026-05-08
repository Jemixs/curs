namespace SportClub.Domain;

// Result<T> — обгортка для результатів роботи сервісів

public sealed class Result<T>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();

    // Методи створення

    public static Result<T> Ok(T value) =>
        new() { IsSuccess = true, Value = value };

    public static Result<T> Fail(string error) =>
        new() { IsSuccess = false, Errors = new[] { error } };

    public static Result<T> Fail(IEnumerable<string> errors) =>
        new() { IsSuccess = false, Errors = errors.ToArray() };

    public static Result<T> Fail(Exception ex) =>
        Fail(ex.Message);

    // Неявне перетворення типів
    public static implicit operator Result<T>(T value) => Ok(value);

    // Допоміжні методи
    public string FirstError => Errors.Count > 0 ? Errors[0] : string.Empty;

    public T GetValueOrThrow() =>
        IsSuccess && Value is not null
            ? Value
            : throw new InvalidOperationException($"Result is a failure: {FirstError}");

    public override string ToString() =>
        IsSuccess ? $"Ok({Value})" : $"Fail([{string.Join(", ", Errors)}])";
}

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

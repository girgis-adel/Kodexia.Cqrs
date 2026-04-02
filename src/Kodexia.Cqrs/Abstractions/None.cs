namespace Kodexia.Cqrs;

/// <summary>
/// Represents the absence of a meaningful response.
/// Similar to 'void' but for asynchronous message processing.
/// </summary>
public readonly struct None : IEquatable<None>, IComparable<None>, IComparable
{
    private static readonly None _value = new();

    /// <summary>Gets the singleton value.</summary>
    public static ref readonly None Value => ref _value;

    /// <summary>Gets a pre-completed Task.</summary>
    public static Task<None> Task { get; } = System.Threading.Tasks.Task.FromResult(_value);

    public int CompareTo(None other) => 0;
    int IComparable.CompareTo(object? obj) => 0;
    public override int GetHashCode() => 0;
    public bool Equals(None other) => true;
    public override bool Equals(object? obj) => obj is None;
    public static bool operator ==(None first, None second) => true;
    public static bool operator !=(None first, None second) => false;
    public override string ToString() => "[]";
}

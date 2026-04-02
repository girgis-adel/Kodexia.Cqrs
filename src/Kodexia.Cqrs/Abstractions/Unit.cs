namespace Kodexia.Cqrs;

/// <summary>
/// Represents a void-equivalent return value for request handlers that produce no meaningful result.
/// Use <see cref="Unit"/> as the <c>TResponse</c> when a handler logically returns <c>void</c>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Unit"/> is a <see href="https://en.wikipedia.org/wiki/Unit_type">unit type</see>
/// from functional programming — a type with exactly one value.
/// </para>
/// <para>
/// Use <see cref="Value"/> to obtain the singleton instance, or <see cref="Task"/> to
/// obtain a pre-completed <see cref="System.Threading.Tasks.Task{TResult}"/> of <see cref="Unit"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // IRequest handlers that do not return a meaningful value can use Unit:
/// public class MyHandler : IRequestHandler&lt;MyCommand, Unit&gt;
/// {
///     public Task&lt;Unit&gt; HandleAsync(MyCommand request, CancellationToken ct)
///         => Unit.Task;
/// }
/// </code>
/// </example>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
    private static readonly Unit _value = new();

    /// <summary>Gets the singleton <see cref="Unit"/> value.</summary>
    public static ref readonly Unit Value => ref _value;

    /// <summary>Gets a pre-completed <see cref="System.Threading.Tasks.Task{TResult}"/> that returns <see cref="Unit"/>.</summary>
    public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(_value);

    /// <inheritdoc />
    public int CompareTo(Unit other) => 0;

    /// <inheritdoc />
    int IComparable.CompareTo(object? obj) => 0;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public bool Equals(Unit other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>Always returns <see langword="true"/>; all <see cref="Unit"/> values are equal.</summary>
    public static bool operator ==(Unit first, Unit second) => true;

    /// <summary>Always returns <see langword="false"/>; all <see cref="Unit"/> values are equal.</summary>
    public static bool operator !=(Unit first, Unit second) => false;

    /// <summary>Returns the string representation <c>"()"</c>.</summary>
    public override string ToString() => "()";
}

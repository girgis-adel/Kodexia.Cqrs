namespace Kodexia.Cqrs.Internal;

internal sealed class ObjectDetails : IComparer<ObjectDetails>
{
    public string Name { get; }
    public string? AssemblyName { get; }
    public string? Location { get; }
    public object Value { get; }
    public Type Type { get; }
    public bool IsOverridden { get; set; }

    public ObjectDetails(object value)
    {
        Value = value;
        Type = value.GetType();
        Name = Type.Name;
        AssemblyName = Type.Assembly.GetName().Name;
        Location = Type.Namespace?.Replace($"{AssemblyName}.", string.Empty);
    }

    public int Compare(ObjectDetails? x, ObjectDetails? y)
    {
        if (x is null) return 1;
        if (y is null) return -1;
        return CompareByAssembly(x, y) ?? CompareByNamespace(x, y) ?? CompareByLocation(x, y);
    }

    private int? CompareByAssembly(ObjectDetails x, ObjectDetails y)
    {
        var xMatch = x.AssemblyName == AssemblyName;
        var yMatch = y.AssemblyName == AssemblyName;

        if (xMatch && !yMatch) return -1;
        if (!xMatch && yMatch) return 1;
        if (!xMatch && !yMatch) return 0;
        return null;
    }

    private int? CompareByNamespace(ObjectDetails x, ObjectDetails y)
    {
        if (Location is null || x.Location is null || y.Location is null)
            return 0;

        var xMatch = x.Location.StartsWith(Location, StringComparison.Ordinal);
        var yMatch = y.Location.StartsWith(Location, StringComparison.Ordinal);

        if (xMatch && !yMatch) return -1;
        if (!xMatch && yMatch) return 1;
        if (xMatch && yMatch) return 0;
        return null;
    }

    private int CompareByLocation(ObjectDetails x, ObjectDetails y)
    {
        if (Location is null || x.Location is null || y.Location is null)
            return 0;

        var xMatch = Location.StartsWith(x.Location, StringComparison.Ordinal);
        var yMatch = Location.StartsWith(y.Location, StringComparison.Ordinal);

        if (xMatch && !yMatch) return -1;
        if (!xMatch && yMatch) return 1;
        if (x.Location.Length > y.Location.Length) return -1;
        if (x.Location.Length < y.Location.Length) return 1;
        return 0;
    }
}

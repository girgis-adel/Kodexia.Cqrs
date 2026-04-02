namespace Kodexia.Cqrs.Tests.Abstractions;

public class UnitTests
{
    [Fact]
    public void Value_ReturnsSingletonInstance()
    {
        var a = Unit.Value;
        var b = Unit.Value;

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Equals_AlwaysReturnsTrue()
    {
        var a = new Unit();
        var b = Unit.Value;

        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
    }

    [Fact]
    public void Equals_NonUnit_ReturnsFalse()
    {
        var a = Unit.Value;

        a.Equals("not-unit").Should().BeFalse();
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_AlwaysReturnsZero()
    {
        Unit.Value.GetHashCode().Should().Be(0);
        new Unit().GetHashCode().Should().Be(0);
    }

    [Fact]
    public void CompareTo_AlwaysReturnsZero()
    {
        var a = new Unit();
        var b = Unit.Value;

        a.CompareTo(b).Should().Be(0);
        ((IComparable)a).CompareTo(b).Should().Be(0);
    }

    [Fact]
    public void ToString_ReturnsUnitLiteral()
    {
        Unit.Value.ToString().Should().Be("()");
    }

    [Fact]
    public async Task Task_ReturnsCompletedTaskWithUnitValue()
    {
        var result = await Unit.Task;

        result.Should().Be(Unit.Value);
        Unit.Task.IsCompleted.Should().BeTrue();
    }
}

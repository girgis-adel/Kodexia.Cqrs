namespace Kodexia.Cqrs.Tests.Abstractions;

public class NoneTests
{
    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingTwoNoneValues()
    {
        var unit1 = None.Value;
        var unit2 = new None();

        unit1.Equals(unit2).Should().BeTrue();
        (unit1 == unit2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithNull()
    {
        None.Value.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldReturnZero()
    {
        None.Value.GetHashCode().Should().Be(0);
    }

    [Fact]
    public void ToString_ShouldReturnRepresentation()
    {
        None.Value.ToString().Should().Be("[]");
    }

    [Fact]
    public void Task_ShouldBeCompletedTask()
    {
        var task = None.Task;
        task.IsCompleted.Should().BeTrue();
        task.Result.Should().Be(None.Value);
    }

    [Fact]
    public void CompareTo_ShouldReturnZero()
    {
        None.Value.CompareTo(new None()).Should().Be(0);
        ((IComparable)None.Value).CompareTo(new None()).Should().Be(0);
    }
}

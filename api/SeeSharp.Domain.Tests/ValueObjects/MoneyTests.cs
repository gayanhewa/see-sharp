using SeeSharp.Domain.ValueObjects;

namespace SeeSharp.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void From_WithPositiveAmount_StoresAmount()
    {
        var money = Money.From(10.50m);
        Assert.Equal(10.50m, money.Amount);
    }

    [Fact]
    public void From_WithNegativeAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.From(-1m));
    }

    [Fact]
    public void Add_SumsAmounts()
    {
        var result = Money.From(2m) + Money.From(3m);
        Assert.Equal(5m, result.Amount);
    }

    [Fact]
    public void Zero_IsZeroAmount()
    {
        Assert.Equal(0m, Money.Zero.Amount);
    }
}

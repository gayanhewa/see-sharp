using SeeSharp.Domain.Entities;

namespace SeeSharp.Domain.Tests.Entities;

public class ExpenseTests
{
    [Fact]
    public void Create_SetsFieldsAndGeneratesId()
    {
        var date = new DateOnly(2026, 7, 25);
        var expense = Expense.Create("Domain renewal", 12.00m, date, "Namecheap", null);

        Assert.NotEqual(Guid.Empty, expense.Id);
        Assert.Equal("Domain renewal", expense.Description);
        Assert.Equal(12.00m, expense.Amount);
        Assert.Equal(date, expense.Date);
        Assert.Equal("Namecheap", expense.Vendor);
        Assert.Null(expense.CategoryId);
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Expense.Create("Bad", -5m, new DateOnly(2026, 1, 1), null, null));
    }
}

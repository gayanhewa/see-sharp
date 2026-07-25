using SeeSharp.Domain.Entities;
using SeeSharp.Domain.Enums;
using SeeSharp.Domain.Exceptions;

namespace SeeSharp.Domain.Tests.Entities;

public class InvoiceTests
{
    private static Invoice NewDraft()
    {
        var invoice = Invoice.Create(
            Guid.NewGuid(), "INV-001",
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), null);
        return invoice;
    }

    [Fact]
    public void Create_StartsInDraft()
    {
        Assert.Equal(InvoiceStatus.Draft, NewDraft().Status);
    }

    [Fact]
    public void Total_SumsLineItems()
    {
        var invoice = NewDraft();
        invoice.AddLineItem("Design", 2, 100m);
        invoice.AddLineItem("Hosting", 1, 25m);
        Assert.Equal(225m, invoice.Total);
    }

    [Fact]
    public void MarkAsPaid_FromDraft_Throws()
    {
        var invoice = NewDraft();
        Assert.Throws<InvalidInvoiceTransitionException>(() => invoice.MarkAsPaid());
    }

    [Fact]
    public void MarkAsPaid_FromSent_Succeeds()
    {
        var invoice = NewDraft();
        invoice.MarkAsSent();
        invoice.MarkAsPaid();
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void AddLineItem_AfterSent_Throws()
    {
        var invoice = NewDraft();
        invoice.MarkAsSent();
        Assert.Throws<InvalidOperationException>(() => invoice.AddLineItem("Late", 1, 10m));
    }

    [Fact]
    public void MarkAsSent_Twice_Throws()
    {
        var invoice = NewDraft();
        invoice.MarkAsSent();
        Assert.Throws<InvalidInvoiceTransitionException>(() => invoice.MarkAsSent());
    }
}

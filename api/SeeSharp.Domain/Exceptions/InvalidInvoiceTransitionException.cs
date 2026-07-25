using SeeSharp.Domain.Enums;

namespace SeeSharp.Domain.Exceptions;

public sealed class InvalidInvoiceTransitionException(InvoiceStatus from, InvoiceStatus to)
    : InvalidOperationException($"Cannot transition invoice from {from} to {to}.")
{
    public InvoiceStatus From { get; } = from;
    public InvoiceStatus To { get; } = to;
}

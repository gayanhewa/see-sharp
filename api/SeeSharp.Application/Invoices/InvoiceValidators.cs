using FluentValidation;

namespace SeeSharp.Application.Invoices;

public sealed class LineItemRequestValidator : AbstractValidator<LineItemRequest>
{
    public LineItemRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0m);
    }
}

public sealed class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Number).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.IssueDate);
        RuleForEach(x => x.LineItems).SetValidator(new LineItemRequestValidator());
    }
}

public sealed class UpdateInvoiceRequestValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceRequestValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.IssueDate);
        RuleForEach(x => x.LineItems).SetValidator(new LineItemRequestValidator());
    }
}

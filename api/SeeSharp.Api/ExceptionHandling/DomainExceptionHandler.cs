using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SeeSharp.Domain.Exceptions;

namespace SeeSharp.Api.ExceptionHandling;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, "Validation failed",
                string.Join("; ", ve.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))),
            InvalidInvoiceTransitionException ite => (StatusCodes.Status409Conflict, "Invalid invoice transition", ite.Message),
            ArgumentException ae => (StatusCodes.Status400BadRequest, "Invalid request", ae.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "An unexpected error occurred.")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.io/{status}"
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}

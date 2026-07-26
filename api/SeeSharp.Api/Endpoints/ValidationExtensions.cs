using FluentValidation;

namespace SeeSharp.Api.Endpoints;

public static class ValidationExtensions
{
    public static async Task ValidateAndThrowAsync<T>(
        this IValidator<T> validator, T instance, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(instance, ct);
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }
}

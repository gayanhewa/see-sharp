using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Categories;

public record CreateCategoryRequest(string Name);

public record CategoryResponse(Guid Id, string Name)
{
    public static CategoryResponse From(Category category) => new(category.Id, category.Name);
}

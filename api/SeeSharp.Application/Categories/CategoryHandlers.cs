using Microsoft.EntityFrameworkCore;
using SeeSharp.Application.Abstractions;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Categories;

public static class CategoryHandlers
{
    public static async Task<CategoryResponse> CreateAsync(
        IAppDbContext db, CreateCategoryRequest req, CancellationToken ct)
    {
        var category = Category.Create(req.Name);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return CategoryResponse.From(category);
    }

    public static async Task<IReadOnlyList<CategoryResponse>> ListAsync(IAppDbContext db, CancellationToken ct)
    {
        return await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name))
            .ToListAsync(ct);
    }

    public static async Task<bool> DeleteAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return false;
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

using Microsoft.EntityFrameworkCore;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Common;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Clients;

public static class ClientHandlers
{
    public static async Task<ClientResponse> CreateAsync(
        IAppDbContext db, CreateClientRequest req, CancellationToken ct)
    {
        var client = Client.Create(req.Name, req.Email, req.Address);
        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);
        return ClientResponse.From(client);
    }

    public static async Task<ClientResponse?> UpdateAsync(
        IAppDbContext db, Guid id, UpdateClientRequest req, CancellationToken ct)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null) return null;
        client.Update(req.Name, req.Email, req.Address);
        await db.SaveChangesAsync(ct);
        return ClientResponse.From(client);
    }

    public static async Task<bool> DeleteAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null) return false;
        db.Clients.Remove(client);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static async Task<ClientResponse?> GetAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var client = await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return client is null ? null : ClientResponse.From(client);
    }

    public static async Task<PagedResult<ClientResponse>> ListAsync(
        IAppDbContext db, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Clients.AsNoTracking().OrderBy(c => c.Name);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new ClientResponse(c.Id, c.Name, c.Email, c.Address, c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ClientResponse>(items, page, pageSize, total);
    }
}

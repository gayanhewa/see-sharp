using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Clients;

public record CreateClientRequest(string Name, string? Email, string? Address);

public record UpdateClientRequest(string Name, string? Email, string? Address);

public record ClientResponse(Guid Id, string Name, string? Email, string? Address, DateTimeOffset CreatedAt)
{
    public static ClientResponse From(Client client) =>
        new(client.Id, client.Name, client.Email, client.Address, client.CreatedAt);
}

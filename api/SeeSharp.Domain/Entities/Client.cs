namespace SeeSharp.Domain.Entities;

public sealed class Client
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Client() { }

    public static Client Create(string name, string? email, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Client name is required.", nameof(name));

        return new Client
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string? email, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Client name is required.", nameof(name));
        Name = name.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace SeeSharp.Api.Tests;

public class ClientsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient Client()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFactory.Token);
        return client;
    }

    [Fact]
    public async Task Post_then_Get_returns_created_client()
    {
        var client = Client();
        var create = await client.PostAsJsonAsync("/clients",
            new { name = "Integration Client", email = "i@test.dev", address = (string?)null });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await create.Content.ReadFromJsonAsync<ClientDto>();
        created!.Name.Should().Be("Integration Client");

        var get = await client.GetAsync($"/clients/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_without_token_is_unauthorized()
    {
        var res = await factory.CreateClient().GetAsync("/clients");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record ClientDto(Guid Id, string Name, string? Email, string? Address, DateTimeOffset CreatedAt);
}

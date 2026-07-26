using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace SeeSharp.Api.Tests;

public class InvoicesApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient Client()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFactory.Token);
        return client;
    }

    [Fact]
    public async Task Marking_a_draft_invoice_paid_returns_409()
    {
        var client = Client();

        var clientRes = await client.PostAsJsonAsync("/clients",
            new { name = "Inv Client", email = (string?)null, address = (string?)null });
        var created = await clientRes.Content.ReadFromJsonAsync<IdOnly>();

        var invoiceRes = await client.PostAsJsonAsync("/invoices", new
        {
            clientId = created!.Id,
            number = "INV-T1",
            issueDate = "2026-07-01",
            dueDate = "2026-07-31",
            notes = (string?)null,
            lineItems = new[] { new { description = "Work", quantity = 1, unitPrice = 100.0 } }
        });
        invoiceRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var invoice = await invoiceRes.Content.ReadFromJsonAsync<IdOnly>();

        var statusRes = await client.PostAsJsonAsync($"/invoices/{invoice!.Id}/status", new { status = "paid" });
        statusRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private sealed record IdOnly(Guid Id);
}

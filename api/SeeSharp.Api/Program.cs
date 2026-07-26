using FluentValidation;
using SeeSharp.Api.Auth;
using SeeSharp.Application.Clients;
using SeeSharp.Infrastructure;
using SeeSharp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AppDb")
    ?? throw new InvalidOperationException("ConnectionStrings:AppDb is not configured.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddValidatorsFromAssemblyContaining<CreateClientRequestValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<SeeSharp.Api.ExceptionHandling.DomainExceptionHandler>();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMiddleware<TokenAuthMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Endpoint groups are mapped here by later tasks:
// app.MapClients();
// app.MapCategories();
// app.MapExpenses();
// app.MapInvoices();
// app.MapReports();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

app.Run();

public partial class Program { }

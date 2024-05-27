using Elefess;
using Elefess.Authenticators.GitHub;
using Elefess.Hosting.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using OpenFastDL.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

// Octokit/GitHub webhooks
builder.Services.AddSingleton<WebhookEventProcessor, GitHubService>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 524288000; // 500 MiB
});

// LFS and related services
builder.Services.AddGitHubAuthenticator(builder.Configuration["GitHub:Organization"]!, builder.Configuration["GitHub:Repository"]!);
builder.Services.AddElefessDefaults();
builder.Services.AddElefessMvcDefaults();
builder.Services.AddOidMapper<PostgresLfsOidMapper>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dataSource = new NpgsqlDataSourceBuilder(builder.Configuration["PostgresConnectionString"]).Build();
builder.Services.AddDbContext<DatabaseContext>(x => x.UseNpgsql(dataSource));

//builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// app.UseApiKeyAuthentication();

//app.MapControllers();

app.MapGitLfsBatch();

// default route: /api/github/webhooks
app.MapGitHubWebhooks();

app.MapOidEndpoints();
app.MapUploadEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    db.Database.Migrate();
}

app.Run();
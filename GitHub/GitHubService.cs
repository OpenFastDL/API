using System.Reflection;
using System.Security.Cryptography;
using JWT.Algorithms;
using JWT.Builder;
using Octokit;
using Octokit.Webhooks;

namespace OpenFastDL.Api;

public sealed class GitHubService : WebhookEventProcessor, IHostedService
{
    private static readonly AssemblyName CurrentAssemblyName = typeof(GitHubService).Assembly.GetName();
    
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private IJwtAlgorithm _jwtAlgorithm = null!;
    private IGitHubClient _currentAppClient = null!;
    private DateTimeOffset? _appClientTokenExpiresAt;

    public GitHubService(ILogger<GitHubService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /* TODO: Nothing at the moment requires the app to process a issue as soon as it's opened/changed
    protected override async Task ProcessIssuesWebhookAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        string actionType = action;
        _logger.LogInformation("Issue handler fired! Action type: {Type}", actionType);

        if (action != IssuesAction.Opened)
            return;

        var client = await GetCurrentAppClientAsync();
        await client.Issue.Comment.Create(issuesEvent, "Hello, World!");
    }
    */

    private async Task<IGitHubClient> GetCurrentAppClientAsync()
    {
        var now = DateTimeOffset.UtcNow;
        // if the token isn't close to expiring yet, return the last authorized client to reduce requests.
        if (_appClientTokenExpiresAt - now > TimeSpan.FromSeconds(10))
        {
            return _currentAppClient;
        }
        
        var authClient = new GitHubClient(GetAppProductHeaderValue())
        {
            Credentials = new Credentials(GetJwtToken(), AuthenticationType.Bearer)
        };

        var installation = await authClient.GitHubApps.GetOrganizationInstallationForCurrent(_configuration["GitHub:Organization"]);
        var token = await authClient.GitHubApps.CreateInstallationToken(installation.Id);

        _appClientTokenExpiresAt = token.ExpiresAt;
        _currentAppClient = new GitHubClient(GetInstallationProductHeaderValue(installation.Id))
        {
            Credentials = new Credentials(token.Token)
        };

        return _currentAppClient;
    }

    private string GetJwtToken()
    {
        // https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-json-web-token-jwt-for-a-github-app
        var now = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(60));
        return JwtBuilder.Create()
            .AddClaim(ClaimName.IssuedAt, now.ToUnixTimeSeconds())
            .AddClaim(ClaimName.ExpirationTime, now.AddMinutes(10).ToUnixTimeSeconds())
            .AddClaim(ClaimName.Issuer, 316030)
            .WithAlgorithm(_jwtAlgorithm)
            .Encode();
    }

    private static ProductHeaderValue GetAppProductHeaderValue()
        => new(CurrentAssemblyName.Name, CurrentAssemblyName.Version!.ToString(3));

    private static ProductHeaderValue GetInstallationProductHeaderValue(long installationId)
        => new($"{CurrentAssemblyName.Name}-Installation{installationId}", CurrentAssemblyName.Version!.ToString(3));

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        var publicKey = RSA.Create();
        publicKey.ImportFromPem(await File.ReadAllTextAsync(_configuration["GitHub:App:PublicKey"]!, cancellationToken));
        
        var privateKey = RSA.Create();
        privateKey.ImportFromPem(await File.ReadAllTextAsync(_configuration["GitHub:App:PrivateKey"]!, cancellationToken));

        _jwtAlgorithm = new RS256Algorithm(publicKey, privateKey);
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
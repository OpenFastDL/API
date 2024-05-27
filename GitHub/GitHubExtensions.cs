using Octokit;
using Octokit.Webhooks.Events;

namespace OpenFastDL.Api;

public static class GitHubExtensions
{
    public static Task<IssueComment> Create(this IIssueCommentsClient client, IssuesEvent @event, string comment)
        => client.Create(@event.Repository!.Id, (int)@event.Issue.Number, comment);
}
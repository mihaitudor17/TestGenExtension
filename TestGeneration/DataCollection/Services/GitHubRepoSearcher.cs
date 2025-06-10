using Octokit;

namespace DataCollection.Services;

public class GitHubRepoSearcher
{
    private readonly GitHubClient _client;

    public GitHubRepoSearcher(string token)
    {
        _client = new GitHubClient(new ProductHeaderValue("StepDefinitionCollector"))
        {
            Credentials = new Credentials(token)
        };
    }

    public async Task<List<Repository>> SearchSpecFlowReposAsync(int maxRepos = 100)
    {
        var allRepos = new List<Repository>();
        int perPage = 100; // max allowed by GitHub API via Octokit
        int page = 1;

        while (allRepos.Count < maxRepos)
        {
            int remaining = maxRepos - allRepos.Count;
            int fetchCount = remaining > perPage ? perPage : remaining;

            var request = new SearchRepositoriesRequest("SpecFlow")
            {
                Language = Language.CSharp,
                SortField = RepoSearchSort.Stars,
                Order = SortDirection.Descending,
                Page = page,
                PerPage = fetchCount
            };

            var result = await _client.Search.SearchRepo(request);

            if (result.Items.Count == 0)
            {
                break;
            }

            allRepos.AddRange(result.Items);

            if (result.Items.Count < fetchCount)
            {
                break;
            }

            page++;
        }

        return allRepos.Take(maxRepos).ToList();
    }

    public async Task CloneRepoAsync(string cloneUrl, string localPath)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone --depth 1 {cloneUrl} \"{localPath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var process = System.Diagnostics.Process.Start(psi);
        await process.WaitForExitAsync();
    }
}
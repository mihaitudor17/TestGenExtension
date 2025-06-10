using DataCollection.Models;
using DataCollection.Parsing;
using DataCollection.Services;
using System.Text.Json;

class Program
{
    const int TargetCount = 100_000;
    const int MaxRepos = 1000;
    const string OutputFile = "step_definitions.json";
    const string Token = "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";

    static async Task Main(string[] args)
    {
        var extractor = new StepDefinitionExtractor();
        var searcher = new GitHubRepoSearcher(Token);

        var allPairs = new List<StepDefinitionPair>();
        var tempDir = Path.Combine(Path.GetTempPath(), "repos_" + Guid.NewGuid());

        Directory.CreateDirectory(tempDir);
        Console.WriteLine("Searching GitHub repos...");

        var repos = await searcher.SearchSpecFlowReposAsync(MaxRepos);
        Console.WriteLine($"Found {repos.Count} repos.");

        foreach (var repo in repos)
        {
            var name = repo.FullName.Replace("/", "_");
            var localRepoPath = Path.Combine(tempDir, name);

            try
            {
                Console.WriteLine($"Cloning {repo.FullName}...");
                await searcher.CloneRepoAsync(repo.CloneUrl, localRepoPath);

                var pairs = extractor.ExtractPairsFromDirectory(localRepoPath);

                allPairs.AddRange(pairs);
                Console.WriteLine($"Repo {repo.FullName} yielded {pairs.Count} pairs. Total: {allPairs.Count}");

                if (allPairs.Count >= TargetCount)
                    break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with {repo.FullName}: {ex.Message}");
            }
        }

        Console.WriteLine($"Collected {allPairs.Count} pairs. Saving to JSON...");
        var json = JsonSerializer.Serialize(allPairs, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(OutputFile, json);
        Console.WriteLine($"Done. Saved to {OutputFile}");
    }
}
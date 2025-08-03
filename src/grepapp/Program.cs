using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ivy.GrepApp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GrepApp;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var app = new CommandApp<SearchCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("grepapp");
        });

        return await app.RunAsync(args);
    }
}

public sealed class SearchCommand : AsyncCommand<SearchCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<query>")]
        [Description("Search query to find in code")]
        public string Query { get; init; } = string.Empty;

        [CommandOption("-l|--language")]
        [Description("Filter by programming language (e.g., python, javascript, csharp)")]
        public string? Language { get; init; }

        [CommandOption("-r|--repository")]
        [Description("Filter by repository (e.g., microsoft/vscode)")]
        public string? Repository { get; init; }

        [CommandOption("-p|--path")]
        [Description("Filter by file path pattern")]
        public string? Path { get; init; }

        [CommandOption("-n|--limit")]
        [Description("Maximum number of results to show (default: 10)")]
        public int? Limit { get; init; }

        [CommandOption("-v|--verbose")]
        [Description("Show detailed output with code snippets")]
        public bool Verbose { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            await SearchCodeAsync(settings.Query, settings.Language, settings.Repository, 
                settings.Path, settings.Limit, settings.Verbose);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private static async Task SearchCodeAsync(string query, string? language, string? repository, string? path, int? limit, bool verbose)
    {
        try
        {
            AnsiConsole.Write(new FigletText("GrepApp")
                .LeftJustified()
                .Color(Color.Blue));

            var response = await AnsiConsole.Status()
                .Start("Searching GitHub code...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Star);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    using var client = new GrepAppSearchClient();
                    var request = new SearchRequest(query)
                    {
                        Language = language,
                        Repository = repository,
                        Path = path,
                        ResultLimit = limit ?? 10
                    };

                    return await client.SearchAsync(request);
                });

            DisplayResults(response, verbose);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Environment.Exit(1);
        }
    }

    static void DisplayResults(SearchResponse response, bool verbose)
    {
        if (response.Summary.TotalResults == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No results found for your query.[/]");
            return;
        }

        // Display summary
        var summaryTable = new Table()
            .Title("Search Summary")
            .AddColumn("Metric")
            .AddColumn("Value")
            .Border(TableBorder.Rounded);

        summaryTable.AddRow("Query", $"[yellow]{response.Query}[/]");
        summaryTable.AddRow("Total Results", $"[green]{response.Summary.TotalResults:N0}[/]");
        summaryTable.AddRow("Results Shown", $"[blue]{response.Summary.ResultsShown}[/]");
        summaryTable.AddRow("Repositories", $"[magenta]{response.Summary.RepositoriesFound}[/]");

        AnsiConsole.Write(summaryTable);
        AnsiConsole.WriteLine();

        // Display top languages if available
        if (response.Summary.TopLanguages?.Count > 0)
        {
            var languageChart = new BarChart()
                .Width(60)
                .Label("[yellow]Top Languages[/]");

            foreach (var lang in response.Summary.TopLanguages)
            {
                languageChart.AddItem(lang.Language, lang.Count, Color.FromInt32(GetColorForLanguage(lang.Language)));
            }

            AnsiConsole.Write(languageChart);
            AnsiConsole.WriteLine();
        }

        // Display results by repository
        foreach (var repo in response.ResultsByRepository)
        {
            var fileTable = new Table()
                .AddColumn("File")
                .AddColumn("Language")
                .AddColumn("Matches")
                .AddColumn("Lines");

            if (verbose)
            {
                fileTable.AddColumn("Code Preview");
            }

            foreach (var file in repo.Files)
            {
                var fileUrl = $"https://github.com/{repo.Repository}/blob/{file.Branch}/{file.FilePath}";
                if (file.LineNumbers.Count > 0)
                {
                    fileUrl += $"#L{file.LineNumbers.First()}";
                }
                var fileName = $"[cyan link={fileUrl}]{file.FilePath}[/]";
                var language = $"[yellow]{file.Language}[/]";
                var matches = $"[green]{file.TotalMatches}[/]";
                var lines = file.LineNumbers.Count > 0 
                    ? $"[dim]{string.Join(", ", file.LineNumbers.Take(3).Select(x => x.ToString()))}{(file.LineNumbers.Count > 3 ? "..." : "")}[/]"
                    : "[dim]N/A[/]";

                if (verbose && !string.IsNullOrEmpty(file.CodeSnippet))
                {
                    var preview = file.CodeSnippet.Length > 100 
                        ? file.CodeSnippet.Substring(0, 100) + "..."
                        : file.CodeSnippet;
                    fileTable.AddRow(fileName, language, matches, lines, $"[dim]{preview.EscapeMarkup()}[/]");
                }
                else
                {
                    fileTable.AddRow(fileName, language, matches, lines);
                }
            }

            var repoPanel = new Panel(fileTable)
                .Header($"[bold blue]{repo.Repository}[/] - [green]{repo.MatchesCount} matches in {repo.Files.Count} files[/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Blue);

            AnsiConsole.Write(repoPanel);
            AnsiConsole.WriteLine();
        }

        // Show code snippets in detail view
        if (verbose)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[yellow]Code Snippets[/]").RuleStyle("dim"));
            AnsiConsole.WriteLine();

            foreach (var repo in response.ResultsByRepository)
            {
                var filesWithSnippets = repo.Files.Where(f => !string.IsNullOrEmpty(f.CodeSnippet)).ToList();
                foreach (var file in filesWithSnippets)
                {
                    var fileUrl = $"https://github.com/{repo.Repository}/blob/{file.Branch}/{file.FilePath}";
                    if (file.LineNumbers.Count > 0)
                    {
                        fileUrl += $"#L{file.LineNumbers.First()}";
                    }
                    var codePanel = new Panel(file.CodeSnippet)
                        .Header($"[blue]{repo.Repository}[/] / [cyan link={fileUrl}]{file.FilePath}[/]")
                        .Border(BoxBorder.Rounded)
                        .BorderColor(Color.Green);

                    AnsiConsole.Write(codePanel);
                    AnsiConsole.WriteLine();
                }
            }
        }

        // Show tips
        var hasSnippets = response.ResultsByRepository.Any(r => r.Files.Any(f => !string.IsNullOrEmpty(f.CodeSnippet)));
        if (!verbose && hasSnippets)
        {
            AnsiConsole.MarkupLine("[dim]Tip: Use --verbose (-v) to see code snippets[/]");
        }
    }

    static int GetColorForLanguage(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "python" => 208,      // Orange
            "javascript" => 226,  // Yellow
            "typescript" => 39,   // Blue
            "csharp" => 129,      // Purple
            "java" => 202,        // Red-Orange
            "go" => 45,           // Cyan
            "rust" => 166,        // Orange-Red
            "cpp" => 27,          // Dark Blue
            "c" => 24,            // Dark Blue
            "php" => 99,          // Purple
            "ruby" => 196,        // Red
            "swift" => 208,       // Orange
            "kotlin" => 165,      // Magenta
            _ => 250              // Grey
        };
    }
}
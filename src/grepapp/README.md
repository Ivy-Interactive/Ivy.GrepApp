# GrepApp CLI

A beautiful command-line tool for searching GitHub code using the grep.app API with colorful, pretty-printed output.

Created by the team at [Ivy](https://ivy.app) - The ultimate framework for creating internal business app in pure C#.

## Installation

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

### Install as Global Tool

Install globally as a .NET tool from NuGet:

```bash
dotnet tool install -g grepapp
```

### Update the Tool

To update to the latest version:

```bash
dotnet tool update -g grepapp
```

### Uninstall

To remove the tool:

```bash
dotnet tool uninstall -g grepapp
```

### Verify Installation

After installation, verify it's working:

```bash
grepapp --help
```

## Usage

### Basic Search
```bash
grepapp "HttpClient"
```

### Search with Language Filter
```bash
grepapp "async Task" --language csharp
```

### Search in Specific Repository
```bash
grepapp "useState" --repository facebook/react
```

### Search with Path Filter
```bash
grepapp "import React" --path "*.tsx"
```

### Limit Results
```bash
grepapp "console.log" --limit 5
```

### Verbose Output (with Code Snippets)
```bash
grepapp "function" --language javascript --verbose
```

## Options

- `<query>`: Search query (required - first argument)
- `--language, -l`: Filter by programming language
- `--repository, -r`: Filter by repository
- `--path, -p`: Filter by file path pattern
- `--limit, -n`: Maximum number of results (default: 10)
- `--verbose, -v`: Show detailed output with code snippets

## Features

- 🎨 Beautiful, colorful terminal output
- 📊 Visual charts showing top languages
- 🔍 Detailed search results organized by repository
- 💻 Code snippet previews in verbose mode
- ⚡ Fast search powered by grep.app API
- 🛠️ Easy installation as a global .NET tool

## Examples

Search for React hooks in TypeScript files:
```bash
grepapp "useEffect" -l typescript -v
```

Find authentication code in a specific repository:
```bash
grepapp "authentication" -r microsoft/vscode -n 20
```

Search for configuration files:
```bash
grepapp "config" -p "*.json" -l json
```

## Publishing to NuGet

To publish this tool to NuGet:

1. **Build the package:**
   ```bash
   dotnet pack --configuration Release
   ```

2. **Publish to NuGet:**
   ```bash
   dotnet nuget push bin/Release/grepapp.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
   ```

3. **Install globally:**
   ```bash
   dotnet tool install -g grepapp
   ```

## Development

To run from source:
```bash
dotnet run -- "search query" --language csharp --verbose
```

To build:
```bash
dotnet build
```

To pack:
```bash
dotnet pack
```
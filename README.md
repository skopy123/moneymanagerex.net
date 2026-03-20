# MoneyManager EX .NET

A C# reimplementation of [MoneyManager EX](https://moneymanagerex.org/) — the open-source personal finance manager originally written in C++. This project was converted from C++ to C# with the help of AI and extends the original with a web interface and an experimental MCP server.

The SQLite database format is fully compatible with the original MMEX application, so existing `.mmb` databases open without migration.

## Projects

| Project | Description |
|---|---|
| `mmex.net.core` | Domain entities, EF Core DbContext, and service interfaces |
| `mmex.net.winform` | Windows Forms desktop application |
| `mmex.net.webServer` | Blazor Server web interface (browser access to your database) |
| `mmex.net.mcpServer` | Experimental MCP server — exposes your finances as an AI tool context |
| `mmex.net.importer` | Import utilities |
| `mmex.net.tests` | Unit and smoke tests |

## Requirements

- .NET 10
- Windows (WinForms front-end); the web and MCP servers run cross-platform

## Getting Started (WinForms)

1. Clone the repo
2. Open `moneymanagerex.net.slnx` in Visual Studio 2022+ or Rider
3. Set `mmex.net.winform` as the startup project and run
4. On first launch, select an existing `.mmb` database or create a new one — the path is remembered for subsequent launches

<img width="1086" height="543" alt="image" src="https://github.com/user-attachments/assets/c14e8dd1-1a02-4fcb-ba8a-05b4fdb136a6" />


## Web Server

The `mmex.net.webServer` project provides a modern Blazor-based web interface with the same functionality as the desktop app: account management, transaction CRUD with splits, scheduled transactions, payee and category management, and file attachments.

<img width="1139" height="585" alt="mmex net web" src="https://github.com/user-attachments/assets/cb1ef6b7-6745-496d-8965-45203328680b" />


### Run directly

```bash
cd mmex.net.webServer
dotnet run /path/to/your/database.mmb
```

Or set the database path via environment variable:

```bash
export MMEX_DB_PATH=/path/to/your/database.mmb
dotnet run
```

### Run with Docker

1. Place your `.mmb` database file in a `data/` directory:

```bash
mkdir data
cp /path/to/your/database.mmb data/mmex.mmb
```

2. Build and run with Docker Compose:

```bash
docker compose up --build
```

3. Open http://localhost:8080

You can also build and run the Docker image directly:

```bash
docker build -t mmex-web .
docker run -p 8080:8080 -v $(pwd)/data:/data -e MMEX_DB_PATH=/data/mmex.mmb mmex-web
```

## Attachment folder

Attachments are stored alongside the database in the MMEX-standard folder structure:

```
<db directory>/
  Attachments/
    MMEX_<dbname>_Attachments/
      Transaction/
      ...
```

## CI/CD

The project includes GitHub Actions workflows:

- **Build Web Server** — builds and tests the Blazor web server on push/PR to `master` (when web or core files change), and verifies the Docker image builds
- **Build WinForms App** — builds the Windows Forms desktop application on push/PR to `master` (when winform or core files change)
- **Release** — triggered by version tags (`v*`), builds both apps and creates a GitHub Release with downloadable archives

To create a release:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## MCP Server (experimental)

The `mmex.net.mcpServer` project exposes your financial data as tools consumable by AI assistants that support the Model Context Protocol. Point your MCP client at the server and ask natural-language questions about your transactions, balances, and budgets.

> **Note:** This is experimental. Do not expose the MCP server to untrusted networks.

## Relationship to the original MMEX

This project is not affiliated with the official MoneyManager EX project. It is an independent reimplementation that reads and writes the same SQLite schema. The original C++ source is at [github.com/moneymanagerex/moneymanagerex](https://github.com/moneymanagerex/moneymanagerex).

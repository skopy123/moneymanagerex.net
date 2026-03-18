# MoneyManager EX .NET

A C# reimplementation of [MoneyManager EX](https://moneymanagerex.org/) — the open-source personal finance manager originally written in C++. This project was converted from C++ to C# with the help of AI and extends the original with a web interface and an experimental MCP server.

The SQLite database format is fully compatible with the original MMEX application, so existing `.mmb` databases open without migration.

## Projects

| Project | Description |
|---|---|
| `mmex.net.core` | Domain entities, EF Core DbContext, and service interfaces |
| `mmex.net.winform` | Windows Forms desktop application |
| `mmex.net.webServer` | ASP.NET Core web interface (browser access to your database) |
| `mmex.net.mcpServer` | Experimental MCP server — exposes your finances as an AI tool context |
| `mmex.net.importer` | Import utilities |
| `mmex.net.tests` | Unit and smoke tests |

## Requirements

- .NET 10
- Windows (WinForms front-end); the web and MCP servers run cross-platform

## Getting Started

1. Clone the repo
2. Open `moneymanagerex.net.slnx` in Visual Studio 2022+ or Rider
3. Set `mmex.net.winform` as the startup project and run
4. On first launch, select an existing `.mmb` database or create a new one — the path is remembered for subsequent launches

## Attachment folder

Attachments are stored alongside the database in the MMEX-standard folder structure:

```
<db directory>/
  Attachments/
    MMEX_<dbname>_Attachments/
      Transaction/
      ...
```

## MCP Server (experimental)

The `mmex.net.mcpServer` project exposes your financial data as tools consumable by AI assistants that support the Model Context Protocol. Point your MCP client at the server and ask natural-language questions about your transactions, balances, and budgets.

> **Note:** This is experimental. Do not expose the MCP server to untrusted networks.

## Relationship to the original MMEX

This project is not affiliated with the official MoneyManager EX project. It is an independent reimplementation that reads and writes the same SQLite schema. The original C++ source is at [github.com/moneymanagerex/moneymanagerex](https://github.com/moneymanagerex/moneymanagerex).

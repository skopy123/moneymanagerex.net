# mmex.net.mcpServer

**Status: Stub — not yet implemented.**

Planned: MCP (Model Context Protocol) server exposing MoneyManager data to AI assistants.

## Planned tools
| Tool | Description |
|------|-------------|
| `record_transaction` | Create a new transaction |
| `list_transactions` | List transactions for an account, with optional date range |
| `get_account_balance` | Get current balance for an account |
| `list_payees` | List all active payees |
| `list_accounts` | List all open accounts |
| `list_categories` | List categories (hierarchical) |

## Dependencies
- `mmex.net.core` for database access
- MCP server SDK (to be determined)

# mmex.net.webServer

**Status: Stub — not yet implemented.**

Planned: Kestrel + Blazor (WebAssembly or Server) single-user web interface for MoneyManager.

## Planned features
- Full CRUD for accounts, transactions, payees, categories
- Mobile-friendly responsive layout
- Dark/light mode toggle (CSS custom properties, persisted in localStorage)
- Localization via .resx resource files (English first, extensible)
- Date and number formatting independent of browser locale — user configures locale in SETTING_V1

## Dependencies
- `mmex.net.core` for database access
- `Microsoft.AspNetCore.Components.WebAssembly` or `Server`

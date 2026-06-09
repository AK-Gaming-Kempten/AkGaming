# AK Gaming InvoiceGenerator

Standalone invoice generator/renderer for AK Gaming with:

- Shared rendering core (`Core`)
- Local CLI workflow (`Cli`)
- Persistence and HTTP workflows hosted by the management tool (`AkGaming.Management/Modules/InvoiceManagement`)

The HTML renderer follows the layout of the sponsoring invoice and applies AK Gaming core theme colors (`dark-green`, `green`, `light-green-grey` palette).

## CLI usage

Create a starter JSON payload:

```bash
dotnet run --project AkGaming.InvoiceGenerator/Cli -- --sample AkGaming.InvoiceGenerator/sample-invoice.json
```

Render PDF invoice (native renderer):

```bash
dotnet run --project AkGaming.InvoiceGenerator/Cli -- --input AkGaming.InvoiceGenerator/sample-invoice.json --output AkGaming.InvoiceGenerator/out/invoice.pdf
```

Render HTML invoice (optional):

```bash
dotnet run --project AkGaming.InvoiceGenerator/Cli -- --input AkGaming.InvoiceGenerator/sample-invoice.json --output AkGaming.InvoiceGenerator/out/invoice.html
```

## Management API

The authenticated invoice API is part of `AkGaming.Management.WebApi`. Its admin-only routes are:

- `GET/POST /invoices` lists and creates persisted invoices
- `GET/PUT/DELETE /invoices/{id}` reads, edits, and deletes invoices
- `POST /invoices/render-html` previews an invoice draft as HTML
- `POST /invoices/render-pdf` renders an invoice draft as PDF
- `GET/POST/PUT/DELETE /invoice-party-presets` manages reusable invoice parties
- `GET/POST/PUT/DELETE /invoice-payment-terms-presets` manages reusable payment terms
- `GET/POST/PUT/DELETE /invoice-bank-account-presets` manages reusable bank accounts
- `GET/POST/PUT/DELETE /invoice-line-item-presets` manages reusable line items
- `GET/POST/PUT/DELETE /invoice-line-item-collection-presets` manages reusable line item collections

The management frontend exposes all preset workflows on the tabbed `/invoices/presets` page.

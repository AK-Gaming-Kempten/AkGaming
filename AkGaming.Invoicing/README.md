# AK Gaming Invoicing

Standalone invoice generator/renderer for AK Gaming with:

- Shared rendering core (`Core`)
- Local CLI workflow (`Cli`)
- HTTP API for later management-tool integration (`Api`)

The HTML renderer follows the layout of the sponsoring invoice and applies AK Gaming core theme colors (`dark-green`, `green`, `light-green-grey` palette).

## CLI usage

Create a starter JSON payload:

```bash
dotnet run --project AkGaming.Invoicing/Cli -- --sample AkGaming.Invoicing/sample-invoice.json
```

Render PDF invoice (native renderer):

```bash
dotnet run --project AkGaming.Invoicing/Cli -- --input AkGaming.Invoicing/sample-invoice.json --output AkGaming.Invoicing/out/invoice.pdf
```

Render HTML invoice (optional):

```bash
dotnet run --project AkGaming.Invoicing/Cli -- --input AkGaming.Invoicing/sample-invoice.json --output AkGaming.Invoicing/out/invoice.html
```

## API usage

Run API:

```bash
dotnet run --project AkGaming.Invoicing/Api
```

Endpoints:

- `GET /api/invoices/sample` returns a sample payload
- `POST /api/invoices/render-html` returns invoice HTML response
- `POST /api/invoices/render-file` returns downloadable HTML file
- `POST /api/invoices/render-pdf` returns downloadable PDF file (native render)

Swagger is available in development mode.

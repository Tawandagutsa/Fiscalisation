# Fiscalisation Service

A .NET worker + web UI that polls SQL Server, builds the fiscalisation payload, posts it to an API, and writes the response fields back to configurable columns.

## Configure

Update settings via the web UI at `/config` or edit `config.json` directly.

Key settings:
- `apiUrl`: endpoint to POST the receipt payload to.
- `connectionString`: SQL Server connection string.
- `tableName`: table or view name (include schema, e.g. `dbo.Deals` or `[dbo].[Deals]`).
- `whereClause`: optional raw SQL filter. If empty, the service uses:
  - `statusColumn = pendingStatusValue` and `toFiscaliseColumn = toFiscaliseValue`.
- `columns`: map DB column names to payload fields.
- `responseColumns`: map API response fields to DB columns.
- `responseColumns.errorMessage`: column to store API failure messages.
- `responseColumns.fullResponse`: column to store raw API response bodies.
- `emailSettings`: SMTP settings for timeout alerts.
- `timeoutStatusValue`, `inProgressStatusValue`, `failedStatusValue`: status values used during processing.
- `retryCountColumn`, `lastAttemptAtColumn`, `lastSuccessAtColumn`: reliability tracking columns.
- `maxRetries`, `retryBackoffBaseSeconds`, `retryBackoffMaxSeconds`: timeout retry behavior.

## Run (dev)

```bash
dotnet run --project FiscalisationService.csproj
```

Then visit:
- `http://localhost:5000` (or the URL shown in logs)
- `http://localhost:5000/config`
- `http://localhost:5000/health`
- `http://localhost:5000/metrics`

## Install as Windows Service

1. Publish

```bash
dotnet publish -c Release -o out
```

2. Create service (PowerShell as admin)

```powershell
sc.exe create FiscalisationService binPath= "C:\path\to\out\FiscalisationService.exe"
sc.exe start FiscalisationService
```

## Notes

- The service posts a JSON body that matches your sample payload.
- Only columns listed in `responseColumns` are updated.
- `config.json` is loaded on startup and after saves from the web UI.
- Email alerts fire only on API timeouts and are throttled by `throttleMinutes`.
- `TIMEOUT` records are retried using exponential backoff until `maxRetries` is reached.

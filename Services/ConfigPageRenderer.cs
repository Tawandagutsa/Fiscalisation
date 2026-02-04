using System.Net;
using System.Text;
using FiscalisationService.Models;

namespace FiscalisationService.Services;

public sealed class ConfigPageRenderer
{
    public string Render(ServiceConfig config, bool saved)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine("  <title>Fiscalisation Config</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    :root {");
        sb.AppendLine("      --bg: #f2efe9;");
        sb.AppendLine("      --card: #ffffff;");
        sb.AppendLine("      --ink: #1d1f23;");
        sb.AppendLine("      --muted: #5a616d;");
        sb.AppendLine("      --accent: #1b6d5a;");
        sb.AppendLine("      --accent-dark: #135041;");
        sb.AppendLine("      --border: #e2e1db;");
        sb.AppendLine("      --shadow: 0 16px 40px rgba(16, 24, 40, 0.12);");
        sb.AppendLine("    }");
        sb.AppendLine("    * { box-sizing: border-box; }");
        sb.AppendLine("    body { margin: 0; font-family: 'Trebuchet MS', 'Lucida Grande', sans-serif; color: var(--ink); background: radial-gradient(circle at top, #fff 0%, var(--bg) 55%); }");
        sb.AppendLine("    header { padding: 28px 24px 10px; }");
        sb.AppendLine("    h1 { font-family: 'Palatino Linotype', 'Book Antiqua', serif; margin: 0 0 6px; letter-spacing: 0.3px; }");
        sb.AppendLine("    .hint { color: var(--muted); margin: 0; }");
        sb.AppendLine("    .shell { max-width: 1100px; margin: 0 auto; padding: 0 24px 60px; }");
        sb.AppendLine("    .card { background: var(--card); border-radius: 18px; box-shadow: var(--shadow); padding: 22px; }");
        sb.AppendLine("    form { margin-top: 16px; }");
        sb.AppendLine("    fieldset { border: 1px solid var(--border); border-radius: 14px; padding: 18px; margin: 16px 0; background: #fff; }");
        sb.AppendLine("    legend { font-weight: 700; color: var(--accent-dark); padding: 0 10px; font-size: 15px; text-transform: uppercase; letter-spacing: 0.08em; }");
        sb.AppendLine("    label { display: block; font-size: 13px; color: var(--muted); margin-bottom: 10px; }");
        sb.AppendLine("    input { width: 100%; padding: 10px 12px; border-radius: 10px; border: 1px solid var(--border); background: #fbfbf9; font-size: 14px; color: var(--ink); }");
        sb.AppendLine("    input:focus { outline: 2px solid rgba(27, 109, 90, 0.25); border-color: var(--accent); background: #fff; }");
        sb.AppendLine("    .row { display: grid; gap: 14px; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); }");
        sb.AppendLine("    .actions { position: sticky; bottom: 16px; display: flex; justify-content: flex-end; padding-top: 8px; }");
        sb.AppendLine("    button { background: var(--accent); color: #fff; border: none; padding: 12px 20px; border-radius: 12px; font-size: 15px; font-weight: 600; box-shadow: 0 8px 18px rgba(27, 109, 90, 0.22); cursor: pointer; }");
        sb.AppendLine("    button:hover { background: var(--accent-dark); }");
        sb.AppendLine("    .saved { padding: 10px 14px; background: #e7f6ef; border: 1px solid #b4e2cd; border-radius: 12px; margin-top: 12px; color: #1e5a48; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <header class=\"shell\">");
        sb.AppendLine("    <h1>Fiscalisation Service Config</h1>");
        sb.AppendLine("    <p class=\"hint\">Update settings and click save. Changes apply to the next polling cycle.</p>");
        if (saved)
        {
            sb.AppendLine("    <div class=\"saved\">Configuration saved.</div>");
        }
        sb.AppendLine("  </header>");
        sb.AppendLine("  <div class=\"shell\">");
        sb.AppendLine("    <div class=\"card\">");
        sb.AppendLine("      <form method=\"post\" action=\"/config\">");

        AppendSection(sb, "Connection", new[]
        {
            Input("ApiUrl", "API URL", config.ApiUrl),
            Input("ConnectionString", "Connection String", config.ConnectionString),
            Input("TableName", "Table Name", config.TableName)
        });

        AppendSection(sb, "Polling", new[]
        {
            Input("PollIntervalSeconds", "Poll Interval (seconds)", config.PollIntervalSeconds.ToString()),
            Input("BatchSize", "Batch Size", config.BatchSize.ToString()),
            Input("MaxRetries", "Max Retries (timeouts)", config.MaxRetries.ToString()),
            Input("RetryBackoffBaseSeconds", "Retry Backoff Base (seconds)", config.RetryBackoffBaseSeconds.ToString()),
            Input("RetryBackoffMaxSeconds", "Retry Backoff Max (seconds)", config.RetryBackoffMaxSeconds.ToString())
        });

        AppendSection(sb, "Filtering", new[]
        {
            Input("WhereClause", "Custom WHERE clause (optional)", config.WhereClause),
            Input("StatusColumn", "Status Column", config.StatusColumn),
            Input("PendingStatusValue", "Pending Status Value", config.PendingStatusValue),
            Input("TimeoutStatusValue", "Timeout Status Value", config.TimeoutStatusValue),
            Input("InProgressStatusValue", "In-Progress Status Value", config.InProgressStatusValue),
            Input("FailedStatusValue", "Failed Status Value", config.FailedStatusValue),
            Input("ToFiscaliseColumn", "To Fiscalise Column", config.ToFiscaliseColumn),
            Input("ToFiscaliseValue", "To Fiscalise Value", config.ToFiscaliseValue)
        });

        AppendSection(sb, "Retry Columns", new[]
        {
            Input("RetryCountColumn", "Retry Count Column", config.RetryCountColumn),
            Input("LastAttemptAtColumn", "Last Attempt At Column", config.LastAttemptAtColumn),
            Input("LastSuccessAtColumn", "Last Success At Column", config.LastSuccessAtColumn)
        });

        AppendSection(sb, "Receipt Defaults", new[]
        {
            Input("ReceiptType", "Receipt Type", config.ReceiptType.ToString()),
            Input("DeviceId", "Device ID", config.DeviceId),
            Input("UsdPlatformValue", "USD Platform Value", config.UsdPlatformValue),
            Input("UsdCurrency", "USD Currency", config.UsdCurrency),
            Input("DefaultCurrency", "Default Currency", config.DefaultCurrency)
        });

        AppendSection(sb, "Buyer Defaults", new[]
        {
            Input("BuyerDefaults.Province", "Province", config.BuyerDefaults.Province),
            Input("BuyerDefaults.City", "City", config.BuyerDefaults.City),
            Input("BuyerDefaults.VatNumber", "VAT Number", config.BuyerDefaults.VatNumber),
            Input("BuyerDefaults.BuyerTin", "Buyer TIN", config.BuyerDefaults.BuyerTin)
        });

        AppendSection(sb, "Receipt Line Defaults", new[]
        {
            Input("ReceiptLineDefaults.LineType", "Line Type", config.ReceiptLineDefaults.LineType),
            Input("ReceiptLineDefaults.HsCode", "HS Code", config.ReceiptLineDefaults.HsCode),
            Input("ReceiptLineDefaults.Name", "Line Name", config.ReceiptLineDefaults.Name),
            Input("ReceiptLineDefaults.TaxPercent", "Tax Percent", config.ReceiptLineDefaults.TaxPercent.ToString()),
            Input("ReceiptLineDefaults.MoneyTypeCode", "Money Type Code", config.ReceiptLineDefaults.MoneyTypeCode.ToString())
        });

        AppendSection(sb, "Column Mappings", new[]
        {
            Input("Columns.Platform", "Platform", config.Columns.Platform),
            Input("Columns.TransactionEntry", "Transaction Entry", config.Columns.TransactionEntry),
            Input("Columns.Details", "Details", config.Columns.Details),
            Input("Columns.DealDate", "Deal Date", config.Columns.DealDate),
            Input("Columns.Brokerage", "Brokerage", config.Columns.Brokerage),
            Input("Columns.AmountDue", "Amount Due", config.Columns.AmountDue)
        });

        AppendSection(sb, "Client Lookup", new[]
        {
            Input("ClientLookup.Enabled", "Enabled (true/false)", config.ClientLookup.Enabled.ToString()),
            Input("ClientLookup.TableName", "Client Table Name", config.ClientLookup.TableName),
            Input("ClientLookup.AccountColumn", "Main Table Account Column", config.ClientLookup.AccountColumn),
            Input("ClientLookup.ClientIdColumn", "Client ID Column", config.ClientLookup.ClientIdColumn),
            Input("ClientLookup.ClientNameColumn", "Client Name Column", config.ClientLookup.ClientNameColumn),
            Input("ClientLookup.TelephoneNoColumn", "Telephone Column", config.ClientLookup.TelephoneNoColumn),
            Input("ClientLookup.EmailAddressColumn", "Email Column", config.ClientLookup.EmailAddressColumn),
            Input("ClientLookup.ClientAddressColumn", "Address Column", config.ClientLookup.ClientAddressColumn)
        });

        AppendSection(sb, "Response Column Mappings", new[]
        {
            Input("ResponseColumns.VerificationCode", "Verification Code", config.ResponseColumns.VerificationCode),
            Input("ResponseColumns.QrUrl", "QR URL", config.ResponseColumns.QrUrl),
            Input("ResponseColumns.FiscalisationStatus", "Fiscalisation Status", config.ResponseColumns.FiscalisationStatus),
            Input("ResponseColumns.DReceiptNumber", "DReceipt Number", config.ResponseColumns.DReceiptNumber),
            Input("ResponseColumns.InvoiceDate", "Invoice Date", config.ResponseColumns.InvoiceDate),
            Input("ResponseColumns.DeviceId", "Device ID", config.ResponseColumns.DeviceId),
            Input("ResponseColumns.ErrorMessage", "Error Message Column", config.ResponseColumns.ErrorMessage),
            Input("ResponseColumns.FullResponse", "Full Response Column", config.ResponseColumns.FullResponse)
        });

        AppendSection(sb, "Email Alerts (Timeouts)", new[]
        {
            Input("EmailSettings.Enabled", "Enabled (true/false)", config.EmailSettings.Enabled.ToString()),
            Input("EmailSettings.SmtpHost", "SMTP Host", config.EmailSettings.SmtpHost),
            Input("EmailSettings.SmtpPort", "SMTP Port", config.EmailSettings.SmtpPort.ToString()),
            Input("EmailSettings.EnableSsl", "Enable SSL (true/false)", config.EmailSettings.EnableSsl.ToString()),
            Input("EmailSettings.SmtpUser", "SMTP User", config.EmailSettings.SmtpUser),
            Input("EmailSettings.SmtpPassword", "SMTP Password", config.EmailSettings.SmtpPassword, "password"),
            Input("EmailSettings.FromAddress", "From Address", config.EmailSettings.FromAddress),
            Input("EmailSettings.ToAddresses", "To Addresses (comma/semicolon)", config.EmailSettings.ToAddresses),
            Input("EmailSettings.ClientName", "Client Name", config.EmailSettings.ClientName),
            Input("EmailSettings.Subject", "Subject", config.EmailSettings.Subject),
            Input("EmailSettings.TimeoutSeconds", "API Timeout Seconds", config.EmailSettings.TimeoutSeconds.ToString()),
            Input("EmailSettings.ThrottleMinutes", "Throttle Minutes", config.EmailSettings.ThrottleMinutes.ToString())
        });

        sb.AppendLine("        <div class=\"actions\">");
        sb.AppendLine("          <button type=\"submit\">Save Configuration</button>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </form>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, string title, IEnumerable<string> inputs)
    {
        sb.AppendLine("  <fieldset>");
        sb.AppendLine($"    <legend>{WebUtility.HtmlEncode(title)}</legend>");
        sb.AppendLine("    <div class=\"row\">");
        foreach (var input in inputs)
        {
            sb.AppendLine(input);
        }
        sb.AppendLine("    </div>");
        sb.AppendLine("  </fieldset>");
    }

    private static string Input(string name, string label, string value, string type = "text")
    {
        var encodedValue = WebUtility.HtmlEncode(value ?? string.Empty);
        return $"<label>{WebUtility.HtmlEncode(label)}<input type=\"{WebUtility.HtmlEncode(type)}\" name=\"{WebUtility.HtmlEncode(name)}\" value=\"{encodedValue}\" /></label>";
    }
}

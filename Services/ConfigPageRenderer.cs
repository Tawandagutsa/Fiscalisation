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
        sb.AppendLine("    body { font-family: 'Segoe UI', Tahoma, sans-serif; margin: 24px; background: #f7f4ef; color: #222; }");
        sb.AppendLine("    h1 { margin-bottom: 4px; }");
        sb.AppendLine("    .hint { color: #555; margin-bottom: 16px; }");
        sb.AppendLine("    form { background: #fff; padding: 20px; border-radius: 12px; box-shadow: 0 8px 24px rgba(0,0,0,0.08); }");
        sb.AppendLine("    fieldset { border: 1px solid #ddd; border-radius: 10px; padding: 16px; margin-bottom: 18px; }");
        sb.AppendLine("    legend { font-weight: 600; padding: 0 8px; }");
        sb.AppendLine("    label { display: block; margin-bottom: 10px; }");
        sb.AppendLine("    input { width: 100%; padding: 8px 10px; border-radius: 8px; border: 1px solid #ccc; }");
        sb.AppendLine("    .row { display: grid; gap: 12px; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); }");
        sb.AppendLine("    button { background: #1d6b5f; color: #fff; border: none; padding: 10px 18px; border-radius: 8px; font-size: 16px; }");
        sb.AppendLine("    .saved { padding: 8px 12px; background: #e7f6ef; border: 1px solid #b4e2cd; border-radius: 8px; margin-bottom: 16px; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <h1>Fiscalisation Service Config</h1>");
        sb.AppendLine("  <div class=\"hint\">Update settings and click save. Changes apply to the next polling cycle.</div>");
        if (saved)
        {
            sb.AppendLine("  <div class=\"saved\">Configuration saved.</div>");
        }
        sb.AppendLine("  <form method=\"post\" action=\"/config\">");

        AppendSection(sb, "Connection", new[]
        {
            Input("ApiUrl", "API URL", config.ApiUrl),
            Input("ConnectionString", "Connection String", config.ConnectionString),
            Input("TableName", "Table Name", config.TableName)
        });

        AppendSection(sb, "Polling", new[]
        {
            Input("PollIntervalSeconds", "Poll Interval (seconds)", config.PollIntervalSeconds.ToString()),
            Input("BatchSize", "Batch Size", config.BatchSize.ToString())
        });

        AppendSection(sb, "Filtering", new[]
        {
            Input("WhereClause", "Custom WHERE clause (optional)", config.WhereClause),
            Input("StatusColumn", "Status Column", config.StatusColumn),
            Input("PendingStatusValue", "Pending Status Value", config.PendingStatusValue),
            Input("TimeoutStatusValue", "Timeout Status Value", config.TimeoutStatusValue),
            Input("ToFiscaliseColumn", "To Fiscalise Column", config.ToFiscaliseColumn),
            Input("ToFiscaliseValue", "To Fiscalise Value", config.ToFiscaliseValue)
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
            Input("Columns.ClientName", "Client Name", config.Columns.ClientName),
            Input("Columns.TelephoneNo", "Telephone No", config.Columns.TelephoneNo),
            Input("Columns.EmailAddress", "Email Address", config.Columns.EmailAddress),
            Input("Columns.ClientAddress", "Client Address", config.Columns.ClientAddress),
            Input("Columns.Details", "Details", config.Columns.Details),
            Input("Columns.DealDate", "Deal Date", config.Columns.DealDate),
            Input("Columns.Brokerage", "Brokerage", config.Columns.Brokerage),
            Input("Columns.AmountDue", "Amount Due", config.Columns.AmountDue)
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

        sb.AppendLine("    <button type=\"submit\">Save Configuration</button>");
        sb.AppendLine("  </form>");
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

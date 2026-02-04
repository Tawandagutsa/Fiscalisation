using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace FiscalisationService.Models;

public sealed class ServiceConfig
{
    public string ApiUrl { get; set; } = "https://api.example.com/receipts";
    public string ConnectionString { get; set; } = "";
    public string TableName { get; set; } = "Deals";
    public string WhereClause { get; set; } = "";
    public string StatusColumn { get; set; } = "FiscalisationStatus";
    public string PendingStatusValue { get; set; } = "PENDING";
    public string TimeoutStatusValue { get; set; } = "TIMEOUT";
    public string InProgressStatusValue { get; set; } = "IN_PROGRESS";
    public string FailedStatusValue { get; set; } = "FAILED";
    public string ToFiscaliseColumn { get; set; } = "ToFiscalise";
    public string ToFiscaliseValue { get; set; } = "Yes";
    public int PollIntervalSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 50;
    public int MaxRetries { get; set; } = 5;
    public int RetryBackoffBaseSeconds { get; set; } = 10;
    public int RetryBackoffMaxSeconds { get; set; } = 300;
    public string RetryCountColumn { get; set; } = "RetryCount";
    public string LastAttemptAtColumn { get; set; } = "LastAttemptAt";
    public string LastSuccessAtColumn { get; set; } = "LastSuccessAt";

    public int ReceiptType { get; set; } = 0;
    public string DeviceId { get; set; } = "17436";
    public string UsdPlatformValue { get; set; } = "VFEX";
    public string UsdCurrency { get; set; } = "USD";
    public string DefaultCurrency { get; set; } = "ZWG";

    public BuyerDefaults BuyerDefaults { get; set; } = new();
    public ReceiptLineDefaults ReceiptLineDefaults { get; set; } = new();

    public ColumnMappings Columns { get; set; } = new();
    public ResponseColumnMappings ResponseColumns { get; set; } = new();
    public EmailSettings EmailSettings { get; set; } = new();
    public ClientLookupSettings ClientLookup { get; set; } = new();

    public string EffectiveWhereClause()
    {
        if (!string.IsNullOrWhiteSpace(WhereClause))
        {
            return WhereClause;
        }

        var statusFilter = $"([{StatusColumn}] = '{PendingStatusValue}' OR [{StatusColumn}] = '{TimeoutStatusValue}')";
        var fiscaliseFilter = $"[{ToFiscaliseColumn}] = '{ToFiscaliseValue}'";
        var retryFilter = string.IsNullOrWhiteSpace(RetryCountColumn)
            ? ""
            : $" AND ([{RetryCountColumn}] IS NULL OR [{RetryCountColumn}] < {MaxRetries})";
        return $"{statusFilter} AND {fiscaliseFilter}{retryFilter}";
    }

    public static ServiceConfig FromForm(IFormCollection form, ServiceConfig fallback)
    {
        var config = fallback.Clone();

        config.ApiUrl = FormValue(form, "ApiUrl", config.ApiUrl);
        config.ConnectionString = FormValue(form, "ConnectionString", config.ConnectionString);
        config.TableName = FormValue(form, "TableName", config.TableName);
        config.WhereClause = FormValue(form, "WhereClause", config.WhereClause);
        config.StatusColumn = FormValue(form, "StatusColumn", config.StatusColumn);
        config.PendingStatusValue = FormValue(form, "PendingStatusValue", config.PendingStatusValue);
        config.TimeoutStatusValue = FormValue(form, "TimeoutStatusValue", config.TimeoutStatusValue);
        config.InProgressStatusValue = FormValue(form, "InProgressStatusValue", config.InProgressStatusValue);
        config.FailedStatusValue = FormValue(form, "FailedStatusValue", config.FailedStatusValue);
        config.ToFiscaliseColumn = FormValue(form, "ToFiscaliseColumn", config.ToFiscaliseColumn);
        config.ToFiscaliseValue = FormValue(form, "ToFiscaliseValue", config.ToFiscaliseValue);
        config.PollIntervalSeconds = FormInt(form, "PollIntervalSeconds", config.PollIntervalSeconds);
        config.BatchSize = FormInt(form, "BatchSize", config.BatchSize);
        config.MaxRetries = FormInt(form, "MaxRetries", config.MaxRetries);
        config.RetryBackoffBaseSeconds = FormInt(form, "RetryBackoffBaseSeconds", config.RetryBackoffBaseSeconds);
        config.RetryBackoffMaxSeconds = FormInt(form, "RetryBackoffMaxSeconds", config.RetryBackoffMaxSeconds);
        config.RetryCountColumn = FormValue(form, "RetryCountColumn", config.RetryCountColumn);
        config.LastAttemptAtColumn = FormValue(form, "LastAttemptAtColumn", config.LastAttemptAtColumn);
        config.LastSuccessAtColumn = FormValue(form, "LastSuccessAtColumn", config.LastSuccessAtColumn);

        config.ReceiptType = FormInt(form, "ReceiptType", config.ReceiptType);
        config.DeviceId = FormValue(form, "DeviceId", config.DeviceId);
        config.UsdPlatformValue = FormValue(form, "UsdPlatformValue", config.UsdPlatformValue);
        config.UsdCurrency = FormValue(form, "UsdCurrency", config.UsdCurrency);
        config.DefaultCurrency = FormValue(form, "DefaultCurrency", config.DefaultCurrency);

        config.BuyerDefaults.Province = FormValue(form, "BuyerDefaults.Province", config.BuyerDefaults.Province);
        config.BuyerDefaults.City = FormValue(form, "BuyerDefaults.City", config.BuyerDefaults.City);
        config.BuyerDefaults.VatNumber = FormValue(form, "BuyerDefaults.VatNumber", config.BuyerDefaults.VatNumber);
        config.BuyerDefaults.BuyerTin = FormValue(form, "BuyerDefaults.BuyerTin", config.BuyerDefaults.BuyerTin);

        config.ReceiptLineDefaults.LineType = FormValue(form, "ReceiptLineDefaults.LineType", config.ReceiptLineDefaults.LineType);
        config.ReceiptLineDefaults.HsCode = FormValue(form, "ReceiptLineDefaults.HsCode", config.ReceiptLineDefaults.HsCode);
        config.ReceiptLineDefaults.Name = FormValue(form, "ReceiptLineDefaults.Name", config.ReceiptLineDefaults.Name);
        config.ReceiptLineDefaults.TaxPercent = FormDecimal(form, "ReceiptLineDefaults.TaxPercent", config.ReceiptLineDefaults.TaxPercent);
        config.ReceiptLineDefaults.MoneyTypeCode = FormInt(form, "ReceiptLineDefaults.MoneyTypeCode", config.ReceiptLineDefaults.MoneyTypeCode);

        config.Columns.Platform = FormValue(form, "Columns.Platform", config.Columns.Platform);
        config.Columns.TransactionEntry = FormValue(form, "Columns.TransactionEntry", config.Columns.TransactionEntry);
        config.Columns.ClientName = FormValue(form, "Columns.ClientName", config.Columns.ClientName);
        config.Columns.TelephoneNo = FormValue(form, "Columns.TelephoneNo", config.Columns.TelephoneNo);
        config.Columns.EmailAddress = FormValue(form, "Columns.EmailAddress", config.Columns.EmailAddress);
        config.Columns.ClientAddress = FormValue(form, "Columns.ClientAddress", config.Columns.ClientAddress);
        config.Columns.Details = FormValue(form, "Columns.Details", config.Columns.Details);
        config.Columns.DealDate = FormValue(form, "Columns.DealDate", config.Columns.DealDate);
        config.Columns.Brokerage = FormValue(form, "Columns.Brokerage", config.Columns.Brokerage);
        config.Columns.AmountDue = FormValue(form, "Columns.AmountDue", config.Columns.AmountDue);

        config.ClientLookup.Enabled = FormBool(form, "ClientLookup.Enabled", config.ClientLookup.Enabled);
        config.ClientLookup.TableName = FormValue(form, "ClientLookup.TableName", config.ClientLookup.TableName);
        config.ClientLookup.AccountColumn = FormValue(form, "ClientLookup.AccountColumn", config.ClientLookup.AccountColumn);
        config.ClientLookup.ClientIdColumn = FormValue(form, "ClientLookup.ClientIdColumn", config.ClientLookup.ClientIdColumn);
        config.ClientLookup.ClientNameColumn = FormValue(form, "ClientLookup.ClientNameColumn", config.ClientLookup.ClientNameColumn);
        config.ClientLookup.TelephoneNoColumn = FormValue(form, "ClientLookup.TelephoneNoColumn", config.ClientLookup.TelephoneNoColumn);
        config.ClientLookup.EmailAddressColumn = FormValue(form, "ClientLookup.EmailAddressColumn", config.ClientLookup.EmailAddressColumn);
        config.ClientLookup.ClientAddressColumn = FormValue(form, "ClientLookup.ClientAddressColumn", config.ClientLookup.ClientAddressColumn);

        config.ResponseColumns.VerificationCode = FormValue(form, "ResponseColumns.VerificationCode", config.ResponseColumns.VerificationCode);
        config.ResponseColumns.QrUrl = FormValue(form, "ResponseColumns.QrUrl", config.ResponseColumns.QrUrl);
        config.ResponseColumns.FiscalisationStatus = FormValue(form, "ResponseColumns.FiscalisationStatus", config.ResponseColumns.FiscalisationStatus);
        config.ResponseColumns.DReceiptNumber = FormValue(form, "ResponseColumns.DReceiptNumber", config.ResponseColumns.DReceiptNumber);
        config.ResponseColumns.InvoiceDate = FormValue(form, "ResponseColumns.InvoiceDate", config.ResponseColumns.InvoiceDate);
        config.ResponseColumns.DeviceId = FormValue(form, "ResponseColumns.DeviceId", config.ResponseColumns.DeviceId);
        config.ResponseColumns.ErrorMessage = FormValue(form, "ResponseColumns.ErrorMessage", config.ResponseColumns.ErrorMessage);
        config.ResponseColumns.FullResponse = FormValue(form, "ResponseColumns.FullResponse", config.ResponseColumns.FullResponse);

        config.EmailSettings.Enabled = FormBool(form, "EmailSettings.Enabled", config.EmailSettings.Enabled);
        config.EmailSettings.SmtpHost = FormValue(form, "EmailSettings.SmtpHost", config.EmailSettings.SmtpHost);
        config.EmailSettings.SmtpPort = FormInt(form, "EmailSettings.SmtpPort", config.EmailSettings.SmtpPort);
        config.EmailSettings.EnableSsl = FormBool(form, "EmailSettings.EnableSsl", config.EmailSettings.EnableSsl);
        config.EmailSettings.SmtpUser = FormValue(form, "EmailSettings.SmtpUser", config.EmailSettings.SmtpUser);
        config.EmailSettings.SmtpPassword = FormValue(form, "EmailSettings.SmtpPassword", config.EmailSettings.SmtpPassword);
        config.EmailSettings.FromAddress = FormValue(form, "EmailSettings.FromAddress", config.EmailSettings.FromAddress);
        config.EmailSettings.ToAddresses = FormValue(form, "EmailSettings.ToAddresses", config.EmailSettings.ToAddresses);
        config.EmailSettings.ClientName = FormValue(form, "EmailSettings.ClientName", config.EmailSettings.ClientName);
        config.EmailSettings.Subject = FormValue(form, "EmailSettings.Subject", config.EmailSettings.Subject);
        config.EmailSettings.TimeoutSeconds = FormInt(form, "EmailSettings.TimeoutSeconds", config.EmailSettings.TimeoutSeconds);
        config.EmailSettings.ThrottleMinutes = FormInt(form, "EmailSettings.ThrottleMinutes", config.EmailSettings.ThrottleMinutes);

        return config;
    }

    public ServiceConfig Clone()
    {
        return new ServiceConfig
        {
            ApiUrl = ApiUrl,
            ConnectionString = ConnectionString,
            TableName = TableName,
            WhereClause = WhereClause,
            StatusColumn = StatusColumn,
            PendingStatusValue = PendingStatusValue,
            TimeoutStatusValue = TimeoutStatusValue,
            InProgressStatusValue = InProgressStatusValue,
            FailedStatusValue = FailedStatusValue,
            ToFiscaliseColumn = ToFiscaliseColumn,
            ToFiscaliseValue = ToFiscaliseValue,
            PollIntervalSeconds = PollIntervalSeconds,
            BatchSize = BatchSize,
            MaxRetries = MaxRetries,
            RetryBackoffBaseSeconds = RetryBackoffBaseSeconds,
            RetryBackoffMaxSeconds = RetryBackoffMaxSeconds,
            RetryCountColumn = RetryCountColumn,
            LastAttemptAtColumn = LastAttemptAtColumn,
            LastSuccessAtColumn = LastSuccessAtColumn,
            ReceiptType = ReceiptType,
            DeviceId = DeviceId,
            UsdPlatformValue = UsdPlatformValue,
            UsdCurrency = UsdCurrency,
            DefaultCurrency = DefaultCurrency,
            BuyerDefaults = BuyerDefaults.Clone(),
            ReceiptLineDefaults = ReceiptLineDefaults.Clone(),
            Columns = Columns.Clone(),
            ResponseColumns = ResponseColumns.Clone(),
            EmailSettings = EmailSettings.Clone(),
            ClientLookup = ClientLookup.Clone()
        };
    }

    private static string FormValue(IFormCollection form, string key, string fallback)
    {
        return form.TryGetValue(key, out StringValues value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : fallback;
    }

    private static int FormInt(IFormCollection form, string key, int fallback)
    {
        return form.TryGetValue(key, out StringValues value) && int.TryParse(value.ToString(), out int parsed)
            ? parsed
            : fallback;
    }

    private static decimal FormDecimal(IFormCollection form, string key, decimal fallback)
    {
        return form.TryGetValue(key, out StringValues value) && decimal.TryParse(value.ToString(), out decimal parsed)
            ? parsed
            : fallback;
    }

    private static bool FormBool(IFormCollection form, string key, bool fallback)
    {
        return form.TryGetValue(key, out StringValues value) && bool.TryParse(value.ToString(), out bool parsed)
            ? parsed
            : fallback;
    }
}

public sealed class BuyerDefaults
{
    public string Province { get; set; } = "Harare";
    public string City { get; set; } = "Harare";
    public string VatNumber { get; set; } = "000000000";
    public string BuyerTin { get; set; } = "0000000000";

    public BuyerDefaults Clone()
    {
        return new BuyerDefaults
        {
            Province = Province,
            City = City,
            VatNumber = VatNumber,
            BuyerTin = BuyerTin
        };
    }
}

public sealed class ReceiptLineDefaults
{
    public string LineType { get; set; } = "Sale";
    public string HsCode { get; set; } = "001";
    public string Name { get; set; } = "0.92% Brokerage Fees";
    public decimal TaxPercent { get; set; } = 15.5m;
    public int MoneyTypeCode { get; set; } = 5;

    public ReceiptLineDefaults Clone()
    {
        return new ReceiptLineDefaults
        {
            LineType = LineType,
            HsCode = HsCode,
            Name = Name,
            TaxPercent = TaxPercent,
            MoneyTypeCode = MoneyTypeCode
        };
    }
}

public sealed class ColumnMappings
{
    public string Platform { get; set; } = "Platform";
    public string TransactionEntry { get; set; } = "TransactionEntry";
    public string ClientName { get; set; } = "ClientName";
    public string TelephoneNo { get; set; } = "TelephoneNo";
    public string EmailAddress { get; set; } = "EmailAddress";
    public string ClientAddress { get; set; } = "ClientAddress";
    public string Details { get; set; } = "Details";
    public string DealDate { get; set; } = "DealDate";
    public string Brokerage { get; set; } = "Brokerage";
    public string AmountDue { get; set; } = "AmountDue";

    public ColumnMappings Clone()
    {
        return new ColumnMappings
        {
            Platform = Platform,
            TransactionEntry = TransactionEntry,
            ClientName = ClientName,
            TelephoneNo = TelephoneNo,
            EmailAddress = EmailAddress,
            ClientAddress = ClientAddress,
            Details = Details,
            DealDate = DealDate,
            Brokerage = Brokerage,
            AmountDue = AmountDue
        };
    }
}

public sealed class ResponseColumnMappings
{
    public string VerificationCode { get; set; } = "VerificationCode";
    public string QrUrl { get; set; } = "qrlUrl";
    public string FiscalisationStatus { get; set; } = "FiscalisationStatus";
    public string DReceiptNumber { get; set; } = "dreceiptNumber";
    public string InvoiceDate { get; set; } = "invoiceDate";
    public string DeviceId { get; set; } = "deviceID";
    public string ErrorMessage { get; set; } = "FiscalisationError";
    public string FullResponse { get; set; } = "FiscalisationResponse";

    public ResponseColumnMappings Clone()
    {
        return new ResponseColumnMappings
        {
            VerificationCode = VerificationCode,
            QrUrl = QrUrl,
            FiscalisationStatus = FiscalisationStatus,
            DReceiptNumber = DReceiptNumber,
            InvoiceDate = InvoiceDate,
            DeviceId = DeviceId,
            ErrorMessage = ErrorMessage,
            FullResponse = FullResponse
        };
    }
}

public sealed class ClientLookupSettings
{
    public bool Enabled { get; set; } = true;
    public string TableName { get; set; } = "Client";
    public string AccountColumn { get; set; } = "Account";
    public string ClientIdColumn { get; set; } = "ID";
    public string ClientNameColumn { get; set; } = "ClientName";
    public string TelephoneNoColumn { get; set; } = "TelephoneNo";
    public string EmailAddressColumn { get; set; } = "EmailAddress";
    public string ClientAddressColumn { get; set; } = "ClientAddress";

    public ClientLookupSettings Clone()
    {
        return new ClientLookupSettings
        {
            Enabled = Enabled,
            TableName = TableName,
            AccountColumn = AccountColumn,
            ClientIdColumn = ClientIdColumn,
            ClientNameColumn = ClientNameColumn,
            TelephoneNoColumn = TelephoneNoColumn,
            EmailAddressColumn = EmailAddressColumn,
            ClientAddressColumn = ClientAddressColumn
        };
    }
}

public sealed class EmailSettings
{
    public bool Enabled { get; set; } = false;
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string ToAddresses { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string Subject { get; set; } = "Fiscalisation API timeout";
    public int TimeoutSeconds { get; set; } = 15;
    public int ThrottleMinutes { get; set; } = 15;

    public EmailSettings Clone()
    {
        return new EmailSettings
        {
            Enabled = Enabled,
            SmtpHost = SmtpHost,
            SmtpPort = SmtpPort,
            EnableSsl = EnableSsl,
            SmtpUser = SmtpUser,
            SmtpPassword = SmtpPassword,
            FromAddress = FromAddress,
            ToAddresses = ToAddresses,
            ClientName = ClientName,
            Subject = Subject,
            TimeoutSeconds = TimeoutSeconds,
            ThrottleMinutes = ThrottleMinutes
        };
    }
}

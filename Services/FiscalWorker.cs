using System.Globalization;
using FiscalisationService.Models;

namespace FiscalisationService.Services;

public sealed class FiscalWorker : BackgroundService
{
    private readonly ConfigStore _configStore;
    private readonly SqlRepository _repository;
    private readonly FiscalApiClient _apiClient;
    private readonly EmailNotifier _emailNotifier;
    private readonly ServiceStats _stats;
    private readonly ILogger<FiscalWorker> _logger;

    public FiscalWorker(
        ConfigStore configStore,
        SqlRepository repository,
        FiscalApiClient apiClient,
        EmailNotifier emailNotifier,
        ServiceStats stats,
        ILogger<FiscalWorker> logger)
    {
        _configStore = configStore;
        _repository = repository;
        _apiClient = apiClient;
        _emailNotifier = emailNotifier;
        _stats = stats;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var config = _configStore.Current;
            try
            {
                var records = await _repository.GetPendingAsync(config, stoppingToken);
                _stats.RecordBatch(records.Count);
                foreach (var record in records)
                {
                    if (!TryGetInt(record, "ID", out var id))
                    {
                        continue;
                    }

                    var status = GetString(record, config.StatusColumn);
                    var retryTrackingEnabled = !string.IsNullOrWhiteSpace(config.RetryCountColumn);
                    var retryCount = retryTrackingEnabled ? GetInt(record, config.RetryCountColumn) : 0;
                    var lastAttemptAt = GetDateTime(record, config.LastAttemptAtColumn);

                    if (string.Equals(status, config.TimeoutStatusValue, StringComparison.OrdinalIgnoreCase))
                    {
                        if (retryTrackingEnabled && retryCount >= config.MaxRetries)
                        {
                            await _repository.UpdateStatusAsync(config, id, config.FailedStatusValue, stoppingToken);
                            await _repository.UpdateFailureAsync(config, id, "Max retries exceeded.", null, stoppingToken);
                            _stats.RecordFailure("Max retries exceeded.");
                            continue;
                        }

                        if (retryTrackingEnabled)
                        {
                            var delaySeconds = CalculateBackoffSeconds(config, retryCount);
                            if (lastAttemptAt.HasValue && DateTimeOffset.UtcNow - lastAttemptAt.Value < TimeSpan.FromSeconds(delaySeconds))
                            {
                                continue;
                            }
                        }
                    }

                    await _repository.MarkInProgressAsync(config, id, stoppingToken);

                    object? clientLookupKey = null;
                    if (config.ClientLookup.Enabled && !string.IsNullOrWhiteSpace(config.ClientLookup.AccountColumn))
                    {
                        record.TryGetValue(config.ClientLookup.AccountColumn, out clientLookupKey);
                    }
                    var clientRecord = clientLookupKey is null
                        ? null
                        : await _repository.GetClientAsync(config, clientLookupKey, stoppingToken);

                    var receipt = BuildReceipt(record, clientRecord, config);
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var result = await _apiClient.PostReceiptAsync(config.ApiUrl, receipt, config.EmailSettings.TimeoutSeconds, stoppingToken);
                    stopwatch.Stop();

                    if (result.TimedOut)
                    {
                        _logger.LogWarning("Fiscalisation API timeout for ID {Id}. Took {Elapsed}ms.", id, stopwatch.ElapsedMilliseconds);
                        await _emailNotifier.NotifyTimeoutAsync(config, null, stoppingToken);
                        await _repository.UpdateTimeoutStatusAsync(config, id, stoppingToken);
                        if (retryTrackingEnabled)
                        {
                            await _repository.IncrementRetryAsync(config, id, stoppingToken);
                        }
                        await _repository.UpdateFailureAsync(config, id, result.ErrorMessage ?? "Timeout", result.RawResponse, stoppingToken);
                        _stats.RecordTimeout(result.ErrorMessage ?? "Timeout");
                        continue;
                    }

                    if (result.Response is null)
                    {
                        var error = result.ErrorMessage ?? "API call failed.";
                        _logger.LogWarning("Fiscalisation failed for ID {Id}. {Error}. Took {Elapsed}ms.", id, error, stopwatch.ElapsedMilliseconds);
                        await _repository.UpdateStatusAsync(config, id, config.FailedStatusValue, stoppingToken);
                        await _repository.UpdateFailureAsync(config, id, error, result.RawResponse, stoppingToken);
                        _stats.RecordFailure(error);
                        continue;
                    }

                    await _repository.UpdateResponseAsync(config, id, result.Response, result.RawResponse, stoppingToken);
                    _logger.LogInformation("Fiscalised ID {Id} with status {Status}. Took {Elapsed}ms.", id, result.Response.FiscalisationStatus, stopwatch.ElapsedMilliseconds);
                    _stats.RecordSuccess();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing fiscalisation batch.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, config.PollIntervalSeconds)), stoppingToken);
        }
    }

    private static ReceiptDetails BuildReceipt(Dictionary<string, object?> record, Dictionary<string, object?>? clientRecord, ServiceConfig config)
    {
        var platform = GetString(record, config.Columns.Platform);
        var transactionEntry = GetString(record, config.Columns.TransactionEntry);
        var clientName = GetString(clientRecord, config.ClientLookup.ClientNameColumn);
        var phone = GetString(clientRecord, config.ClientLookup.TelephoneNoColumn);
        var email = GetString(clientRecord, config.ClientLookup.EmailAddressColumn);
        var address = GetString(clientRecord, config.ClientLookup.ClientAddressColumn);
        var details = GetString(record, config.Columns.Details);
        var dealDate = GetString(record, config.Columns.DealDate);

        var brokerage = GetDecimal(record, config.Columns.Brokerage);
        var amountDue = GetDecimal(record, config.Columns.AmountDue);

        var currency = string.Equals(platform, config.UsdPlatformValue, StringComparison.OrdinalIgnoreCase)
            ? config.UsdCurrency
            : config.DefaultCurrency;

        var receiptTotal = (brokerage + amountDue).ToString("0.00", CultureInfo.InvariantCulture);
        var brokerageText = brokerage.ToString("0.00", CultureInfo.InvariantCulture);

        return new ReceiptDetails
        {
            ReceiptType = config.ReceiptType,
            ReceiptCurrency = currency,
            DeviceId = config.DeviceId,
            InvoiceNo = string.IsNullOrWhiteSpace(transactionEntry) ? "0" : transactionEntry,
            BuyerData = new BuyerData
            {
                BuyerRegisterName = string.IsNullOrWhiteSpace(clientName) ? "Customer name" : clientName,
                BuyerTradeName = string.IsNullOrWhiteSpace(clientName) ? "Customer name" : clientName,
                VatNumber = config.BuyerDefaults.VatNumber,
                BuyerTin = config.BuyerDefaults.BuyerTin,
                BuyerContacts = new BuyerContacts
                {
                    PhoneNo = string.IsNullOrWhiteSpace(phone) ? "client phone number" : phone,
                    Email = string.IsNullOrWhiteSpace(email) ? "email address" : email
                },
                BuyerAddress = new BuyerAddress
                {
                    Province = config.BuyerDefaults.Province,
                    Street = string.IsNullOrWhiteSpace(address) ? "client address" : Trim(address, 100),
                    HouseNo = string.IsNullOrWhiteSpace(address) ? "client address" : address,
                    City = config.BuyerDefaults.City
                }
            },
            ReceiptNotes = string.IsNullOrWhiteSpace(details) ? "details" : details,
            ReceiptDate = string.IsNullOrWhiteSpace(dealDate) ? "" : dealDate,
            ReceiptLines = new List<ReceiptLine>
            {
                new ReceiptLine
                {
                    ReceiptLineType = config.ReceiptLineDefaults.LineType,
                    ReceiptLineNo = 1,
                    ReceiptLineHSCode = config.ReceiptLineDefaults.HsCode,
                    ReceiptLineName = config.ReceiptLineDefaults.Name,
                    ReceiptLinePrice = brokerageText,
                    ReceiptLineQuantity = 1,
                    ReceiptLineTotal = brokerageText,
                    TaxPercent = config.ReceiptLineDefaults.TaxPercent
                }
            },
            ReceiptPayments = new List<ReceiptPayment>
            {
                new ReceiptPayment
                {
                    MoneyTypeCode = config.ReceiptLineDefaults.MoneyTypeCode,
                    PaymentAmount = receiptTotal
                }
            },
            ReceiptTotal = receiptTotal
        };
    }

    private static string GetString(Dictionary<string, object?>? record, string column)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            return string.Empty;
        }

        if (record is null)
        {
            return string.Empty;
        }

        return record.TryGetValue(column, out var value) && value is not null
            ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            : string.Empty;
    }

    private static decimal GetDecimal(Dictionary<string, object?> record, string column)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            return 0m;
        }

        if (!record.TryGetValue(column, out var value) || value is null)
        {
            return 0m;
        }

        if (value is decimal decimalValue)
        {
            return decimalValue;
        }

        if (decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return 0m;
    }

    private static bool TryGetInt(Dictionary<string, object?> record, string column, out int value)
    {
        value = 0;
        if (!record.TryGetValue(column, out var raw) || raw is null)
        {
            return false;
        }

        if (raw is int intValue)
        {
            value = intValue;
            return true;
        }

        return int.TryParse(raw.ToString(), out value);
    }

    private static int GetInt(Dictionary<string, object?> record, string column)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            return 0;
        }

        if (!record.TryGetValue(column, out var raw) || raw is null)
        {
            return 0;
        }

        if (raw is int intValue)
        {
            return intValue;
        }

        return int.TryParse(raw.ToString(), out var parsed) ? parsed : 0;
    }

    private static DateTimeOffset? GetDateTime(Dictionary<string, object?> record, string column)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            return null;
        }

        if (!record.TryGetValue(column, out var raw) || raw is null)
        {
            return null;
        }

        if (raw is DateTimeOffset dto)
        {
            return dto;
        }

        if (raw is DateTime dt)
        {
            return new DateTimeOffset(dt);
        }

        return DateTimeOffset.TryParse(raw.ToString(), out var parsed) ? parsed : null;
    }

    private static int CalculateBackoffSeconds(ServiceConfig config, int retryCount)
    {
        var exponent = Math.Max(0, retryCount - 1);
        var delay = config.RetryBackoffBaseSeconds * Math.Pow(2, exponent);
        return (int)Math.Min(Math.Max(1, delay), config.RetryBackoffMaxSeconds);
    }

    private static string Trim(string value, int max)
    {
        return value.Length <= max ? value : value.Substring(0, max);
    }
}

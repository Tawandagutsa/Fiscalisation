namespace FiscalisationService.Services;

public sealed class ServiceStats
{
    private readonly object _lock = new();
    private DateTimeOffset? _lastRunUtc;
    private DateTimeOffset? _lastSuccessUtc;
    private DateTimeOffset? _lastErrorUtc;
    private string? _lastErrorMessage;
    private int _lastBatchCount;
    private long _totalProcessed;
    private long _totalSuccess;
    private long _totalTimeout;
    private long _totalFailed;

    public void RecordBatch(int count)
    {
        lock (_lock)
        {
            _lastRunUtc = DateTimeOffset.UtcNow;
            _lastBatchCount = count;
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _totalProcessed++;
            _totalSuccess++;
            _lastSuccessUtc = DateTimeOffset.UtcNow;
        }
    }

    public void RecordTimeout(string message)
    {
        lock (_lock)
        {
            _totalProcessed++;
            _totalTimeout++;
            _lastErrorUtc = DateTimeOffset.UtcNow;
            _lastErrorMessage = message;
        }
    }

    public void RecordFailure(string message)
    {
        lock (_lock)
        {
            _totalProcessed++;
            _totalFailed++;
            _lastErrorUtc = DateTimeOffset.UtcNow;
            _lastErrorMessage = message;
        }
    }

    public StatsSnapshot Snapshot()
    {
        lock (_lock)
        {
            return new StatsSnapshot(
                _lastRunUtc,
                _lastSuccessUtc,
                _lastErrorUtc,
                _lastErrorMessage,
                _lastBatchCount,
                _totalProcessed,
                _totalSuccess,
                _totalTimeout,
                _totalFailed);
        }
    }
}

public sealed record StatsSnapshot(
    DateTimeOffset? LastRunUtc,
    DateTimeOffset? LastSuccessUtc,
    DateTimeOffset? LastErrorUtc,
    string? LastErrorMessage,
    int LastBatchCount,
    long TotalProcessed,
    long TotalSuccess,
    long TotalTimeout,
    long TotalFailed);

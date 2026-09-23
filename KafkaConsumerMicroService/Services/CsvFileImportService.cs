using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KafkaConsumerMicroService.Data;

namespace KafkaConsumerMicroService.Services
{
    /// <summary>
    /// Service responsible for parsing CSV input streams and delegating processing of each record.
    /// Keeps controller free of parsing and business responsibilities.
    /// </summary>
    public class CsvFileImportService : IFileImportService
    {
        private readonly IRecordValidator _validator;
        private readonly IRecordDispatcher _dispatcher;

        public CsvFileImportService(IRecordValidator validator, IRecordDispatcher dispatcher)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public async Task<(int processed, string[] errors)> ImportCsvAsync(Stream csvStream, CancellationToken cancellationToken)
        {
            if (csvStream == null) throw new ArgumentNullException(nameof(csvStream));

            var errors = new List<string>();
            var processed = 0;

            using var reader = new StreamReader(csvStream);

            // Read header
            var header = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(header))
            {
                return (0, new[] { "Empty CSV" });
            }

            if (header.StartsWith("\"") && header.EndsWith("\""))
            {
                header = header.Substring(1, header.Length - 2);
            }
            var columns = header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var idxSource = Array.IndexOf(columns, "SourceSystem");
            var idxAccount = Array.IndexOf(columns, "AccountNumber");
            var idxPnL = Array.IndexOf(columns, "PnLAmount");

            if (idxSource < 0 || idxAccount < 0 || idxPnL < 0)
            {
                return (0, new[] { "CSV must contain header columns: SourceSystem,AccountNumber,PnLAmount" });
            }

            string line;
            while (!cancellationToken.IsCancellationRequested && (line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("\"") && line.EndsWith("\""))
                {
                    line = line.Substring(1, line.Length - 2);
                }

                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                try
                {
                    var source = parts.Length > idxSource ? parts[idxSource] : string.Empty;

                    long account = 0;
                    if (parts.Length > idxAccount && !long.TryParse(parts[idxAccount], NumberStyles.Integer, CultureInfo.InvariantCulture, out account))
                    {
                        throw new FormatException($"AccountNumber not a valid integer: '{(parts.Length > idxAccount ? parts[idxAccount] : string.Empty)}'");
                    }

                    double pnl = 0.0;
                    if (parts.Length > idxPnL && !double.TryParse(parts[idxPnL], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out pnl))
                    {
                        throw new FormatException($"PnLAmount not a valid number: '{(parts.Length > idxPnL ? parts[idxPnL] : string.Empty)}'");
                    }

                    var rec = new MarketRecord
                    {
                        RawPayload = line,
                        SourceSystem = source,
                        AccountNumber = account,
                        PnLAmount = pnl,
                        ReceivedAt = DateTimeOffset.UtcNow
                    };

                    rec.IsValid = _validator.IsValid(rec);
                    await _dispatcher.StoreAndBroadcastAsync(rec).ConfigureAwait(false);
                    processed++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Line: '{line}' => {ex.Message}");

                    // Persist invalid record for audit and reporting. Do not duplicate logic: reuse dispatcher.
                    var errRec = new MarketRecord
                    {
                        RawPayload = line,
                        SourceSystem = parts.Length > idxSource ? parts[idxSource] : "<parse_error>",
                        AccountNumber = 0,
                        PnLAmount = 0,
                        IsValid = false,
                        ReceivedAt = DateTimeOffset.UtcNow,
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message, errorType = ex.GetType().Name, processedAt = DateTimeOffset.UtcNow })
                    };

                    try
                    {
                        await _dispatcher.StoreAndBroadcastAsync(errRec).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Swallow dispatcher errors during error reporting to avoid masking original parse issues.
                    }
                }
            }

            return (processed, errors.ToArray());
        }
    }
}

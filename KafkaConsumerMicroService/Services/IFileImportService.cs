using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaConsumerMicroService.Services
{
    public interface IFileImportService
    {
        /// <summary>
        /// Imports CSV data from the provided stream. Returns number of processed rows and any parsing errors.
        /// </summary>
        Task<(int processed, string[] errors)> ImportCsvAsync(Stream csvStream, CancellationToken cancellationToken);
    }
}

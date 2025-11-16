using Snowflake.Data.Client;
using Microsoft.Extensions.Configuration;

namespace HanziWriterLanding.Services
{
    public class SnowflakeService
    {
        private readonly string _connectionString;

        public SnowflakeService(IConfiguration configuration)
        {
            // Store this in appsettings.json / user-secrets as "ConnectionStrings:Snowflake"
            _connectionString = configuration.GetConnectionString("Snowflake")
                ?? throw new InvalidOperationException("Snowflake connection string not configured.");
        }

        /// <summary>
        /// Generate a simple Chinese sentence using the given vocab word.
        /// </summary>
        public async Task<string> GenerateSentenceAsync(string vocab, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(vocab))
                return string.Empty;

            var prompt = $"Using「{vocab}」create a simple sentence in Chinese. Return only the sentence.";

            using var conn = new SnowflakeDbConnection
            {
                ConnectionString = _connectionString
            };

            await conn.OpenAsync(cancellationToken);

            // You can tweak the model name as needed
            const string modelName = "mistral-7b";

            string query = $@"
                select snowflake.cortex.complete(
                    '{modelName}',
                    '{prompt.Replace("'", "''")}'
                ) as result;
            ";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return reader.GetString(0);
            }

            return string.Empty;
        }
    }
}

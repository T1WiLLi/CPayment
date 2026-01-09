using CPayment.Interfaces;
using CPayment.Models;
using CPayment.Services;
using CPayment.Utils;
using System.Text.Json;

namespace CPayment.Providers
{
    public sealed class BlockStreamBitcoinProvider : IProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private string _baseUrl = "https://blockstream.info/api"; // Default mainnet

        public string Name => "Blockstream (Esplora)";

        public void Configure(Network network)
        {
            _baseUrl = network switch
            {
                Network.Main => _baseUrl,
                Network.Test => "https://blockstream.info/testnet/api",
                _ => throw new NotSupportedException($"{nameof(BlockStreamBitcoinProvider)}.{nameof(Configure)} => Unsupported network '{network}'.")
            };
        }

        public async Task<int> GetTipHeightAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/blocks/tip/height";

            using var response = await HttpClientService.Instance.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!int.TryParse(body, out var height)  || height <= 0)
            {
                throw new InvalidOperationException($"{nameof(BlockStreamBitcoinProvider)}.{nameof(GetTipHeightAsync)} => Invalid tip height response: '{body}'.");
            }

            return height;
        }

        public async Task<IReadOnlyList<EsploraTransaction>> GetAddressTransactionsAsync(string address, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException($"{nameof(BlockStreamBitcoinProvider)}.{GetAddressTransactionsAsync} => Address must be provided.", nameof(address));
            }

            var url = $"{_baseUrl}/address/{Uri.EscapeDataString(address)}/txs";

            using var response = await HttpClientService.Instance.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var txs = await JsonSerializer.DeserializeAsync<List<EsploraTransaction>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            return txs ?? [];
        }

        public async Task<EsploraTransaction> GetTransactionAsync(string txId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(txId))
            {
                throw new ArgumentException($"{nameof(BlockStreamBitcoinProvider)}.{GetTransactionAsync} => TxId must be provided.", nameof(txId));
            }

            var url = $"{_baseUrl}/tx/{Uri.EscapeDataString(txId)}";

            using var response = await HttpClientService.Instance.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var tx = await JsonSerializer.DeserializeAsync<EsploraTransaction>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            return tx is null
                ? throw new InvalidOperationException($"{nameof(BlockStreamBitcoinProvider)}.{nameof(GetTransactionAsync)} => Null transaction response for tx '{txId}'.")
                : tx;
        }
    }
}

using CPayment.Interfaces;
using CPayment.Models;
using CPayment.Services;
using CPayment.Utils;
using System.Net.Http.Json;
using System.Text.Json;
using FeeRate = NBitcoin.FeeRate;
using NBitcoin;
using System.Text;
using Network = CPayment.Utils.Network;
using NBitcoinNetwork = NBitcoin.Network;

namespace CPayment.Providers
{
    public sealed class BlockStreamBitcoinProvider : IProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private string _baseUrl = "https://blockstream.info/api"; // Default mainnet
        private NBitcoinNetwork _nbitcoinNetwork = NBitcoinNetwork.Main;

        public string Name => "Blockstream (Esplora)";

        public void Configure(Network network)
        {
            _baseUrl = network switch
            {
                Network.Main => "https://blockstream.info/api",
                Network.Test => "https://blockstream.info/testnet/api",
                _ => throw new NotSupportedException($"{nameof(BlockStreamBitcoinProvider)}.{nameof(Configure)} => Unsupported network '{network}'.")
            };

            _nbitcoinNetwork = network switch
            {
                Network.Main => NBitcoinNetwork.Main,
                Network.Test => NBitcoinNetwork.TestNet,
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

        public async Task<IReadOnlyList<SpendableOutput>> GetSpendableOutputsAsync(string address, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException($"{nameof(BlockStreamBitcoinProvider)}.{nameof(GetSpendableOutputsAsync)} => Address must be provided.", nameof(address));
            }

            var url = $"{_baseUrl}/address/{Uri.EscapeDataString(address)}/utxo";

            using var response = await HttpClientService.Instance.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var utxos = await JsonSerializer.DeserializeAsync<List<EsploraUtxo>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (utxos is null || utxos.Count == 0)
            {
                return Array.Empty<SpendableOutput>();
            }

            var tipHeight = await GetTipHeightAsync(cancellationToken).ConfigureAwait(false);
            var depositAddress = BitcoinAddress.Create(address, _nbitcoinNetwork);
            var depositScript = depositAddress.ScriptPubKey;

            var outputs = new List<SpendableOutput>(utxos.Count);
            foreach (var utxo in utxos)
            {
                var confirmations = utxo.Status.BlockHeight is null
                    ? 0
                    : CalculateConfirmations(utxo.Status.BlockHeight.Value, tipHeight);

                var coin = new Coin(
                    new OutPoint(new uint256(utxo.TxId), (uint)utxo.Vout),
                    new TxOut(Money.Satoshis(utxo.ValueSats), depositScript));

                outputs.Add(new SpendableOutput(coin, confirmations));
            }

            return outputs;
        }

        public async Task BroadcastAsync(string txHex, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(txHex))
            {
                throw new ArgumentException($"{nameof(BlockStreamBitcoinProvider)}.{nameof(BroadcastAsync)} => Tx hex must be provided.", nameof(txHex));
            }

            var url = $"{_baseUrl}/tx";

            using var content = new StringContent(txHex, Encoding.UTF8, "text/plain");
            using var response = await HttpClientService.Instance.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task<FeeRate> GetSweepFeeRateAsync(FeePolicy policy, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/fee-estimates";

            Dictionary<string, double>? estimates;
            try
            {
                estimates = await HttpClientService.Instance
                    .GetFromJsonAsync<Dictionary<string, double>>(url, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                estimates = null;
            }

            double targetVbyte = policy switch
            {
                FeePolicy.High => TryGet(estimates, "2") ?? 10d,
                FeePolicy.Medium => TryGet(estimates, "6") ?? 5d,
                _ => TryGet(estimates, "12") ?? 1d
            };

            var satPerKvb = (long)Math.Ceiling(targetVbyte * 1000d);
            if (satPerKvb < 1) satPerKvb = 1;

            return new FeeRate(NBitcoin.Money.Satoshis(satPerKvb));

            static double? TryGet(Dictionary<string, double>? dict, string key) =>
                dict is not null && dict.TryGetValue(key, out var v) ? v : null;
        }

        private static int CalculateConfirmations(int blockHeight, int tipHeight)
        {
            if (blockHeight <= 0 || tipHeight < blockHeight)
            {
                return 0;
            }

            return (tipHeight - blockHeight) + 1;
        }
    }
}

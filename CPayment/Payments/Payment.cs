using CPayment.Configuration;
using CPayment.Exceptions;
using CPayment.Interfaces;
using CPayment.Models;
using CPayment.Public;
using CPayment.Services;
using CPayment.Utils;
using NBitcoin;
using NBitcoin.Policy;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CPayment.Payments;

public sealed class Payment
{
    private const decimal SatoshisPerBtc = 100_000_000m;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly long _amountSatoshis;
    private readonly CPaymentOptions _options;
    private readonly IProvider _provider;
    private readonly Key _depositKey;
    private readonly NBitcoin.Network _nbitcoinNetwork;

    public Payment(decimal amount, PaymentType currency, PaymentMetadata metadata)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentNullException.ThrowIfNull(metadata);

        if (currency != PaymentType.BTC)
        {
            throw new NotSupportedException($"{nameof(Payment)} => Payment type '{currency}' is not supported yet.");
        }

        Amount = amount;
        Currency = currency;
        Metadata = metadata;

        _options = CPaymentExtensions.Options;
        _provider = _options.Provider ?? throw new CPaymentConfigurationException($"{nameof(Payment)} => Provider is not configured.");
        _provider.Configure(_options.Network);

        _amountSatoshis = ToSatoshis(amount);

        var derived = AddressDerivationService.DeriveDeposit(_options, metadata);
        _depositKey = derived.PrivateKey;
        _nbitcoinNetwork = derived.Network;
        PaymentTo = derived.Address;
    }

    public decimal Amount { get; }

    public PaymentType Currency { get; }

    public PaymentMetadata Metadata { get; }

    public string PaymentTo { get; }

    public Task<PaymentVerificationResult> VerifyAsync(
        VerifyOptions? verifyOptions = null,
        CancellationToken cancellationToken = default)
    {
        verifyOptions ??= new VerifyOptions();

        var minConfirmations = verifyOptions.MinConfirmations ?? _options.DefaultConfirmations;
        if (minConfirmations < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(verifyOptions.MinConfirmations), "Min confirmations must be >= 0.");
        }

        return VerifyInternalAsync(minConfirmations, cancellationToken);
    }

    public async Task<PaymentVerificationResult> WaitUntilConfirmedAsync(
        VerifyOptions? verifyOptions = null,
        TimeSpan? pollInterval = null,
        TimeSpan? timeout = null,
        Action<PaymentVerificationResult>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        pollInterval ??= TimeSpan.FromSeconds(15);
        verifyOptions ??= new VerifyOptions();

        var start = DateTimeOffset.UtcNow;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await VerifyAsync(verifyOptions, cancellationToken).ConfigureAwait(false);
            onProgress?.Invoke(result);

            if (result.Status == PaymentStatus.Confirmed)
            {
                return result;
            }

            if (timeout is not null && DateTimeOffset.UtcNow - start >= timeout.Value)
            {
                throw new TimeoutException($"Payment not confirmed within {timeout.Value}.");
            }

            await Task.Delay(pollInterval.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SweepAsync(CancellationToken cancellationToken = default)
    {
        var sweep = _options.Wallet?.AutoSweep;
        if (sweep is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Wallet!.MasterAddress))
        {
            throw new CPaymentConfigurationException($"{nameof(Payment)}.{nameof(SweepAsync)} => Master address is not configured.");
        }

        var utxos = await _provider.GetSpendableOutputsAsync(PaymentTo, cancellationToken).ConfigureAwait(false);
        if (utxos.Count == 0)
        {
            return;
        }

        var eligible = utxos
            .Where(x => x.Confirmations >= sweep.MinConfirmations)
            .ToList();

        if (eligible.Count == 0)
        {
            return;
        }

        var totalSats = eligible.Sum(x => x.Coin.Amount.Satoshi);
        var minSweepSats = ToSatoshis(sweep.MinSweepAmount);

        if (totalSats < minSweepSats)
        {
            return;
        }

        var destination = BitcoinAddress.Create(_options.Wallet.MasterAddress, _nbitcoinNetwork);

        var coins = eligible.Select(x => x.Coin).ToArray();

        var feeRate = await _provider.GetSweepFeeRateAsync(sweep.FeePolicy, cancellationToken).ConfigureAwait(false);

        var builder = _nbitcoinNetwork.CreateTransactionBuilder();
        builder.OptInRBF = sweep.EnableRbf;

        builder.AddCoins(coins);
        builder.AddKeys(_depositKey);

        builder.SendAll(destination);
        builder.SendEstimatedFees(feeRate);

        var tx = builder.BuildTransaction(sign: true);

        if (!builder.Verify(tx, out var policyErrors))
        {
            throw new InvalidOperationException(
                $"{nameof(Payment)}.{nameof(SweepAsync)} => Built transaction failed policy check: {FormatPolicyErrors(policyErrors)}");
        }

        var sentToDestination = tx.Outputs
            .Where(o => o.ScriptPubKey == destination.ScriptPubKey)
            .Sum(o => o.Value.Satoshi);

        if (sentToDestination <= 0 || sentToDestination < minSweepSats)
        {
            return;
        }

        await _provider.BroadcastAsync(tx.ToHex(), cancellationToken).ConfigureAwait(false);
    }

    private async Task<PaymentVerificationResult> VerifyInternalAsync(
        int minConfirmations,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<EsploraTransaction> transactions;
        try
        {
            transactions = await _provider.GetAddressTransactionsAsync(PaymentTo, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return PaymentVerificationResult.Unknown(
                $"Failed to query provider '{_provider.Name}': {ex.Message}");
        }

        if (transactions.Count == 0)
        {
            return PaymentVerificationResult.NotFound;
        }

        PaymentCandidate? bestMatch = null;

        foreach (var tx in transactions)
        {
            var amountToAddress = SumOutputsToAddress(tx, PaymentTo);
            if (amountToAddress < _amountSatoshis)
            {
                continue;
            }

            var confirmations = await GetConfirmationsAsync(tx, cancellationToken).ConfigureAwait(false);
            if (confirmations < 0)
            {
                return PaymentVerificationResult.Unknown(
                    $"Unable to compute confirmations for tx '{tx.TxId}'.");
            }

            if (bestMatch is null || confirmations > bestMatch.Value.Confirmations)
            {
                bestMatch = new PaymentCandidate(tx.TxId, amountToAddress, confirmations);
            }
        }

        if (bestMatch is null)
        {
            return PaymentVerificationResult.NotFound;
        }

        var amountBtc = FromSatoshis(bestMatch.Value.AmountSatoshis);

        return bestMatch.Value.Confirmations >= minConfirmations
            ? PaymentVerificationResult.Confirmed(bestMatch.Value.TxId, bestMatch.Value.Confirmations, amountBtc)
            : PaymentVerificationResult.Unconfirmed(bestMatch.Value.TxId, bestMatch.Value.Confirmations, amountBtc);
    }

    private async Task<int> GetConfirmationsAsync(
        EsploraTransaction tx,
        CancellationToken cancellationToken)
    {
        if (!tx.Status.Confirmed)
        {
            return 0;
        }

        if (tx.Status.BlockHeight is null)
        {
            return -1;
        }

        int tipHeight;
        try
        {
            tipHeight = await _provider.GetTipHeightAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return -1;
        }

        if (tipHeight <= 0)
        {
            return -1;
        }

        return CalculateConfirmations(tx.Status.BlockHeight.Value, tipHeight);
    }

    private string GetBaseEsploraUrl()
    {
        return _options.Network switch
        {
            Utils.Network.Main => "https://blockstream.info/api",
            Utils.Network.Test => "https://blockstream.info/testnet/api",
            _ => throw new NotSupportedException($"{nameof(Payment)} => Unsupported network '{_options.Network}'.")
        };
    }

    private async Task<FeeRate> GetFeeRateAsync(FeePolicy policy, CancellationToken cancellationToken)
    {
        var url = $"{GetBaseEsploraUrl()}/fee-estimates";

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

        return new FeeRate(Money.Satoshis(satPerKvb));

        static double? TryGet(Dictionary<string, double>? dict, string key) =>
            dict is not null && dict.TryGetValue(key, out var v) ? v : null;
    }

    private static string FormatPolicyErrors(TransactionPolicyError[]? errors)
    {
        if (errors is null || errors.Length == 0)
        {
            return "Unknown policy error.";
        }

        static string Describe(TransactionPolicyError e)
        {
            var baseMsg = e.ToString();

            return e switch
            {
                FeeTooLowPolicyError f =>
                    $"{baseMsg} (fee={f.Fee}, expectedMin={f.ExpectedMinFee})",

                FeeTooHighPolicyError f =>
                    $"{baseMsg} (fee={f.Fee}, expectedMax={f.ExpectedMaxFee})",

                DustPolicyError d =>
                    $"{baseMsg} (value={d.Value}, dustThreshold={d.DustThreshold})",

                TransactionSizePolicyError s =>
                    $"{baseMsg} (actualSize={s.ActualSize}, maxSize={s.MaximumSize})",

                OutputPolicyError o =>
                    $"{baseMsg} (outputIndex={o.OutputIndex})",

                InputPolicyError i =>
                    $"{baseMsg} (inputIndex={i.InputIndex}, outpoint={i.OutPoint})",

                DuplicateInputPolicyError di =>
                    $"{baseMsg} (outpoint={di.OutPoint}, inputIndices=[{string.Join(",", di.InputIndices)}])",
                _ =>
                    baseMsg
            };
        }

        return string.Join("; ", errors.Select(Describe));
    }

    private static long SumOutputsToAddress(EsploraTransaction tx, string address)
    {
        long total = 0;

        foreach (var vout in tx.Vout)
        {
            if (string.Equals(vout.ScriptPubKeyAddress, address, StringComparison.OrdinalIgnoreCase))
            {
                checked
                {
                    total += vout.ValueSats;
                }
            }
        }

        return total;
    }

    private static int CalculateConfirmations(int blockHeight, int tipHeight)
    {
        if (blockHeight <= 0 || tipHeight < blockHeight)
        {
            return 0;
        }

        return (tipHeight - blockHeight) + 1;
    }

    private static long ToSatoshis(decimal btc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(btc);

        var sats = decimal.Round(btc * SatoshisPerBtc, 0, MidpointRounding.AwayFromZero);

        if (sats > long.MaxValue)
        {
            throw new OverflowException($"{nameof(Payment)} => BTC amount '{btc}' is too large.");
        }

        return (long)sats;
    }

    private static decimal FromSatoshis(long satoshis) => satoshis / SatoshisPerBtc;

    private readonly record struct PaymentCandidate(string TxId, long AmountSatoshis, int Confirmations);
}

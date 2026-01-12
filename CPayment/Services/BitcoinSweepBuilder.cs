using CPayment.Models;
using NBitcoin;
using NBitcoin.Policy;

namespace CPayment.Services;

internal sealed class BitcoinSweepBuilder : ISweepBuilder
{
    private readonly Network _network;

    public BitcoinSweepBuilder(Network network)
    {
        _network = network ?? throw new ArgumentNullException(nameof(network));
    }

    public Transaction BuildSweep(Key depositKey, IReadOnlyList<SpendableOutput> utxos, BitcoinAddress destination, FeeRate feeRate, bool enableRbf)
    {
        ArgumentNullException.ThrowIfNull(depositKey);
        ArgumentNullException.ThrowIfNull(utxos);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(feeRate);

        if (utxos.Count == 0)
        {
            throw new ArgumentException("No spendable outputs provided.", nameof(utxos));
        }

        var coins = utxos.Select(u => u.Coin).ToArray();

        var builder = _network.CreateTransactionBuilder();
        builder.OptInRBF = enableRbf;

        builder.AddCoins(coins);
        builder.AddKeys(depositKey);

        builder.SendAll(destination);
        builder.SendEstimatedFees(feeRate);

        var tx = builder.BuildTransaction(sign: true);

        if (!builder.Verify(tx, out var policyErrors))
        {
            throw new InvalidOperationException(
                $"Sweep transaction failed policy check: {FormatPolicyErrors(policyErrors)}");
        }

        return tx;
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
}

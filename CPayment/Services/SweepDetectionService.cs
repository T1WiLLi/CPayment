using CPayment.Interfaces;

namespace CPayment.Services;

internal static class SweepDetectionService
{
    public static async Task<bool> WasSweptAsync(string address, IProvider provider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var utxos = await provider.GetSpendableOutputsAsync(address, cancellationToken).ConfigureAwait(false);
        if (utxos.Count > 0)
        {
            return false; // still spendable funds
        }

        var txs = await provider.GetAddressTransactionsAsync(address, cancellationToken).ConfigureAwait(false);
        if (txs.Count == 0)
        {
            return false; // never funded
        }

        // Heuristic: 1 input, 2 outputs, and our address as the prevout.
        return txs.Any(tx =>
            tx.Vin.Count == 1 &&
            tx.Vout.Count == 2 &&
            tx.Vin.Any(v =>
                string.Equals(v.Prevout?.ScriptPubKeyAddress, address, StringComparison.OrdinalIgnoreCase)));
    }
}

using CPayment.Models;
using NBitcoin;

namespace CPayment.Services;

internal interface ISweepBuilder
{
    Transaction BuildSweep(Key depositKey, IReadOnlyList<SpendableOutput> utxos, BitcoinAddress destination, FeeRate feeRate, bool enableRbf);
}

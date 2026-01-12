using CPayment.Models;
using CPayment.Utils;
using FeeRate = NBitcoin.FeeRate;

namespace CPayment.Interfaces;

public interface IProvider
{
    string Name { get; }

    void Configure(Network network);

    Task<int> GetTipHeightAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EsploraTransaction>> GetAddressTransactionsAsync(string address, CancellationToken cancellationToken = default);

    Task<EsploraTransaction> GetTransactionAsync(string txId, CancellationToken cancellationToken = default);

    Task<FeeRate> GetSweepFeeRateAsync(FeePolicy policy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EsploraUtxo>> GetUtxosAsync(string address, CancellationToken cancellationToken = default);

    Task BroadcastAsync(string txHex, CancellationToken cancellationToken = default);
}

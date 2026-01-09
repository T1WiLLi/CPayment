using CPayment.Models;
using CPayment.Utils;

namespace CPayment.Interfaces;

public interface IProvider
{
    string Name { get; }

    void Configure(Network network);

    Task<int> GetTipHeightAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EsploraTransaction>> GetAddressTransactionsAsync(string address, CancellationToken cancellationToken = default);

    Task<EsploraTransaction> GetTransactionAsync(string txId, CancellationToken cancellationToken = default);
}

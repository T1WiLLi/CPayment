using NBitcoin;

namespace CPayment.Models;

public sealed class SpendableOutput
{
    public SpendableOutput(Coin coin, int confirmations)
    {
        Coin = coin ?? throw new ArgumentNullException(nameof(coin));
        Confirmations = confirmations;
    }

    public Coin Coin { get; }

    public int Confirmations { get; }
}

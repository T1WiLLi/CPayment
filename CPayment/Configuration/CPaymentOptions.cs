using CPayment.Interfaces;
using CPayment.Utils;

namespace CPayment.Configuration;

public sealed class CPaymentOptions
{
    public Network Network { get; internal set; }
    public IProvider? Provider { get; internal set; } // TODO: Change to IProvider when available

    public int DefaultConfirmations { get; internal set; } = 1;

    public string? DerivationSalt { get; internal set; }
    public IReadOnlyList<string> RequiredDerivationKeys { get; internal set; } = [];

    public WalletOptions? Wallet { get; internal set; }
}

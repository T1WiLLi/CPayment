using CPayment.Utils;

namespace CPayment.Configuration;

public sealed class AutoSweepOptions
{
    public int MinConfirmations { get; set; } = 1;
    public FeePolicy FeePolicy { get; set; } = FeePolicy.Medium;
    public bool EnabledRbf { get; set; } = true;
    public decimal MinUtxoAmount { get; set; } = 0.0001m;
}

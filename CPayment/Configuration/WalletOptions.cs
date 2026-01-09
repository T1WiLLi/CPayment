namespace CPayment.Configuration;

public sealed class WalletOptions
{
    public string? Mnemonic { get; internal set; }
    public string? Xpub { get; internal set; }
    public string? MasterAddress { get; internal set; }
    public AutoSweepOptions? AutoSweep { get; internal set; }
}

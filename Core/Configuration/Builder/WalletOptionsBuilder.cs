namespace CPayment.Configuration.Builder;

public sealed class WalletOptionsBuilder
{
    private readonly WalletOptions _wallet = new();

    public WalletOptionsBuilder UseMnemonic(string mnemonic)
    {
        _wallet.Mnemonic = mnemonic;
        return this;
    }

    public WalletOptionsBuilder UseXpub(string xpub) 
    {
        _wallet.Xpub = xpub;
        return this;
    }

    public WalletOptionsBuilder SetMasterAddress(string address)
    {
        _wallet.MasterAddress = address;
        return this;
    }

    public WalletOptionsBuilder ConfigureSweep(AutoSweepOptions options)
    {
        _wallet.AutoSweep = options;
        return this;
    }

    internal WalletOptions Build() => _wallet;
}

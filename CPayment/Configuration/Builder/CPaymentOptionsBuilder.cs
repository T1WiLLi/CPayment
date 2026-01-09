using CPayment.Utils;

namespace CPayment.Configuration.Builder
{
    public sealed class CPaymentOptionsBuilder
    {
        private readonly CPaymentOptions _options = new();

        public CPaymentOptionsBuilder UseNetwork(Network network)
        {
            _options.Network = network;
            return this;
        }

        public CPaymentOptionsBuilder UseProvider(object provider)
        {
            _options.Provider = provider;
            return this;
        }

        public CPaymentOptionsBuilder SetDefaultConfirmations(int confirmations)
        {
            _options.DefaultConfirmations = confirmations;
            return this;
        }

        public CPaymentOptionsBuilder SetDerivationSalt(string salt)
        {
            _options.DerivationSalt = salt;
            return this;
        }

        public CPaymentOptionsBuilder SetDerivationRequiredKeys(params string[] keys)
        {
            _options.RequiredDerivationKeys = keys;
            return this;
        }

        public CPaymentOptionsBuilder UseBitcoinWallet(Action<WalletOptionsBuilder> configure)
        {
            var builder = new WalletOptionsBuilder();
            configure(builder);
            _options.Wallet = builder.Build();
            return this;
        }

        internal CPaymentOptions Build() => _options;
    }
}
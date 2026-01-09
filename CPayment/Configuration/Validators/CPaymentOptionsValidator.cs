using CPayment.Exceptions;

namespace CPayment.Configuration.Validators;

public static class CPaymentOptionsValidator
{
    public static void Validate(CPaymentOptions options)
    {
        if (options.Provider is null)
        {
            throw new CPaymentConfigurationException($"{nameof(CPaymentOptionsValidator)}.{nameof(Validate)} => Provider must be configured.");
        }

        if (options.DefaultConfirmations < 0 || options.DefaultConfirmations > 100)
        {
            throw new CPaymentConfigurationException($"{nameof(CPaymentOptionsValidator)}.{nameof(Validate)} => DefaultConfirmations must be between 0 and 100.");
        }

        ValidateDerivation(options);
        ValidateWallet(options.Wallet);
    }

    private static void ValidateDerivation(CPaymentOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DerivationSalt))
        {
            throw new CPaymentConfigurationException($"{nameof(CPaymentOptionsValidator)}.{nameof(ValidateDerivation)} => DerivationSalt must be configured.");
        }
        
        if (options.DerivationSalt.Length < 8)
        {
            throw new CPaymentConfigurationException($"{nameof(CPaymentOptionsValidator)}.{nameof(ValidateDerivation)} => DerivationSalt must be at least 8 characters long.");
        }

        if (options.RequiredDerivationKeys.Count == 0)
        {
            throw new CPaymentConfigurationException($"{nameof(CPaymentOptionsValidator)}.{nameof(ValidateDerivation)} => RequiredDerivationKeys must be greater than 0.");
        }

        if (options.RequiredDerivationKeys.Any(string.IsNullOrWhiteSpace))
        {
            throw new CPaymentConfigurationException($"{nameof(CPaymentOptionsValidator)}.{nameof(ValidateDerivation)} => RequiredDerivationKeys cannot contain null or empty values.");
        }
    }

    private static void ValidateWallet(WalletOptions? wallet)
    {
        if (wallet is null)
        {
            throw new CPaymentConfigurationException($"{nameof(CPaymentOptionsValidator)}.{nameof(ValidateWallet)} => Wallet options can't be NULL.");
        }

        if (!string.IsNullOrWhiteSpace(wallet.Mnemonic))
        {
            var wordCount = wallet.Mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount is not (12 or 15 or 18 or 21 or 24))
            {
                throw new CPaymentConfigurationException($"{nameof(CPaymentOptionsValidator)}.{nameof(ValidateWallet)} => Mnemonic must be a valid BIP-39 word count.");
            }
        }

        if (string.IsNullOrWhiteSpace(wallet.MasterAddress))
        {
            throw new CPaymentConfigurationException($"{nameof(CPaymentOptionsValidator)}.{nameof(ValidateWallet)} => Master address must be configured.");
        }

        if (wallet.AutoSweep is not null)
        {
            if (wallet.AutoSweep.MinConfirmations < 0)
            {
                throw new CPaymentConfigurationException($"{nameof(CPaymentOptionsValidator)}.{nameof(ValidateWallet)} => Auto-sweep MinConfirmations must be >= 0.");
            }

            if (wallet.AutoSweep.MinUtxoAmount <= 0)
            {
                throw new CPaymentConfigurationException($"{nameof(CPaymentOptionsValidator)}.{nameof(ValidateWallet)} => Auto-sweep MinSweepAmount must be greater than zero.");
            }
        }
    }
}

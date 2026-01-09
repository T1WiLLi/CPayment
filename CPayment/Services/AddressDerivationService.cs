using CPayment.Configuration;
using CPayment.Exceptions;
using CPayment.Payments;
using NBitcoin;
using System.Security.Cryptography;
using System.Text;

namespace CPayment.Services
{
    internal static class AddressDerivationService
    {
        /// <summary>
        /// Derives a unique deposit address based on global config + payment metadata.
        /// Uses BIP84 (native segwit) path: m/84'/coin_type'/0'/0/index
        /// Index is deterministically derived via HMAC-SHA256 over (salt + canonical metadata).
        /// </summary>
        public static string DeriveDepositAddress(CPaymentOptions options, PaymentMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(metadata);

            if (options.Wallet is null)
            {
                throw new CPaymentConfigurationException(
                    $"{nameof(AddressDerivationService)}.{nameof(DeriveDepositAddress)} => Wallet is not configured.");
            }

            if (string.IsNullOrWhiteSpace(options.Wallet.Mnemonic))
            {
                throw new CPaymentConfigurationException(
                    $"{nameof(AddressDerivationService)}.{nameof(DeriveDepositAddress)} => Wallet mnemonic is not configured.");
            }

            if (string.IsNullOrWhiteSpace(options.DerivationSalt))
            {
                throw new CPaymentConfigurationException(
                    $"{nameof(AddressDerivationService)}.{nameof(DeriveDepositAddress)} => Derivation salt is not configured.");
            }

            EnsureRequiredKeys(options, metadata);

            var nbitcoinNetwork = options.Network switch
            {
                Utils.Network.Main => Network.Main,
                Utils.Network.Test => Network.TestNet,
                _ => throw new NotSupportedException(
                        $"{nameof(AddressDerivationService)}.{nameof(DeriveDepositAddress)} => Unsupported network '{options.Network}'.")
            };

            var canonical = BuildCanonicalMetadata(options.RequiredDerivationKeys, metadata);

            var mnemonic = new Mnemonic(options.Wallet.Mnemonic);

            var master = mnemonic.DeriveExtKey();

            var index = DeriveIndex(
                master.PrivateKey.ToBytes(),
                options.DerivationSalt!,
                canonical
            );

            var coinType = options.Network == Utils.Network.Main ? 0 : 1;

            var path = new KeyPath($"84'/{coinType}'/0'/0/{index}");

            var key = master.Derive(path);

            var address = key.PrivateKey
                .PubKey
                .GetAddress(ScriptPubKeyType.Segwit, nbitcoinNetwork);

            return address.ToString();
        }

        private static void EnsureRequiredKeys(CPaymentOptions options, PaymentMetadata metadata)
        {
            if (options.RequiredDerivationKeys is null || options.RequiredDerivationKeys.Count == 0)
            {
                throw new CPaymentConfigurationException(
                    $"{nameof(AddressDerivationService)}.{nameof(EnsureRequiredKeys)} => Derivation required keys are not configured.");
            }

            foreach (var key in options.RequiredDerivationKeys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new CPaymentConfigurationException(
                        $"{nameof(AddressDerivationService)}.{nameof(EnsureRequiredKeys)} => Derivation required keys contain an empty entry.");
                }

                if (!metadata.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException(
                        $"{nameof(AddressDerivationService)}.{nameof(EnsureRequiredKeys)} => Payment metadata is missing required derivation key '{key}'.");
                }
            }
        }

        private static string BuildCanonicalMetadata(
            IReadOnlyList<string> requiredKeys,
            PaymentMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(requiredKeys);
            ArgumentNullException.ThrowIfNull(metadata);

            var sb = new StringBuilder();

            for (var i = 0; i < requiredKeys.Count; i++)
            {
                var key = requiredKeys[i];
                var value = metadata[key].Trim();

                sb.Append(key.Trim());
                sb.Append('=');
                sb.Append(value);

                if (i < requiredKeys.Count - 1)
                {
                    sb.Append('\n');
                }
            }

            return sb.ToString();
        }

        private static uint DeriveIndex(
            byte[] seed,
            string salt,
            string canonicalMetadata)
        {
            ArgumentNullException.ThrowIfNull(seed);
            ArgumentNullException.ThrowIfNull(salt);
            ArgumentNullException.ThrowIfNull(canonicalMetadata);

            var keyMaterial = SHA256.HashData(
                Concat(seed, Encoding.UTF8.GetBytes(salt)));

            var data = Encoding.UTF8.GetBytes(canonicalMetadata);

            byte[] hmac;
            using (var h = new HMACSHA256(keyMaterial))
            {
                hmac = h.ComputeHash(data);
            }

            var raw = BitConverter.ToUInt32(hmac, 0);
            return raw % 10_000_000u;
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            ArgumentNullException.ThrowIfNull(a);
            ArgumentNullException.ThrowIfNull(b);

            var r = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }
    }
}
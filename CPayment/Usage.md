# CPayment Planification of Usage Guide

The CPayment library provides a simple and efficient way to handle
crypto-based payments in your applications.

I want CPayment to be simple, while still being powerful, secure and
flexible.

code example: 
```csharp
using CPayment;

public class Program
{
	public static void Main(string[] args)
	{
		// Global configuration for CPayment
		CPayment.Configure(cfg => 
		{
			cfg.UseNetwork(Network.Main); // BTC Mainnet or BTC Testnet
			cfg.UseProvider(new BlockStreamBitcoinProvider()); // Built-in Provider for BTC

			cfg.SetDefaultConfirmations(3); // Default confirmations to consider a payment as confirmed (Optional)
			cfg.SetDerivationSalt("your-unique-nonce"); // Nonce to use for address derivation
			cfg.SetDerivationRequiredKeys("userId", "orderId"); // Required cfg configuration keys for address derivation

			cfg.UseBitcoinWallet(wallet => 
			{
				wallet.UseMnemonic("your-mnemonic-phrase-here"); // Instead, you can also do wallet.UseXpub("your-xpub-here"); but to auto-sweep u need to configure a Signer which is not the case with Mnemonic
				wallet.SetMasterAddress("your-master-address-here");

				// If using Xpub, to have auto-sweep enabled you will need to provide a signer to automatically signed CPayment auto-generated PBSTs
				// wallet.UseSigner(new BitcoinCoreSigner("http://localhost:8332", "rpc-username", "rpc-password"));

				wallet.ConfigureSweep(new AutoSweepOptions
				{
					MinConfirmations = 1,
					FeePolicy = FeePolicy.Medium, // Low, Medium, High
					EnableRbf = true, // Enable Replace-By-Fee
					MinSweepAmount = 0.00001m // Minimum amount to sweep // Can also use PaymentConverter here!
				});
			});
		});


		// Initialize a payment
		var payment = PaymentFactory.Create(
			amount: 0.00001m, (Or user can do a PaymentConverter.Convert(12.32, PaymentConverter.USD); // Amount in BTC
			currency: PaymentType.BTC,
			metadata: PaymentMetadata.Create(
				("userId", "user-1234"),
				("orderId", "order-5678")
			)
		);

		Console.WriteLine($"Deposit address: {payment.PaymentTo}");

		// We then query the blockchain to check for transfer status
		var result = await payment.VerifyAsync(new VerifyOptions
		{
			MinConfirmations = 3,
		});

		// Status can be "NotFound", "Unconfirmed", "Confirmed", "Unknown"
		Console.WriteLine($"Payment status: {result.Status} (tx: {result.TxId ?? "n/a"})");

		if (result.Status == PaymentStatus.Confirmed)
		{
			// Sweep funds to master address if auto-sweep is enabled
			await payment.SweepAsync();
		}
	}
}

// High-level representation of a payment
public class Payment
{
}

public class CPayment
{
}

public class VerifyOptions
{
}

public enum Network
{
	public Main;
	public Test;
}

public enum PaymentType
{
}
```

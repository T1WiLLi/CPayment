using CPayment.Configuration;
using CPayment.Payments;
using CPayment.Providers;
using CPayment.Public;
using CPayment.Utils;

namespace Tools;

internal static class Program
{
    // Testnet wallet (server/master) derived from testnetadresse1.txt
    private const string ServerMnemonic = "onion object mandate dolphin despair when nephew load cream radio home peasant";
    private const string ServerMasterAddress = "tb1ql746wryfjkq0xl40x5gawwcnz69w8xhdvwxswm"; // P2WPKH

    // Client demo wallet (sender) from testnetadresse2.txt
    private const string ClientFundingAddress = "tb1q4y069fdj67avek2d6580dgvr3q5cpngdjsq46q"; // P2WPKH

    public static async Task Main()
    {
        Console.WriteLine("CPayment - Testnet live flow");
        Console.WriteLine("----------------------------");

        ConfigureCPayment();

        Console.WriteLine($"Server master address (sweep target): {ServerMasterAddress}");
        Console.WriteLine($"Client funding address (ask faucet here): {ClientFundingAddress}");

        var payment = PaymentFactory.Create(
            amount: PaymentConverter.Convert(9.99m, PaymentSupportedFiat.CAD, PaymentType.BTC), // adjust as needed
            currency: PaymentType.BTC,
            metadata: PaymentMetadata.Create(
                ("userId", "demo-user"),
                ("orderId", "order-0001"))
        );

        Console.WriteLine($"Deposit address for this payment: {payment.PaymentTo}");
        Console.WriteLine();
        Console.WriteLine("1) Fund the client address from a testnet faucet");
        Console.WriteLine("2) Send the payment amount from client to deposit address above");
        Console.WriteLine("Press ENTER to verify the payment once you've broadcast the tx...");
        Console.WriteLine($"Send this amount of BTC: {payment.Amount}");
        Console.ReadLine();

        Console.WriteLine("Waiting for confirmation (poll 15s, timeout 15m). Press Ctrl+C to cancel.");

        PaymentVerificationResult result;
        try
        {
            result = await payment.WaitUntilConfirmedAsync(
                new VerifyOptions { MinConfirmations = 1 },
                pollInterval: TimeSpan.FromSeconds(15),
                timeout: TimeSpan.FromMinutes(15),
                onProgress: r =>
                {
                    Console.WriteLine($"[{DateTimeOffset.UtcNow:HH:mm:ss}] Status={r.Status}, Conf={r.Confirmations}, Tx={r.TxId ?? "n/a"}");
                });
        }
        catch (TimeoutException)
        {
            Console.WriteLine("Timed out waiting for confirmation.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Payment confirmed! TxId: {result.TxId}, Confirmations: {result.Confirmations}, Amount: {result.Amount} BTC");

        Console.WriteLine("Attempting auto-sweep to master address...");
        try
        {
            await payment.SweepAsync();
            Console.WriteLine("Sweep submitted (check Blockstream for broadcast).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sweep failed: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Done.");
    }

    private static void ConfigureCPayment()
    {
        CPaymentExtensions.Configure(cfg =>
        {
            cfg.UseNetwork(Network.Test);
            cfg.UseProvider(new BlockStreamBitcoinProvider());
            cfg.SetDefaultConfirmations(1);
            cfg.SetDerivationSalt("demo-derivation-salt");
            cfg.SetDerivationRequiredKeys("userId", "orderId");

            cfg.UseBitcoinWallet(wallet =>
            {
                wallet.UseMnemonic(ServerMnemonic);
                wallet.SetMasterAddress(ServerMasterAddress);
                wallet.ConfigureSweep(new AutoSweepOptions
                {
                    MinConfirmations = 1,
                    FeePolicy = FeePolicy.High,
                    EnableRbf = true,
                    MinSweepAmount = 0.000001m
                });
            });
        });
    }
}

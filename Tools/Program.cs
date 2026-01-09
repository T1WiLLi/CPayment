using CPayment.Payments;
using CPayment.Utils;

namespace Tools
{
    internal static class Program
    {
        public static void Main()
        {
            Console.WriteLine("CPayment – PaymentConverter test");
            Console.WriteLine("--------------------------------");

            try
            {
                var btcAmount = PaymentConverter.Convert(
                    amount: 14.99m,
                    paymentSupportedFiat: PaymentSupportedFiat.CAD,
                    type: PaymentType.BTC
                );

                Console.WriteLine($"12.32 USD => {btcAmount} BTC");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Conversion failed:");
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine();
            Console.WriteLine("Done. Press any key to exit...");
            Console.ReadKey();
        }
    }
}

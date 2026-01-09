using CPayment.Interfaces;
using CPayment.Payments.Converters;
using CPayment.Utils;

namespace CPayment.Payments;

public static class PaymentConverter
{
    private static readonly Dictionary<PaymentType, IPaymentConverter> _converters =
    new()
    {
        { PaymentType.BTC, new BitcoinPaymentConverter() }
    };

    public static async Task<decimal> ConvertAsync(
        decimal amount,
        PaymentSupportedFiat paymentSupportedFiat,
        PaymentType type)
    {
        if (!_converters.TryGetValue(type, out var converter))
        {
            throw new NotSupportedException($"{nameof(PaymentConverter)}.{nameof(ConvertAsync)} => No payment converter registered for '{type}'.");
        }

        return await converter.ConvertAsync(amount, paymentSupportedFiat);
    }

    public static decimal Convert(  
        decimal amount,
        PaymentSupportedFiat paymentSupportedFiat,
        PaymentType type)
    {
        return ConvertAsync(amount, paymentSupportedFiat, type).GetAwaiter().GetResult();
    }
}

using CPayment.Payments;
using CPayment.Utils;

namespace CPayment.Interfaces;

public interface IPaymentConverter
{
    PaymentType Type { get; }

    IReadOnlyCollection<PaymentSupportedFiat> SupportedFiatCurrencies { get; }

    Task<decimal> ConvertAsync(decimal amount, PaymentSupportedFiat fiatCurrency);
}

using CPayment.Public;

namespace CPayment.Payments;

public static class PaymentFactory
{
    public static Payment Create(decimal amount, PaymentType currency, PaymentMetadata metadata)
    {
        if (currency != PaymentType.BTC)
        {
            throw new NotSupportedException($"Payment type '{currency}' is not supported yet.");
        }

        return new Payment(amount, currency, metadata);
    }
}

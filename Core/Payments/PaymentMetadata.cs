namespace CPayment.Payments;

public sealed class PaymentMetadata : Dictionary<string, string>
{
    private PaymentMetadata() : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    public static PaymentMetadata Create(params (string Key, string Value)[] values)
    {
        var metadata = new PaymentMetadata();

        foreach (var (key, value) in values)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"{nameof(PaymentMetadata)}.{Create} => Metadata key cannot be null or whitespace.");
            }

            if (value is null)
            {
                throw new ArgumentException($"{nameof(PaymentMetadata)}.{Create} => Metadata value cannot be null.");
            }

            metadata[key] = value;
        }

        return metadata;
    }
}

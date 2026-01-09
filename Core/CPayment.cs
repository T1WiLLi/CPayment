using CPayment.Configuration;
using CPayment.Configuration.Builder;
using CPayment.Configuration.Validators;

namespace CPayment;

public static class CPayment
{
    private static CPaymentOptions? _options;

    public static void Configure(Action<CPaymentOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new CPaymentOptionsBuilder();
        configure(builder);

        var options = builder.Build();
        CPaymentOptionsValidator.Validate(options);

        _options = options;
    }

    public static CPaymentOptions Options => _options ?? throw new InvalidOperationException("CPayment is not configured. Please call CPayment.Configure() before using it.");
}

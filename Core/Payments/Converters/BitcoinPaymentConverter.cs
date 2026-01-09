using CPayment.Interfaces;
using CPayment.Services;
using CPayment.Utils;
using System.Text.Json;

namespace CPayment.Payments.Converters;

internal sealed class BitcoinPaymentConverter : IPaymentConverter
{
    private const string TickerUrl = "https://blockchain.info/ticker";

    public PaymentType Type => PaymentType.BTC;

    public IReadOnlyCollection<PaymentSupportedFiat> SupportedFiatCurrencies =>
    [
        PaymentSupportedFiat.USD,
        PaymentSupportedFiat.CAD,
        PaymentSupportedFiat.EUR,
    ];

    public async Task<decimal> ConvertAsync(decimal amount, PaymentSupportedFiat paymentSupportedFiat)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (!SupportedFiatCurrencies.Contains(paymentSupportedFiat))
        {
            throw new NotSupportedException(
                $"{nameof(BitcoinPaymentConverter)}.{nameof(ConvertAsync)} => Fiat currency '{paymentSupportedFiat}' is not supported for BTC conversion.");
        }

        var fiatCurrency = paymentSupportedFiat.ToString(); // USD => "USD"

        var last = await GetLastPriceAsync(fiatCurrency);

        return decimal.Round(
            amount / last,
            8,
            MidpointRounding.AwayFromZero
        );
    }

    private static async Task<decimal> GetLastPriceAsync(string fiatCurrency)
    {
        var client = HttpClientService.Instance;

        using var response = await client.GetAsync(TickerUrl);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);

        if (!json.RootElement.TryGetProperty(fiatCurrency, out var currencyObj))
        {
            throw new InvalidOperationException(
                $"{nameof(BitcoinPaymentConverter)}.{nameof(GetLastPriceAsync)} => Currency '{fiatCurrency}' not found in ticker response.");
        }

        if (!currencyObj.TryGetProperty("last", out var lastElement))
        {
            throw new InvalidOperationException(
                $"{nameof(BitcoinPaymentConverter)}.{nameof(GetLastPriceAsync)} => Field 'last' not found for currency '{fiatCurrency}'.");
        }

        if (lastElement.ValueKind != JsonValueKind.Number)
        {
            throw new InvalidOperationException(
                $"{nameof(BitcoinPaymentConverter)}.{nameof(GetLastPriceAsync)} => Field 'last' is not a number for currency '{fiatCurrency}'.");
        }

        if (lastElement.TryGetDecimal(out var lastDecimal))
        {
            if (lastDecimal <= 0)
            {
                throw new InvalidOperationException(
                    $"{nameof(BitcoinPaymentConverter)}.{nameof(GetLastPriceAsync)} => Invalid 'last' price '{lastDecimal}' for currency '{fiatCurrency}'.");
            }

            return lastDecimal;
        }

        var lastDouble = lastElement.GetDouble();
        if (double.IsNaN(lastDouble) || double.IsInfinity(lastDouble) || lastDouble <= 0)
        {
            throw new InvalidOperationException(
                $"{nameof(BitcoinPaymentConverter)}.{nameof(GetLastPriceAsync)} => Invalid 'last' price '{lastDouble}' for currency '{fiatCurrency}'.");
        }

        return (decimal)lastDouble;
    }
}
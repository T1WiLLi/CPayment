using System.Text.Json.Serialization;

namespace CPayment.Models;

public sealed class EsploraUtxo
{
    [JsonPropertyName("txid")]
    public string TxId { get; set; } = string.Empty;

    [JsonPropertyName("vout")]
    public int Vout { get; set; }

    [JsonPropertyName("status")]
    public EsploraTxStatus Status { get; set; } = new();

    [JsonPropertyName("value")]
    public long ValueSats { get; set; }
}

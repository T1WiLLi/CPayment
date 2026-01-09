using System.Text.Json.Serialization;

namespace CPayment.Models;

public sealed class EsploraTransaction
{
    [JsonPropertyName("txid")]
    public string TxId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public EsploraTxStatus Status { get; set; } = new();

    [JsonPropertyName("vout")]
    public List<EsploraVout> Vout { get; set; } = [];
}

public sealed class EsploraTxStatus
{
    [JsonPropertyName("confirmed")]
    public bool Confirmed { get; set; }

    [JsonPropertyName("block_height")]
    public int? BlockHeight { get; set; }

    [JsonPropertyName("block_hash")]
    public string? BlockHash { get; set; }

    [JsonPropertyName("block_time")]
    public long? BlockTime { get; set; }
}

public sealed class EsploraVout
{
    [JsonPropertyName("value")]
    public long ValueSats { get; set; }

    [JsonPropertyName("scriptpubkey_address")]
    public string? ScriptPubKeyAddress { get; set; }
}

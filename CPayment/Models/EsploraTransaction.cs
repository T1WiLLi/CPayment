using System.Text.Json.Serialization;

namespace CPayment.Models;

public sealed class EsploraTransaction
{
    [JsonPropertyName("txid")]
    public string TxId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public EsploraTxStatus Status { get; set; } = new();

    [JsonPropertyName("vin")]
    public List<EsploraVin> Vin { get; set; } = [];

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

public sealed class EsploraVin
{
    [JsonPropertyName("txid")]
    public string TxId { get; set; } = string.Empty;

    [JsonPropertyName("vout")]
    public int Vout { get; set; }

    [JsonPropertyName("prevout")]
    public EsploraPrevout Prevout { get; set; } = new();
}

public sealed class EsploraPrevout
{
    [JsonPropertyName("scriptpubkey_address")]
    public string? ScriptPubKeyAddress { get; set; }

    [JsonPropertyName("value")]
    public long ValueSats { get; set; }
}

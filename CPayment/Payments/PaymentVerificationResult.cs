namespace CPayment.Payments;

public sealed class PaymentVerificationResult
{
    private PaymentVerificationResult(
        PaymentStatus status,
        string? txId,
        int confirmations,
        decimal amount,
        string? error)
    {
        Status = status;
        TxId = txId;
        Confirmations = confirmations;
        Amount = amount;
        Error = error;
    }

    public PaymentStatus Status { get; }

    public string? TxId { get; }

    public int Confirmations { get; }

    public decimal Amount { get; }

    public string? Error { get; }

    public static PaymentVerificationResult NotFound { get; } =
        new(PaymentStatus.NotFound, null, 0, 0m, null);

    public static PaymentVerificationResult Unconfirmed(string txId, int confirmations, decimal amount) =>
        new(PaymentStatus.Unconfirmed, txId, confirmations, amount, null);

    public static PaymentVerificationResult Confirmed(string txId, int confirmations, decimal amount) =>
        new(PaymentStatus.Confirmed, txId, confirmations, amount, null);

    public static PaymentVerificationResult Unknown(string? error = null) =>
        new(PaymentStatus.Unknown, null, 0, 0m, error);
}

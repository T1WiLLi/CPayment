namespace CPayment.Payments;

public enum PaymentStatus
{
    NotFound, // No transaction found
    Unconfirmed, // Transaction found but not yet confirmed
    Confirmed, // Transaction confirmed
    Unknown // Status could not be determined
}

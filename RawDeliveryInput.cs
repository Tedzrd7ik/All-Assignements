namespace DeliveryQuotePipeline.Models;

public sealed class RawDeliveryInput
{
    public string? RequestId { get; init; }
    public string? CustomerName { get; init; }
    public string? DistanceKm { get; init; }
    public string? IsExpressRequested { get; init; }
}

namespace DeliveryQuotePipeline.Models;

public readonly record struct DeliveryRequest(
    string RequestId,
    string CustomerName,
    double DistanceKm,
    bool IsExpressRequested);

public readonly record struct DeliveryQuote(
    string RequestId,
    string CustomerName,
    double DistanceKm,
    decimal Cost,
    string PolicyName,
    bool Accepted);

public readonly record struct QuoteSummary(
    IReadOnlyList<DeliveryQuote> Quotes,
    int AcceptedCount,
    int RejectedCount,
    decimal TotalCost,
    string PolicyName);

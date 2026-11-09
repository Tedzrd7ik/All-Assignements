using DeliveryQuotePipeline.Models;

namespace DeliveryQuotePipeline.Pipeline;

public static class PricingPolicies
{
    public static decimal Standard(DeliveryRequest request)
        => 500m + 100m * (decimal)request.DistanceKm;

    public static decimal Express(DeliveryRequest request)
        => 800m + 150m * (decimal)request.DistanceKm;

    public static Func<DeliveryRequest, decimal> GetPolicy(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "standard" => Standard,
            "express"  => Express,
            _ => throw new ArgumentException(
                $"Unknown pricing policy '{name}'. Use 'standard' or 'express'.", nameof(name))
        };
    }
}

using DeliveryQuotePipeline.Models;

namespace DeliveryQuotePipeline.Pipeline;

public static class PipelineStages
{
    public static IReadOnlyList<DeliveryRequest> SelectEligible(
        IReadOnlyList<DeliveryRequest> requests,
        double maxDistanceKm)
    {
        var selected = new List<DeliveryRequest>();
        foreach (var r in requests)
        {
            if (r.DistanceKm > 0 && r.DistanceKm <= maxDistanceKm)
                selected.Add(r);
        }
        return selected.AsReadOnly();
    }

    public static IReadOnlyList<(DeliveryRequest Request, decimal Cost)> CalculateCosts(
        IReadOnlyList<DeliveryRequest> selected,
        Func<DeliveryRequest, decimal> pricePolicy)
    {
        ArgumentNullException.ThrowIfNull(pricePolicy);

        var results = new List<(DeliveryRequest, decimal)>(selected.Count);
        foreach (var r in selected)
        {
            var cost = pricePolicy(r);
            results.Add((r, cost));
        }
        return results.AsReadOnly();
    }

    public static QuoteSummary BuildSummary(
        IReadOnlyList<(DeliveryRequest Request, decimal Cost)> costed,
        string policyName,
        int originalValidatedCount)
    {
        var quotes = costed
            .Select(c => new DeliveryQuote(
                c.Request.RequestId,
                c.Request.CustomerName,
                c.Request.DistanceKm,
                c.Cost,
                policyName,
                Accepted: true))
            .OrderBy(q => q.Cost)
            .ThenBy(q => q.RequestId)
            .ToList()
            .AsReadOnly();

        var accepted = quotes.Count;
        var rejected = Math.Max(0, originalValidatedCount - accepted);
        var total = quotes.Sum(q => q.Cost);

        return new QuoteSummary(quotes, accepted, rejected, total, policyName);
    }

    public static QuoteSummary RunPipeline(
        IReadOnlyList<DeliveryRequest> validatedRequests,
        double maxDistanceKm,
        Func<DeliveryRequest, decimal> pricePolicy,
        string policyName)
    {
        ArgumentNullException.ThrowIfNull(validatedRequests);
        ArgumentNullException.ThrowIfNull(pricePolicy);

        var eligible = SelectEligible(validatedRequests, maxDistanceKm);
        var costed = CalculateCosts(eligible, pricePolicy);
        return BuildSummary(costed, policyName, validatedRequests.Count);
    }
}

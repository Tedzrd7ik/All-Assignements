using DeliveryQuotePipeline.Data;
using DeliveryQuotePipeline.Models;
using DeliveryQuotePipeline.Pipeline;

namespace DeliveryQuotePipeline;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Delivery Quote Pipeline (Assignment 5) ===\n");

        var raw = SampleData.GetDemoDataset();
        Console.WriteLine($"Loaded {raw.Count} raw records.\n");

        var (validated, errors) = Validation.ValidateAll(raw);

        Console.WriteLine("--- Validation ---");
        if (errors.Count > 0)
        {
            Console.WriteLine($"Rejected {errors.Count} invalid record(s):");
            foreach (var e in errors)
                Console.WriteLine($"  • {e}");
        }
        else
            Console.WriteLine("All records valid.");

        Console.WriteLine($"Validated domain records: {validated.Count}\n");

        RunAndPrint(validated, "standard");
        Console.WriteLine();
        RunAndPrint(validated, "express");

        Console.WriteLine("\n--- Empty dataset ---");
        var emptySummary = PipelineStages.RunPipeline(
            Array.Empty<DeliveryRequest>(),
            SampleData.MaxDistanceKm,
            PricingPolicies.Standard,
            "standard");
        Console.WriteLine($"Accepted: {emptySummary.AcceptedCount}, TotalCost: {emptySummary.TotalCost}");
    }

    private static void RunAndPrint(IReadOnlyList<DeliveryRequest> validated, string policyName)
    {
        var policy = PricingPolicies.GetPolicy(policyName);
        var summary = PipelineStages.RunPipeline(
            validated,
            SampleData.MaxDistanceKm,
            policy,
            policyName);

        Console.WriteLine($"=== Policy: {policyName.ToUpperInvariant()} ===");
        Console.WriteLine($"Accepted: {summary.AcceptedCount}  |  Rejected (by distance filter): {summary.RejectedCount}");
        Console.WriteLine($"Total cost: {summary.TotalCost:C}");
        Console.WriteLine("Quotes (ordered by cost):");
        foreach (var q in summary.Quotes)
            Console.WriteLine($"  {q.RequestId}  {q.CustomerName,-8}  {q.DistanceKm,5:F1} km  →  {q.Cost,8:C}");
    }
}

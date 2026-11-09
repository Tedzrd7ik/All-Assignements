using DeliveryQuotePipeline.Data;
using DeliveryQuotePipeline.Models;
using DeliveryQuotePipeline.Pipeline;
using Xunit;

namespace DeliveryQuotePipeline.Tests;

public class PipelineTests
{
    private static IReadOnlyList<DeliveryRequest> ValidDemoRequests()
    {
        var (valid, _) = Validation.ValidateAll(SampleData.GetDemoDataset());
        return valid;
    }

    [Fact]
    public void NormalWorkflow_StandardPolicy_ProducesDocumentedCosts()
    {
        var requests = ValidDemoRequests();
        var summary = PipelineStages.RunPipeline(
            requests,
            SampleData.MaxDistanceKm,
            PricingPolicies.Standard,
            "standard");

        Assert.Equal(SampleData.Expected.AcceptedCount, summary.AcceptedCount);
        Assert.Equal(4, summary.Quotes.Count);

        var costs = summary.Quotes.Select(q => q.Cost).ToList();
        Assert.Equal(SampleData.Expected.StandardCostsOrdered, costs);

        var twoKm = summary.Quotes.Single(q => q.RequestId == "R001");
        Assert.Equal(700m, twoKm.Cost);
    }

    [Fact]
    public void EmptyDataset_ProducesEmptySummary()
    {
        var summary = PipelineStages.RunPipeline(
            Array.Empty<DeliveryRequest>(),
            SampleData.MaxDistanceKm,
            PricingPolicies.Standard,
            "standard");

        Assert.Empty(summary.Quotes);
        Assert.Equal(0, summary.AcceptedCount);
        Assert.Equal(0, summary.RejectedCount);
        Assert.Equal(0m, summary.TotalCost);
        Assert.Equal("standard", summary.PolicyName);
    }

    [Fact]
    public void BoundaryValue_ExactlyMaxDistance_IsAccepted()
    {
        var boundary = new DeliveryRequest("BND", "Boundary", 50.0, false);
        var justOver = new DeliveryRequest("OVR", "Over", 50.1, false);

        var selected = PipelineStages.SelectEligible(
            new[] { boundary, justOver },
            SampleData.MaxDistanceKm);

        Assert.Single(selected);
        Assert.Equal("BND", selected[0].RequestId);
        Assert.Equal(50.0, selected[0].DistanceKm);
    }

    [Fact]
    public void InvalidAndMissingInput_AreRejected()
    {
        var nullResult = Validation.ParseAndValidate(null);
        Assert.False(nullResult.IsValid);
        Assert.Null(nullResult.Request);
        Assert.Contains("null", nullResult.ErrorMessage!, StringComparison.OrdinalIgnoreCase);

        var missingId = Validation.ParseAndValidate(new RawDeliveryInput
        {
            RequestId = "  ",
            CustomerName = "X",
            DistanceKm = "5"
        });
        Assert.False(missingId.IsValid);

        var negative = Validation.ParseAndValidate(new RawDeliveryInput
        {
            RequestId = "N1",
            CustomerName = "Neg",
            DistanceKm = "-1"
        });
        Assert.False(negative.IsValid);
        Assert.Contains("greater than zero", negative.ErrorMessage!);

        var badNum = Validation.ParseAndValidate(new RawDeliveryInput
        {
            RequestId = "B1",
            CustomerName = "Bad",
            DistanceKm = "xyz"
        });
        Assert.False(badNum.IsValid);

        var (_, errors) = Validation.ValidateAll(SampleData.GetDemoDataset());
        Assert.True(errors.Count >= 3);
    }

    [Fact]
    public void TwoPolicies_SameInput_ProduceDifferentDocumentedResults()
    {
        var requests = ValidDemoRequests();

        var standard = PipelineStages.RunPipeline(
            requests, SampleData.MaxDistanceKm,
            PricingPolicies.Standard, "standard");

        var express = PipelineStages.RunPipeline(
            requests, SampleData.MaxDistanceKm,
            PricingPolicies.Express, "express");

        Assert.Equal(SampleData.Expected.AcceptedCount, standard.AcceptedCount);
        Assert.Equal(SampleData.Expected.AcceptedCount, express.AcceptedCount);

        Assert.Equal(SampleData.Expected.StandardCostsOrdered,
            standard.Quotes.Select(q => q.Cost).ToList());
        Assert.Equal(SampleData.Expected.ExpressCostsOrdered,
            express.Quotes.Select(q => q.Cost).ToList());

        Assert.Equal(700m,  standard.Quotes.Single(q => q.RequestId == "R001").Cost);
        Assert.Equal(1100m, express.Quotes.Single(q => q.RequestId == "R001").Cost);

        Assert.NotEqual(standard.TotalCost, express.TotalCost);
    }

    [Fact]
    public void Purity_CallerOwnedCollectionsAreNotMutated()
    {
        var original = new List<DeliveryRequest>
        {
            new("P1", "Pure", 3.0, false),
            new("P2", "Pure2", 60.0, false)
        };
        var snapshotIds = original.Select(r => r.RequestId).ToList();
        var snapshotCount = original.Count;

        var selected = PipelineStages.SelectEligible(original, SampleData.MaxDistanceKm);

        Assert.Equal(snapshotCount, original.Count);
        Assert.Equal(snapshotIds, original.Select(r => r.RequestId).ToList());
        Assert.Single(selected);

        var costed = PipelineStages.CalculateCosts(selected, PricingPolicies.Standard);
        Assert.Equal(snapshotCount, original.Count);
        Assert.Single(costed);
        Assert.Equal(800m, costed[0].Cost);
    }

    [Fact]
    public void HigherOrderFunction_InvokesProvidedDelegate()
    {
        var request = new DeliveryRequest("H1", "HOF", 4.0, false);
        var called = false;
        Func<DeliveryRequest, decimal> spy = r =>
        {
            called = true;
            return 999m;
        };

        var costed = PipelineStages.CalculateCosts(new[] { request }, spy);

        Assert.True(called);
        Assert.Equal(999m, costed[0].Cost);
    }

    [Fact]
    public void LegitimateFalseAndMissingOptionalFlag_AreAccepted()
    {
        var missingFlag = Validation.ParseAndValidate(new RawDeliveryInput
        {
            RequestId = "F1",
            CustomerName = "Flag",
            DistanceKm = "7",
            IsExpressRequested = null
        });
        Assert.True(missingFlag.IsValid);
        Assert.False(missingFlag.Request!.Value.IsExpressRequested);

        var explicitFalse = Validation.ParseAndValidate(new RawDeliveryInput
        {
            RequestId = "F2",
            CustomerName = "Flag2",
            DistanceKm = "7",
            IsExpressRequested = "false"
        });
        Assert.True(explicitFalse.IsValid);
        Assert.False(explicitFalse.Request!.Value.IsExpressRequested);
    }
}

using DeliveryQuotePipeline.Models;

namespace DeliveryQuotePipeline.Data;

public static class SampleData
{
    public const double MaxDistanceKm = 50.0;

    public static IReadOnlyList<RawDeliveryInput> GetDemoDataset() => new List<RawDeliveryInput>
    {
        new() { RequestId = "R001", CustomerName = "Alice",   DistanceKm = "2",   IsExpressRequested = "false" },
        new() { RequestId = "R002", CustomerName = "Bob",     DistanceKm = "10",  IsExpressRequested = "true" },
        new() { RequestId = "R003", CustomerName = "Carol",   DistanceKm = "50",  IsExpressRequested = "false" },
        new() { RequestId = "R004", CustomerName = "Dave",    DistanceKm = "50.1",IsExpressRequested = "false" },
        new() { RequestId = "R005", CustomerName = "Eve",     DistanceKm = "-3",  IsExpressRequested = "false" },
        new() { RequestId = "R006", CustomerName = "Frank",   DistanceKm = null,  IsExpressRequested = "false" },
        new() { RequestId = "R007", CustomerName = "Grace",   DistanceKm = "abc", IsExpressRequested = "false" },
        new() { RequestId = "R008", CustomerName = "Heidi",   DistanceKm = "5",   IsExpressRequested = "true" },
    };

    public static class Expected
    {
        public static readonly IReadOnlyList<decimal> StandardCostsOrdered =
            new[] { 700m, 1000m, 1500m, 5500m };

        public static readonly IReadOnlyList<decimal> ExpressCostsOrdered =
            new[] { 1100m, 1550m, 2300m, 8300m };

        public const int AcceptedCount = 4;
        public const int RejectedByStage1 = 1;
    }
}

using DeliveryQuotePipeline.Models;

namespace DeliveryQuotePipeline.Pipeline;

public static class Validation
{
    public sealed record ValidationResult(
        bool IsValid,
        DeliveryRequest? Request,
        string? ErrorMessage);

    public static ValidationResult ParseAndValidate(RawDeliveryInput? raw)
    {
        if (raw is null)
            return new ValidationResult(false, null, "Input record is null.");

        if (string.IsNullOrWhiteSpace(raw.RequestId))
            return new ValidationResult(false, null, "RequestId is missing or empty.");

        if (string.IsNullOrWhiteSpace(raw.CustomerName))
            return new ValidationResult(false, null, $"RequestId '{raw.RequestId}': CustomerName is missing or empty.");

        if (string.IsNullOrWhiteSpace(raw.DistanceKm))
            return new ValidationResult(false, null, $"RequestId '{raw.RequestId}': DistanceKm is missing.");

        if (!double.TryParse(raw.DistanceKm, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var distance))
            return new ValidationResult(false, null,
                $"RequestId '{raw.RequestId}': DistanceKm '{raw.DistanceKm}' is not a valid number.");

        if (distance <= 0)
            return new ValidationResult(false, null,
                $"RequestId '{raw.RequestId}': DistanceKm must be greater than zero (got {distance}).");

        bool isExpress = false;
        if (!string.IsNullOrWhiteSpace(raw.IsExpressRequested))
        {
            if (!bool.TryParse(raw.IsExpressRequested, out isExpress))
                return new ValidationResult(false, null,
                    $"RequestId '{raw.RequestId}': IsExpressRequested '{raw.IsExpressRequested}' is not a valid boolean.");
        }

        var request = new DeliveryRequest(
            raw.RequestId.Trim(),
            raw.CustomerName.Trim(),
            distance,
            isExpress);

        return new ValidationResult(true, request, null);
    }

    public static (IReadOnlyList<DeliveryRequest> Valid, IReadOnlyList<string> Errors)
        ValidateAll(IEnumerable<RawDeliveryInput?>? rawInputs)
    {
        if (rawInputs is null)
            return (Array.Empty<DeliveryRequest>(), new[] { "Input collection is null." });

        var valid = new List<DeliveryRequest>();
        var errors = new List<string>();

        foreach (var raw in rawInputs)
        {
            var result = ParseAndValidate(raw);
            if (result.IsValid && result.Request.HasValue)
                valid.Add(result.Request.Value);
            else
                errors.Add(result.ErrorMessage ?? "Unknown validation error.");
        }

        return (valid.AsReadOnly(), errors.AsReadOnly());
    }
}

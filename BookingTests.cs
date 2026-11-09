using BookingSystem.App;
using Xunit;

namespace BookingSystem.Tests;

public class BookingTests
{
    private static BookingRequest ValidRequest() =>
        new("Alice Smith", new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 13), 2, "Deluxe");

    [Fact]
    public void Factory_Success_CreatesValidatedBookingWithExpectedData()
    {
        var result = ValidatedBooking.Create(ValidRequest());

        var valid = Assert.IsType<BookingValidationResult.Valid>(result);
        Assert.Equal("Alice Smith", valid.Booking.GuestName);
        Assert.Equal(new DateOnly(2026, 11, 10), valid.Booking.CheckIn);
        Assert.Equal(new DateOnly(2026, 11, 13), valid.Booking.CheckOut);
        Assert.Equal(2, valid.Booking.Guests);
        Assert.Equal("Deluxe", valid.Booking.RoomCategory);
        Assert.True(valid.Booking.Price > 0);
    }

    [Fact]
    public void Factory_Rejection_InvalidDateRange()
    {
        var request = new BookingRequest("Bob", new DateOnly(2026, 11, 15), new DateOnly(2026, 11, 12), 1, "Standard");
        var result = ValidatedBooking.Create(request);

        var inv = Assert.IsType<BookingValidationResult.Invalid>(result);
        Assert.Contains(inv.Reasons, r => r.Contains("Check-out must be after check-in"));
    }

    [Fact]
    public void Factory_Rejection_InvalidGuests()
    {
        var request = new BookingRequest("Carol", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3), 0, "Suite");
        var result = ValidatedBooking.Create(request);

        var inv = Assert.IsType<BookingValidationResult.Invalid>(result);
        Assert.Contains(inv.Reasons, r => r.Contains("At least one guest"));
    }

    [Fact]
    public void Factory_Rejection_InvalidCategory()
    {
        var request = new BookingRequest("Dave", new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 7), 2, "Penthouse");
        var result = ValidatedBooking.Create(request);

        var inv = Assert.IsType<BookingValidationResult.Invalid>(result);
        Assert.Contains(inv.Reasons, r => r.Contains("Unknown room category"));
    }

    [Fact]
    public void Factory_Rejection_EmptyGuestName()
    {
        var request = new BookingRequest("  ", new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 7), 1, "Standard");
        var result = ValidatedBooking.Create(request);

        var inv = Assert.IsType<BookingValidationResult.Invalid>(result);
        Assert.Contains(inv.Reasons, r => r.Contains("Guest name is required"));
    }

    [Fact]
    public void DistinctCases_ValidationAndStates_PatternMatching()
    {
        var validResult = ValidatedBooking.Create(ValidRequest());
        Assert.IsType<BookingValidationResult.Valid>(validResult);

        var invalidResult = ValidatedBooking.Create(new BookingRequest("", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), 1, "Standard"));
        Assert.IsType<BookingValidationResult.Invalid>(invalidResult);

        var booking = ((BookingValidationResult.Valid)validResult).Booking;
        var pending = BookingWorkflow.Start(booking);
        Assert.IsType<BookingState.Pending>(pending);

        var confirmResult = BookingWorkflow.Confirm(pending);
        var success = Assert.IsType<BookingTransitionResult.Success>(confirmResult);
        Assert.IsType<BookingState.Confirmed>(success.NewState);

        var cancelResult = BookingWorkflow.Cancel(success.NewState, "test");
        var cancelSuccess = Assert.IsType<BookingTransitionResult.Success>(cancelResult);
        Assert.IsType<BookingState.Cancelled>(cancelSuccess.NewState);
    }

    [Fact]
    public void NormalOperation_ValidatedValueProducesConfirmedState()
    {
        var result = ValidatedBooking.Create(ValidRequest());
        var valid = Assert.IsType<BookingValidationResult.Valid>(result);
        var pending = BookingWorkflow.Start(valid.Booking);
        var transition = BookingWorkflow.Confirm(pending);

        var success = Assert.IsType<BookingTransitionResult.Success>(transition);
        var confirmed = Assert.IsType<BookingState.Confirmed>(success.NewState);
        Assert.Equal(valid.Booking.GuestName, confirmed.Booking.GuestName);
        Assert.False(string.IsNullOrEmpty(confirmed.ConfirmationCode));
        Assert.True(confirmed.ConfirmedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void MissingPrerequisite_CannotConfirmCancelledBooking()
    {
        var result = ValidatedBooking.Create(ValidRequest());
        var valid = Assert.IsType<BookingValidationResult.Valid>(result);
        var pending = BookingWorkflow.Start(valid.Booking);
        var cancelled = Assert.IsType<BookingTransitionResult.Success>(BookingWorkflow.Cancel(pending, "early cancel"));
        var confirmAttempt = BookingWorkflow.Confirm(cancelled.NewState);

        var rejected = Assert.IsType<BookingTransitionResult.Rejected>(confirmAttempt);
        Assert.Contains("Cannot confirm a cancelled booking", rejected.Reason);
    }

    [Fact]
    public void RepeatedOperation_SecondConfirmReturnsAlreadyDone()
    {
        var result = ValidatedBooking.Create(ValidRequest());
        var valid = Assert.IsType<BookingValidationResult.Valid>(result);
        var pending = BookingWorkflow.Start(valid.Booking);
        var first = Assert.IsType<BookingTransitionResult.Success>(BookingWorkflow.Confirm(pending));
        var second = BookingWorkflow.Confirm(first.NewState);

        var already = Assert.IsType<BookingTransitionResult.AlreadyDone>(second);
        Assert.IsType<BookingState.Confirmed>(already.ExistingState);
    }

    [Fact]
    public void BoundaryValue_ExactlyFourGuestsAccepted()
    {
        var request = new BookingRequest("Eve", new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2), 4, "Standard");
        var result = ValidatedBooking.Create(request);

        var valid = Assert.IsType<BookingValidationResult.Valid>(result);
        Assert.Equal(4, valid.Booking.Guests);
    }

    [Fact]
    public void BoundaryValue_FiveGuestsRejected()
    {
        var request = new BookingRequest("Frank", new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2), 5, "Standard");
        var result = ValidatedBooking.Create(request);

        var inv = Assert.IsType<BookingValidationResult.Invalid>(result);
        Assert.Contains(inv.Reasons, r => r.Contains("Maximum 4 guests"));
    }

    [Fact]
    public void StatePreservation_FailedValidationDoesNotMutateAnything()
    {
        var originalRequest = ValidRequest();
        var result = ValidatedBooking.Create(new BookingRequest(originalRequest.GuestName, originalRequest.CheckOut, originalRequest.CheckIn, originalRequest.Guests, originalRequest.RoomCategory));

        Assert.IsType<BookingValidationResult.Invalid>(result);
        Assert.Equal("Alice Smith", originalRequest.GuestName);
        Assert.Equal(2, originalRequest.Guests);
    }

    [Fact]
    public void StatePreservation_FailedTransitionLeavesOriginalStateUnchanged()
    {
        var result = ValidatedBooking.Create(ValidRequest());
        var valid = Assert.IsType<BookingValidationResult.Valid>(result);
        var pending = BookingWorkflow.Start(valid.Booking);
        var cancelled = Assert.IsType<BookingTransitionResult.Success>(BookingWorkflow.Cancel(pending, "cancel"));
        var originalCancelled = cancelled.NewState;

        var confirmAttempt = BookingWorkflow.Confirm(originalCancelled);
        Assert.IsType<BookingTransitionResult.Rejected>(confirmAttempt);
        Assert.IsType<BookingState.Cancelled>(originalCancelled);
        var c = (BookingState.Cancelled)originalCancelled;
        Assert.Equal("cancel", c.Reason);
    }
}

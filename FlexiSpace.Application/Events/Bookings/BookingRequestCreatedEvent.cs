using MediatR;

namespace FlexiSpace.Application.Events.Bookings
{
    public sealed record BookingRequestCreatedEvent(
        long BookingRequestId,
        long ListingId,
        long SpaceId,
        string? LesseeId,
        string? LessorId,
        string? SpaceAddress) : INotification;
}

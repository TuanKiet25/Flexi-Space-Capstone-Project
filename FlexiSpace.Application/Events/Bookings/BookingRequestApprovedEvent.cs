using MediatR;

namespace FlexiSpace.Application.Events.Bookings
{
    public sealed record BookingRequestApprovedEvent(
        long BookingRequestId,
        long ListingId,
        long SpaceId,
        string? LessorId,
        string? LesseeId,
        string? SpaceAddress) : INotification;
}

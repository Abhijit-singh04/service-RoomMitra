using RoomMitra.Domain.Entities;

namespace RoomMitra.Application.Abstractions.Repositories;

public interface IOtpRequestRepository
{
    Task<OtpRequest?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken);
    Task<OtpRequest?> GetLatestByPhoneAsync(string phoneNumber, CancellationToken cancellationToken);
    Task AddAsync(OtpRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(OtpRequest request, CancellationToken cancellationToken);
}

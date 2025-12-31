using Microsoft.EntityFrameworkCore;
using RoomMitra.Application.Abstractions.Repositories;
using RoomMitra.Domain.Entities;
using RoomMitra.Infrastructure.Persistence;

namespace RoomMitra.Infrastructure.Repositories;

internal sealed class EfOtpRequestRepository : IOtpRequestRepository
{
    private readonly RoomMitraDbContext _dbContext;

    public EfOtpRequestRepository(RoomMitraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OtpRequest?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken)
    {
        return await _dbContext.OtpRequests
            .Where(o => o.RequestId == requestId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<OtpRequest?> GetLatestByPhoneAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        return await _dbContext.OtpRequests
            .Where(o => o.PhoneNumber == phoneNumber)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(OtpRequest request, CancellationToken cancellationToken)
    {
        await _dbContext.OtpRequests.AddAsync(request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(OtpRequest request, CancellationToken cancellationToken)
    {
        _dbContext.OtpRequests.Update(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

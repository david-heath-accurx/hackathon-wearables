using HealthApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace HealthApi.EntityFramework;

public class DeviceRegistrationStorage(HealthApiDbContext db)
{
    public async Task<bool> RegisterAsync(int patientId, string deviceId, CancellationToken ct)
    {
        var alreadyRegistered = await db.DeviceRegistrations
            .AnyAsync(r => r.DeviceId == deviceId, ct);

        if (alreadyRegistered)
            return false;

        db.DeviceRegistrations.Add(new DeviceRegistration
        {
            PatientId = patientId,
            DeviceId = deviceId,
        });

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeregisterAsync(string deviceId, CancellationToken ct)
    {
        var registration = await db.DeviceRegistrations
            .FirstOrDefaultAsync(r => r.DeviceId == deviceId, ct);

        if (registration is null)
            return false;

        db.DeviceRegistrations.Remove(registration);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

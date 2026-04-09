using HealthApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace HealthApi.EntityFramework;

public class DeviceRegistrationStorage(HealthApiDbContext db)
{
    public async Task<bool> RegisterAsync(string patientIdentifier, DateOnly dateOfBirth, string deviceId, CancellationToken ct)
    {
        if (await db.DeviceRegistrations.AnyAsync(r => r.DeviceId == deviceId, ct))
            return false;

        var patient = await db.Patients.FirstOrDefaultAsync(
            p => p.PatientIdentifier == patientIdentifier, ct);

        if (patient is null)
        {
            patient = new Patient { PatientIdentifier = patientIdentifier, DateOfBirth = dateOfBirth };
            db.Patients.Add(patient);
            await db.SaveChangesAsync(ct);
        }
        else if (patient.DateOfBirth != dateOfBirth)
        {
            return false;
        }

        db.DeviceRegistrations.Add(new DeviceRegistration
        {
            PatientId = patient.Id,
            DeviceId = deviceId,
        });

        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> IsRegisteredAsync(string patientIdentifier, DateOnly dateOfBirth, string deviceId, CancellationToken ct)
    {
        return db.DeviceRegistrations.AnyAsync(
            r => r.DeviceId == deviceId
              && r.Patient.PatientIdentifier == patientIdentifier
              && r.Patient.DateOfBirth == dateOfBirth,
            ct);
    }

    public Task<DeviceRegistration?> GetByDeviceIdAsync(string deviceId, CancellationToken ct)
    {
        return db.DeviceRegistrations
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.DeviceId == deviceId, ct);
    }

    // Returns null if the patient identifier+DOB combination is not found.
    // Returns a Patient with no active device registrations if consent was previously withdrawn.
    public Task<Patient?> FindPatientAsync(string patientIdentifier, DateOnly dateOfBirth, CancellationToken ct)
    {
        return db.Patients.FirstOrDefaultAsync(
            p => p.PatientIdentifier == patientIdentifier && p.DateOfBirth == dateOfBirth, ct);
    }

    public async Task<bool> DeregisterAsync(string deviceId, CancellationToken ct)
    {
        var registration = await db.DeviceRegistrations
            .FirstOrDefaultAsync(r => r.DeviceId == deviceId, ct);

        if (registration is null)
            return false;

        await db.HealthDataPoints
            .Where(p => p.DeviceId == deviceId)
            .ExecuteDeleteAsync(ct);

        db.DeviceRegistrations.Remove(registration);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeregisterAllAsync(string patientIdentifier, CancellationToken ct)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(
            p => p.PatientIdentifier == patientIdentifier, ct);

        if (patient is null)
            return false;

        var deviceIds = await db.DeviceRegistrations
            .Where(r => r.PatientId == patient.Id)
            .Select(r => r.DeviceId)
            .ToListAsync(ct);

        if (deviceIds.Count == 0)
            return false;

        await db.HealthDataPoints
            .Where(p => deviceIds.Contains(p.DeviceId!))
            .ExecuteDeleteAsync(ct);

        await db.DeviceRegistrations
            .Where(r => r.PatientId == patient.Id)
            .ExecuteDeleteAsync(ct);

        return true;
    }
}

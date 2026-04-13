using HealthApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace HealthApi.EntityFramework;

public class DeviceRegistrationStorage(HealthApiDbContext db)
{
    public async Task<bool> RegisterAsync(
        string patientIdentifier,
        string forename,
        string surname,
        DateOnly dateOfBirth,
        string postcode,
        string practiceOdsCode,
        string deviceId,
        CancellationToken ct)
    {
        if (await db.DeviceRegistrations.AnyAsync(r => r.DeviceId == deviceId, ct))
            return false;

        var patient = await db.Patients.FirstOrDefaultAsync(
            p => p.PatientIdentifier == patientIdentifier, ct);

        if (patient is null)
        {
            patient = new Patient
            {
                PatientIdentifier = patientIdentifier,
                Forename = forename,
                Surname = surname,
                DateOfBirth = dateOfBirth,
                Postcode = postcode,
                PracticeOdsCode = practiceOdsCode,
            };
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

    public Task UpdateDeviceModelAsync(Guid registrationId, string deviceModel, CancellationToken ct)
    {
        return db.DeviceRegistrations
            .Where(r => r.Id == registrationId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.DeviceModel, deviceModel), ct);
    }

    public async Task<bool> DeregisterAsync(string deviceId, CancellationToken ct)
    {
        // Health data points are deleted via CASCADE on the FK
        var deleted = await db.DeviceRegistrations
            .Where(r => r.DeviceId == deviceId)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    public async Task<bool> DeregisterAllAsync(string patientIdentifier, CancellationToken ct)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(
            p => p.PatientIdentifier == patientIdentifier, ct);

        if (patient is null)
            return false;

        // Health data points are deleted via CASCADE on the FK
        var deleted = await db.DeviceRegistrations
            .Where(r => r.PatientId == patient.Id)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    public Task<Patient?> FindPatientByIdentifierAsync(string patientIdentifier, CancellationToken ct)
    {
        return db.Patients.FirstOrDefaultAsync(
            p => p.PatientIdentifier == patientIdentifier, ct);
    }

    public Task<Patient?> FindPatientByDemographicsAsync(
        string forename, string surname, DateOnly dateOfBirth, string odsCode, CancellationToken ct)
    {
        return db.Patients.FirstOrDefaultAsync(
            p => p.Forename == forename
              && p.Surname == surname
              && p.DateOfBirth == dateOfBirth
              && p.PracticeOdsCode == odsCode,
            ct);
    }
}

using HealthApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace HealthApi.EntityFramework;

public class AlertStorage(HealthApiDbContext db)
{
    public async Task<List<HealthAlert>> GetAsync(string patientIdentifier, CancellationToken ct)
    {
        return await db.HealthAlerts
            .Where(a => a.Patient.PatientIdentifier == patientIdentifier)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync(ct);
    }

    public async Task<Patient> CreateAsync(string patientIdentifier, string severity, string message, CancellationToken ct)
    {
        var patient = await db.Patients
            .Where(p => p.PatientIdentifier == patientIdentifier)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Patient not found: {patientIdentifier}");

        db.HealthAlerts.Add(new HealthAlert
        {
            PatientId = patient.Id,
            Severity = severity,
            Message = message,
        });
        await db.SaveChangesAsync(ct);

        return patient;
    }
}

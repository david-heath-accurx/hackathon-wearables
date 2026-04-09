using HealthApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace HealthApi.EntityFramework;

public class AlertStorage(HealthApiDbContext db)
{
    public async Task CreateAsync(string patientIdentifier, string severity, string message, CancellationToken ct)
    {
        var patientId = await db.Patients
            .Where(p => p.PatientIdentifier == patientIdentifier)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Patient not found: {patientIdentifier}");

        db.HealthAlerts.Add(new HealthAlert
        {
            PatientId = patientId,
            Severity = severity,
            Message = message,
        });
        await db.SaveChangesAsync(ct);
    }
}

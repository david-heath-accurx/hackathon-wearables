using System.Net.Http.Json;
using HealthApi.Domain;
using Microsoft.Extensions.Logging;

namespace HealthApi.Functions;

/// <summary>
/// Sends a patient-initiated triage request to the practice's Accurx inbox when a health alert is raised.
/// </summary>
public class PatientInitiatedMessagingClient(
    IHttpClientFactory httpClientFactory,
    ILogger<PatientInitiatedMessagingClient> logger)
{
    // Hard-coded form identifiers for the health monitoring category on the demo environment
    private static readonly Guid CategoryId = Guid.Parse("12bd474a-4581-42a3-8b2a-c2237bdabf3b");
    private static readonly Guid SubcategoryId = Guid.Parse("7638b491-9c20-49d6-a5c2-9cccb158e639");
    private static readonly Guid QuestionId = Guid.Parse("f588d937-21f1-4b23-a95d-5b37af33f520");

    public async Task SendAlertAsync(Patient patient, string alertMessage, CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient("patientInitiated");

        var request = new
        {
            requestTrackingId = Guid.NewGuid(),
            patientInitiatedIdentifier = patient.PracticeOdsCode,
            patientSurname = patient.Surname,
            patientForename = patient.Forename,
            patientDateOfBirthDay = patient.DateOfBirth.Day,
            patientDateOfBirthMonth = patient.DateOfBirth.Month,
            patientDateOfBirthYear = patient.DateOfBirth.Year,
            patientPostcode = patient.Postcode,
            hasProxy = false,
            submission = new
            {
                categoryId = CategoryId,
                subcategoryId = SubcategoryId,
                attachmentIds = Array.Empty<string>(),
                questions = new[]
                {
                    new { id = QuestionId, answers = new[] { alertMessage } }
                },
                followUpQuestions = Array.Empty<object>(),
            }
        };

        try
        {
            var response = await http.PostAsJsonAsync("/api/PatientInitiatedMessaging", request, ct);

            if (response.IsSuccessStatusCode)
                logger.LogInformation(
                    "Patient initiated message sent to practice {OdsCode} for patient {PatientIdentifier}",
                    patient.PracticeOdsCode, patient.PatientIdentifier);
            else
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "Patient initiated message failed {Status} for practice {OdsCode}: {Body}",
                    (int)response.StatusCode, patient.PracticeOdsCode, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send patient initiated message for patient {PatientIdentifier}",
                patient.PatientIdentifier);
        }
    }
}

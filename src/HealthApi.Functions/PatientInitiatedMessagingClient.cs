using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthApi.Domain;
using Microsoft.Extensions.Logging;

namespace HealthApi.Functions;

public class PatientInitiatedMessagingClient(
    IHttpClientFactory httpClientFactory,
    ILogger<PatientInitiatedMessagingClient> logger)
{
    public async Task SendAlertAsync(Patient patient, string alertMessage, CancellationToken ct)
    {
        var formIds = await DiscoverFormIdsAsync(patient.PracticeOdsCode, ct);
        if (formIds is null)
        {
            logger.LogWarning(
                "Could not discover form IDs for practice {OdsCode} — skipping patient initiated message",
                patient.PracticeOdsCode);
            return;
        }

        var http = httpClientFactory.CreateClient("patientInitiated");

        var request = new
        {
            requestTrackingId = Guid.NewGuid(),
            patientInitiatedIdentifier = patient.PracticeOdsCode.ToLower(),
            patientSurname = patient.Surname,
            patientForename = patient.Forename,
            patientDateOfBirthDay = patient.DateOfBirth.Day,
            patientDateOfBirthMonth = patient.DateOfBirth.Month,
            patientDateOfBirthYear = patient.DateOfBirth.Year,
            patientPostcode = patient.Postcode,
            patientPhoneNumber = "",
            hasProxy = false,
            sendConfirmationMessage = false,
            contactByPhone = false,
            contactBySms = false,
            preferredContactNumber = "",
            patientContactDetails = new { emailAddress = "", phoneNumber = "" },
            contactPreferences = new { contactViaSms = false, contactViaPhone = false, contactViaEmail = false },
            proxyFlowInformation = (object?)null,
            clinicianName = "",
            clinicianAppId = "",
            receptionDataToken = "",
            isDirectLinkSubmission = false,
            nhsLoginType = (string?)null,
            isOutOfHours = false,
            patientMobileAuthResult = "",
            verification = (object?)null,
            submission = new
            {
                categoryId = formIds.CategoryId,
                subcategoryId = formIds.SubcategoryId,
                attachmentIds = Array.Empty<string>(),
                questions = new[]
                {
                    new { id = formIds.QuestionId, answers = new[] { alertMessage } }
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

    private async Task<FormIds?> DiscoverFormIdsAsync(string odsCode, CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient("patientInitiatedForms");
        try
        {
            var landingPageUrl = $"https://dev.accurx.nhs.uk/{odsCode.ToLower()}";

            using var req = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/patientinitiated/{odsCode}/forms?landingPageUrl={Uri.EscapeDataString(landingPageUrl)}");
            req.Headers.Add("X-Request-Tracking-Id", Guid.NewGuid().ToString());

            var response = await http.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Forms endpoint returned {Status} for {OdsCode}: {Body}",
                    (int)response.StatusCode, odsCode, body);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
            var sections = json?["sections"]?.AsArray();
            if (sections is null) return null;

            foreach (var section in sections)
            {
                var categories = section?["categories"]?.AsObject();
                if (categories is null) continue;

                foreach (var categoryProp in categories)
                {
                    var category = categoryProp.Value;
                    if (category?["type"]?.GetValue<string>() != "Questions") continue;
                    if (category["isEnabled"]?.GetValue<bool>() != true) continue;

                    var categoryId = Guid.Parse(category["id"]!.GetValue<string>());
                    var flows = category["flows"]?.AsArray();
                    if (flows is null || flows.Count == 0) continue;

                    foreach (var flow in flows)
                    {
                        var flowId = Guid.Parse(flow!["id"]!.GetValue<string>());
                        var questionId = FindFreeTextQuestionId(flow["pages"]?.AsArray());
                        if (questionId is null) continue;

                        logger.LogInformation(
                            "Discovered form IDs for {OdsCode}: category={CategoryId} subcategory={FlowId} question={QuestionId}",
                            odsCode, categoryId, flowId, questionId);

                        return new FormIds(categoryId, flowId, questionId.Value);
                    }
                }
            }

            logger.LogWarning("No suitable Questions category with a free-text question found for {OdsCode}", odsCode);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to discover form IDs for practice {OdsCode}", odsCode);
            return null;
        }
    }

    private static Guid? FindFreeTextQuestionId(JsonArray? pages)
    {
        if (pages is null) return null;

        foreach (var page in pages)
        {
            var blocks = page?["blocks"]?.AsArray();
            if (blocks is null) continue;

            foreach (var block in blocks)
            {
                if (block?["type"]?.GetValue<string>() != "Question") continue;

                var question = block["question"];
                if (question?["questionType"]?.GetValue<string>() == "FreeText")
                    return Guid.Parse(question["id"]!.GetValue<string>());
            }
        }

        return null;
    }

    private record FormIds(Guid CategoryId, Guid SubcategoryId, Guid QuestionId);
}

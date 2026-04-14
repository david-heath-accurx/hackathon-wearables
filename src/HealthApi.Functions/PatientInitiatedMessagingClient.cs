using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthApi.Domain;
using Microsoft.Extensions.Logging;

namespace HealthApi.Functions;

public class PatientInitiatedMessagingClient(
    IHttpClientFactory httpClientFactory,
    ILogger<PatientInitiatedMessagingClient> logger)
{
    public async Task SendAlertAsync(Patient patient, string severity, string alertMessage, CancellationToken ct)
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

        var prefix = $"Wearable device health alert\nSeverity: {severity}\n";
        var fullMessage = (prefix + alertMessage).Length <= 500
            ? prefix + alertMessage
            : (prefix + alertMessage)[..500];

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
                    new { id = formIds.QuestionId, answers = new[] { fullMessage } }
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

            // Collect every flow across all enabled Questions categories, keeping track of the category.
            // Filter to Admin requestType only — these are the patient-facing admin forms (e.g.
            // "I have an admin request") that include the catch-all "something else" free-text flow.
            var candidates = new List<(Guid CategoryId, int CategoryOrder, Guid FlowId, int FlowOrder, int QuestionCount, Guid QuestionId)>();

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
                    var categoryOrder = category["order"]?.GetValue<int>() ?? int.MaxValue;
                    var flows = category["flows"]?.AsArray();
                    if (flows is null) continue;

                    foreach (var flow in flows)
                    {
                        if (flow?["requestType"]?.GetValue<string>() != "Admin") continue;

                        var flowId = Guid.Parse(flow["id"]!.GetValue<string>());
                        var flowOrder = flow["order"]?.GetValue<int>() ?? int.MaxValue;
                        var pages = flow["pages"]?.AsArray();
                        if (pages is null) continue;

                        var questionBlocks = pages
                            .SelectMany(p => p?["blocks"]?.AsArray() ?? [])
                            .Where(b => b?["type"]?.GetValue<string>() == "Question")
                            .ToList();

                        var freeTextBlock = questionBlocks.FirstOrDefault(
                            b => b?["question"]?["questionType"]?.GetValue<string>() == "FreeText");
                        if (freeTextBlock is null) continue;

                        var questionId = Guid.Parse(freeTextBlock["question"]!["id"]!.GetValue<string>());
                        candidates.Add((categoryId, categoryOrder, flowId, flowOrder, questionBlocks.Count, questionId));
                    }
                }
            }

            if (candidates.Count == 0)
            {
                logger.LogWarning("No Admin flow with a FreeText question found for {OdsCode}", odsCode);
                return null;
            }

            // Prefer a flow with exactly one question (the catch-all "something else" form).
            // Fall back to the first Admin flow by category order, then flow order.
            var best = candidates
                .OrderBy(c => c.QuestionCount == 1 ? 0 : 1)
                .ThenBy(c => c.CategoryOrder)
                .ThenBy(c => c.FlowOrder)
                .First();

            logger.LogInformation(
                "Discovered form IDs for {OdsCode}: category={CategoryId} subcategory={FlowId} question={QuestionId}",
                odsCode, best.CategoryId, best.FlowId, best.QuestionId);

            return new FormIds(best.CategoryId, best.FlowId, best.QuestionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to discover form IDs for practice {OdsCode}", odsCode);
            return null;
        }
    }

    private record FormIds(Guid CategoryId, Guid SubcategoryId, Guid QuestionId);
}

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HealthApi.EntityFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HealthApi.Functions;

/// <summary>
/// Runs a Claude tool-use agent loop that queries recent health data and raises
/// alerts for any clinically concerning patterns it detects.
/// </summary>
public class HealthMonitoringAgent(
    IHttpClientFactory httpClientFactory,
    HealthDataStorage healthData,
    AlertStorage alerts,
    PatientInitiatedMessagingClient messagingClient,
    IConfiguration config,
    ILogger<HealthMonitoringAgent> logger)
{
    private const int MaxIterations = 20;

    private const string SystemPrompt = """
        You are an automated health monitoring agent. Wearable device data is submitted periodically
        and your role is to detect patterns that may warrant medical attention or follow-up.

        Use the available tools to:
        1. Identify patients who have recently submitted health data
        2. Review their metrics in detail and in context
        3. Raise alerts only for genuinely concerning patterns

        Reference ranges:
        - Heart rate (resting): 60–100 bpm | Concern: <40 or sustained >150 bpm
        - Heart rate (during exercise): up to 185 bpm is acceptable when Steps or ExerciseMinutes are elevated
        - Blood oxygen (SpO2): 95–100% | Concern: <94% | Critical: <90%
        - Respiratory rate: 12–20 breaths/min | Concern: <8 or >25
        - HRV: highly individual; a sudden drop of >30% from the patient's recent average may be notable
        - Sleep duration: 6–9 hours typical

        Be conservative. Only raise alerts for sustained anomalies across multiple readings, or
        clear clinical red flags. Do not alert for isolated minor deviations or a normal exercise response.
        When you have finished assessing all patients, stop — do not call any more tools.
        """;

    // Tool definitions sent to Claude on every API call
    private static readonly JsonArray ToolDefinitions = JsonNode.Parse("""
        [
          {
            "name": "list_patients_with_recent_data",
            "description": "Returns patient identifiers that have submitted health data within the last N minutes.",
            "input_schema": {
              "type": "object",
              "properties": {
                "minutes": {
                  "type": "integer",
                  "description": "How far back to look in minutes. Defaults to 30."
                }
              }
            }
          },
          {
            "name": "get_patient_metrics",
            "description": "Returns health metrics for a patient over a recent time window, grouped by metric type.",
            "input_schema": {
              "type": "object",
              "properties": {
                "patient_identifier": {
                  "type": "string",
                  "description": "The patient's unique identifier."
                },
                "hours": {
                  "type": "integer",
                  "description": "Hours of history to retrieve. Defaults to 2."
                }
              },
              "required": ["patient_identifier"]
            }
          },
          {
            "name": "raise_alert",
            "description": "Records a health alert to be reviewed by clinical staff. Use only for clinically significant findings.",
            "input_schema": {
              "type": "object",
              "properties": {
                "patient_identifier": {
                  "type": "string"
                },
                "severity": {
                  "type": "string",
                  "enum": ["low", "medium", "high", "critical"],
                  "description": "low = advisory, medium = review soon, high = review today, critical = immediate attention required."
                },
                "message": {
                  "type": "string",
                  "description": "Clinical description of the concern, including relevant metric values and timestamps."
                }
              },
              "required": ["patient_identifier", "severity", "message"]
            }
          }
        ]
        """)!.AsArray();

    public async Task RunAsync(CancellationToken ct)
    {
        var apiKey = config["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured.");
        var model = config["Anthropic:Model"] ?? "claude-opus-4-5";

        var http = httpClientFactory.CreateClient("anthropic");

        var messages = new List<JsonNode>
        {
            new JsonObject
            {
                ["role"] = "user",
                ["content"] = "Analyse health data from the past 30 minutes. Check all patients with recent submissions and determine whether any metrics indicate a health concern requiring an alert."
            }
        };

        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            var requestBody = new JsonObject
            {
                ["model"] = model,
                ["max_tokens"] = 4096,
                ["system"] = SystemPrompt,
                ["tools"] = ToolDefinitions.DeepClone(),
                ["messages"] = new JsonArray(messages.Select(m => m.DeepClone()).ToArray()),
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
            {
                Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            httpRequest.Headers.Add("x-api-key", apiKey);

            logger.LogDebug("Calling Anthropic API (iteration {Iteration})", iteration + 1);

            var httpResponse = await http.SendAsync(httpRequest, ct);
            if (!httpResponse.IsSuccessStatusCode)
            {
                var error = await httpResponse.Content.ReadAsStringAsync(ct);
                logger.LogError("Anthropic API error {Status}: {Error}", (int)httpResponse.StatusCode, error);
                return;
            }

            var response = await httpResponse.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct)
                ?? throw new InvalidOperationException("Empty response from Anthropic API.");

            var stopReason = response["stop_reason"]!.GetValue<string>();
            var content = response["content"]!.AsArray();

            foreach (var block in content)
            {
                if (block?["type"]?.GetValue<string>() == "text")
                    logger.LogInformation("Agent: {Text}", block["text"]!.GetValue<string>());
            }

            messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = content.DeepClone() });

            if (stopReason == "end_turn")
            {
                logger.LogInformation("Health monitor completed after {Iterations} iteration(s)", iteration + 1);
                return;
            }

            if (stopReason != "tool_use")
            {
                logger.LogWarning("Unexpected stop_reason '{StopReason}' — stopping agent loop", stopReason);
                return;
            }

            var toolResults = new JsonArray();
            foreach (var block in content)
            {
                if (block?["type"]?.GetValue<string>() != "tool_use") continue;

                var toolId = block["id"]!.GetValue<string>();
                var toolName = block["name"]!.GetValue<string>();
                var toolInput = block["input"]!.AsObject();

                logger.LogInformation("Tool call: {Tool} {Input}", toolName, toolInput.ToJsonString());

                var result = await ExecuteToolAsync(toolName, toolInput, ct);

                logger.LogInformation("Tool result: {Result}", result);

                toolResults.Add(new JsonObject
                {
                    ["type"] = "tool_result",
                    ["tool_use_id"] = toolId,
                    ["content"] = result,
                });
            }

            messages.Add(new JsonObject { ["role"] = "user", ["content"] = toolResults });
        }

        logger.LogWarning("Health monitor reached maximum iterations ({Max}) without completing", MaxIterations);
    }

    private Task<string> ExecuteToolAsync(string name, JsonObject input, CancellationToken ct) =>
        name switch
        {
            "list_patients_with_recent_data" => ListPatientsWithRecentDataAsync(input, ct),
            "get_patient_metrics" => GetPatientMetricsAsync(input, ct),
            "raise_alert" => RaiseAlertAsync(input, ct),
            _ => Task.FromResult($"Unknown tool: {name}"),
        };

    private async Task<string> ListPatientsWithRecentDataAsync(JsonObject input, CancellationToken ct)
    {
        var minutes = input["minutes"]?.GetValue<int>() ?? 30;
        var since = DateTimeOffset.UtcNow.AddMinutes(-minutes);
        var patients = await healthData.GetPatientsWithRecentDataAsync(since, ct);
        return JsonSerializer.Serialize(patients);
    }

    private async Task<string> GetPatientMetricsAsync(JsonObject input, CancellationToken ct)
    {
        var patientIdentifier = input["patient_identifier"]!.GetValue<string>();
        var hours = input["hours"]?.GetValue<int>() ?? 2;
        var since = DateTimeOffset.UtcNow.AddHours(-hours);

        var points = await healthData.GetAsync(patientIdentifier, null, since, null, ct);

        var grouped = points
            .GroupBy(p => p.MetricType.ToString())
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(p => p.RecordedAt).Select(p => new
                {
                    value = p.Value,
                    unit = p.Unit,
                    recorded_at = p.RecordedAt.ToString("O"),
                    device = p.DeviceRegistration?.DeviceModel,
                }).ToList()
            );

        return JsonSerializer.Serialize(new
        {
            patient_identifier = patientIdentifier,
            period_hours = hours,
            data_point_count = points.Count,
            metrics = grouped,
        });
    }

    private async Task<string> RaiseAlertAsync(JsonObject input, CancellationToken ct)
    {
        var patientIdentifier = input["patient_identifier"]!.GetValue<string>();
        var severity = input["severity"]!.GetValue<string>();
        var message = input["message"]!.GetValue<string>();

        var patient = await alerts.CreateAsync(patientIdentifier, severity, message, ct);

        logger.LogWarning(
            "HEALTH ALERT [{Severity}] patient={PatientIdentifier} — {Message}",
            severity.ToUpper(), patientIdentifier, message);

        await messagingClient.SendAlertAsync(patient, severity, message, ct);

        return "Alert recorded.";
    }
}

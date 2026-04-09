# Hackathon Wearables API

A public Azure-hosted REST API for storing and retrieving personal health data from wearable devices such as the Apple Watch. Built with ASP.NET Core (.NET 10), Entity Framework Core, and Azure Container Apps.

## Architecture

```
┌─────────────────────┐     JWT      ┌──────────────────────────┐
│   External App      │ ──────────── │  Azure Container Apps    │
│ (Apple Watch, etc.) │              │   hackathon-wearables-api │
└─────────────────────┘              └────────────┬─────────────┘
                                                  │
                              ┌───────────────────┼───────────────────┐
                              │                   │                   │
                    ┌─────────▼────────┐ ┌────────▼────────┐ ┌───────▼──────┐
                    │   Azure SQL DB   │ │   Azure ACR     │ │  Azure AD    │
                    │ hackathon-       │ │ hackathon-       │ │  (Auth)      │
                    │ wearables        │ │ wearables        │ │              │
                    └──────────────────┘ └─────────────────┘ └──────────────┘
```

### Project Structure

```
src/
├── HealthApi.Api/              # ASP.NET Core Web API — controllers, auth wiring, DI
├── HealthApi.Domain/           # Domain models (HealthDataPoint, DeviceRegistration)
└── HealthApi.EntityFramework/  # EF Core DbContext, migrations, storage classes
infra/
└── main.bicep                  # Azure infrastructure (Container App, ACR, environment)
Wearables-Health-API.postman_collection.json
```

## API

**Base URL:** `https://hackathon-wearables-api.whitemushroom-7e5ea013.uksouth.azurecontainerapps.io`

All endpoints except `/auth/token` require a `Bearer` token in the `Authorization` header.

### Authentication

#### `POST /auth/token`

Exchange client credentials for a JWT access token. Tokens expire after 1 hour.

```http
POST /auth/token
Content-Type: application/json

{
  "clientId": "<your-client-id>",
  "clientSecret": "<your-client-secret>"
}
```

```json
{
  "access_token": "eyJ...",
  "expires_in": 3599,
  "token_type": "Bearer"
}
```

---

### Device Registration

Patient consent is captured by registering their device. A device ID uniquely identifies the patient's mobile phone.

#### `POST /device-registrations`

Register a device for a patient (patient gives consent).

```http
POST /device-registrations
Authorization: Bearer <token>
Content-Type: application/json

{
  "patientId": 42,
  "deviceId": "iphone-uuid-abc123"
}
```

| Status | Meaning |
|--------|---------|
| `200 OK` | Device registered |
| `409 Conflict` | Device already registered |

#### `DELETE /device-registrations/{deviceId}`

Deregister a device (patient withdraws consent).

```http
DELETE /device-registrations/iphone-uuid-abc123
Authorization: Bearer <token>
```

| Status | Meaning |
|--------|---------|
| `200 OK` | Device deregistered |
| `404 Not Found` | No registration found for this device |

---

### Health Data

#### `POST /health-data`

Submit a batch of health metric readings from a wearable device.

```http
POST /health-data
Authorization: Bearer <token>
Content-Type: application/json

{
  "deviceId": "apple-watch-s9-001",
  "deviceModel": "Apple Watch Series 9",
  "dataPoints": [
    { "metricType": 0, "value": 72,   "unit": "bpm",   "recordedAt": "2026-04-08T09:00:00Z" },
    { "metricType": 1, "value": 8432, "unit": "steps", "recordedAt": "2026-04-08T09:00:00Z" },
    { "metricType": 3, "value": 98.5, "unit": "%",     "recordedAt": "2026-04-08T09:00:00Z" }
  ]
}
```

#### `GET /health-data`

Retrieve health data for the authenticated user, with optional filters.

```http
GET /health-data?metricType=0&from=2026-04-08T00:00:00Z&to=2026-04-08T23:59:59Z
Authorization: Bearer <token>
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `metricType` | integer | Filter by metric (see table below) |
| `from` | ISO 8601 | Start of time range (inclusive) |
| `to` | ISO 8601 | End of time range (inclusive) |

Results are returned newest-first.

```json
[
  {
    "id": "85d0a55a-e55f-4ca0-9e4c-73cd18945dfa",
    "metricType": 0,
    "value": 72,
    "unit": "bpm",
    "recordedAt": "2026-04-08T09:00:00+00:00",
    "deviceId": "apple-watch-s9-001",
    "deviceModel": "Apple Watch Series 9"
  }
]
```

---

### Metric Types

| Value | Name | Typical Unit |
|-------|------|-------------|
| `0` | HeartRate | bpm |
| `1` | Steps | steps |
| `2` | ActiveCalories | kcal |
| `3` | RestingCalories | kcal |
| `4` | BloodOxygen | % |
| `5` | SleepDuration | hours |
| `6` | StandHours | hours |
| `7` | ExerciseMinutes | minutes |
| `8` | WorkoutDuration | minutes |
| `9` | RespiratoryRate | breaths/min |
| `10` | HeartRateVariability | ms |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [dotnet-ef](https://docs.microsoft.com/en-us/ef/core/cli/dotnet) (`dotnet tool install --global dotnet-ef`)

### Running Locally

1. Update `src/HealthApi.Api/appsettings.json` with a local SQL connection string:

```json
{
  "ConnectionStrings": {
    "HealthApiDb": "Server=(localdb)\\mssqllocaldb;Database=HealthApi;Trusted_Connection=True;"
  }
}
```

2. Apply migrations:

```bash
dotnet ef database update --project src/HealthApi.EntityFramework --startup-project src/HealthApi.Api
```

3. Run:

```bash
dotnet run --project src/HealthApi.Api
```

Swagger UI is available at `https://localhost:{port}/swagger` in development.

### Deploying to Azure

Infrastructure is defined in `infra/main.bicep`. The deployment targets the existing `Hackathon-David-Heath` resource group in the **Sandbox** Azure subscription.

```bash
az account set --subscription Sandbox

# Deploy infrastructure
az deployment group create \
  --resource-group Hackathon-David-Heath \
  --template-file infra/main.bicep \
  --parameters acrAdminPassword="<acr-admin-password>"

# Build and push image
az acr build \
  --registry hackathonwearables \
  --image health-api:latest \
  --file src/HealthApi.Api/Dockerfile \
  .

# Apply migrations (requires firewall access to Azure SQL)
dotnet ef database update \
  --project src/HealthApi.EntityFramework \
  --startup-project src/HealthApi.Api \
  --connection "<azure-sql-connection-string>"
```

---

## Testing

Import `Wearables-Health-API.postman_collection.json` into Postman. The collection covers the full flow:

1. Get Token — auto-saves JWT to a collection variable
2. Submit Heart Rate
3. Submit Mixed Metrics
4. Get All Data
5. Get Heart Rate Only
6. Get Data in Time Range
7. Register Device (Patient Consent)
8. Register Same Device Again (expect 409)
9. Deregister Device (Consent Withdrawn)
10. Deregister Unknown Device (expect 404)
11. Reject Unauthenticated Request (expect 401)

Set the `clientSecret` collection variable to your actual client secret before running.

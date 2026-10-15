# Asset Availability API

A serverless HTTP API built with **.NET 8 isolated Azure Functions** that reports whether a mining or construction asset (excavator, haul truck, etc.) is available on a given date, based on a mock maintenance schedule.

It demonstrates a complete, production-style workflow: a RESTful function, automated build and deployment via **GitHub Actions**, and secure keyless authentication to Azure using **OpenID Connect (OIDC)**.

## Live Demo

Base URL:

```
https://asset-availability-api-hvhhakbae5d7gjc6.austriaeast-01.azurewebsites.net/api
```

Check if asset `EX-01` is available on `2026-10-20`:

```
GET https://asset-availability-api-hvhhakbae5d7gjc6.austriaeast-01.azurewebsites.net/api/assets/EX-01/availability?date=2026-10-20
```

```json
{
  "assetId": "EX-01",
  "available": true,
  "reason": "No scheduled maintenance",
  "nextMaintenance": "2026-11-01"
}
```

## Endpoint

| Method | Route | Query | Description |
| ------ | ----- | ----- | ----------- |
| `GET` | `/api/assets/{id}/availability` | `date` (required, `YYYY-MM-DD`) | Returns the availability of the asset on the requested date |

### Known assets

| ID | Type | Next maintenance |
| -- | ---- | ---------------- |
| `EX-01` | Excavator | 2026-11-01 |
| `EX-02` | Excavator | 2026-10-25 |
| `HT-05` | Haul Truck | 2026-12-15 |
| `HT-06` | Haul Truck | 2026-10-18 |

### Status codes

| Code | Meaning |
| ---- | ------- |
| `200` | Availability returned |
| `400` | Missing or invalid `date` query parameter |
| `404` | Asset ID not found |

## Business Logic

An asset is considered **unavailable** if the requested date falls within the **7 days before** its scheduled maintenance date. Otherwise it is available.

This mirrors real-world fleet scheduling, where equipment is often pulled from service ahead of planned maintenance.

## Tech Stack

- **.NET 8** — target framework
- **Azure Functions (isolated worker model)** — serverless compute
- **Azure Functions Flex Consumption plan** — hosting
- **GitHub Actions** — CI/CD
- **OpenID Connect (OIDC) + RBAC** — keyless deployment authentication
- **Azure CLI (`az`)** — provisioning and management

## Project Structure

```
AssetAvailabilityDemo/
├── .github/workflows/deploy-function.yml   # CI/CD pipeline
├── AssetAvailabilityFunction/
│   ├── AssetAvailabilityFunction.slnx
│   └── AssetAvailabilityFunction/
│       ├── Function1.cs                     # HTTP trigger + business logic
│       ├── Program.cs                       # Isolated worker host setup
│       ├── host.json
│       └── AssetAvailabilityFunction.csproj
└── README.md
```

## Running Locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (local storage emulator)

### Steps

```bash
dotnet restore AssetAvailabilityFunction/AssetAvailabilityFunction/AssetAvailabilityFunction.csproj
func start --project AssetAvailabilityFunction/AssetAvailabilityFunction
```

Then call the local endpoint:

```
http://localhost:7071/api/assets/EX-01/availability?date=2026-10-20
```

## Deployment (CI/CD)

Deployment is fully automated. Any push to `main` (or a manual `workflow_dispatch`) triggers the workflow in `.github/workflows/deploy-function.yml`, which:

1. Checks out the code
2. Sets up the .NET SDK
3. Restores, builds, and publishes the function project in `Release`
4. Logs in to Azure using **OIDC** (federated identity — no secrets stored in GitHub)
5. Deploys the package to the Function App

### How authentication works

The pipeline uses a **user-assigned managed identity** with a **federated credential** trusting the GitHub repository's `main` branch. GitHub exchanges a short-lived OIDC token for an Azure access token, so no long-lived credentials or publish profiles are stored anywhere.

- **Identity**: user-assigned managed identity
- **Role**: `Contributor` scoped to the Function App
- **Federated credential subject**: `repo:<owner>@<id>/<repo>@<id>:ref:refs/heads/main`

### Notes on Flex Consumption

This app runs on the **Flex Consumption** plan, which does **not** expose a Kudu/SCM endpoint. As a result:

- Publish-profile (basic auth) deployments are not supported and return `401`.
- Deployment must use RBAC/OIDC via `azure/login` + `Azure/functions-action`.
- The Function App runtime (`dotnet-isolated 8.0`) must match the project's target framework (`net8.0`), or the host will fail to start (`502`).

## License

This project is provided as a demonstration and is free to use and adapt.

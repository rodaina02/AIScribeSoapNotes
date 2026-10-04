# Clinical Documentation

A local clinical documentation application. A clinician selects a patient, types or dictates the visit conversation, and the API turns that text into a SOAP note and draft ICD-10-CM and CPT suggestions. The note, transcript, and codes are stored in PostgreSQL. The browser can download the generated note as a PDF.

This is a demonstration project. It does not authenticate users, and it is meant to run on a local machine.

## What the application does

The screen is a single documentation page:

1. Load active patients from the API.
2. Choose the patient, encounter title, date, and time.
3. Enter the conversation by typing, or dictate it with the browser microphone.
4. Send the conversation to the API, which creates a visit and runs the documentation pipeline.
5. Show the SOAP note, primary ICD-10-CM code, extra coding notes, CPT lines, and the cleaned transcript.
6. Download that result as a PDF, and review earlier visits for the same patient.

The date and time default to the browser clock and can be changed before generation. The title starts as the first few words of the conversation and can be edited. After a successful generation, the title field shows the chief complaint returned by the model unless the title was edited by hand.

## Key features

- Patient picker and per-patient visit history
- Typed conversation or browser speech recognition (`en-US`, Chrome or Edge)
- Whitespace cleanup of the transcript before the model call
- SOAP generation through the OpenAI chat API, returned as JSON
- Draft ICD-10-CM and CPT suggestions through a second chat call
- Persistence of the encounter, SOAP note, transcript, and code links
- Client-side PDF download of the generated note
- FluentValidation on the create-visit request
- Health check at `GET /health`

## How the workflow works

`POST /api/patients/{patientId}/visits` is the path the screen uses. The request body is:

```json
{
  "title": "Office visit",
  "occurredAt": "2026-10-04T11:30:00.000Z",
  "rawConversation": "Patient reports ..."
}
```

The `X-Tenant-Id` header is required. The UI sends the tenant id that came back with the selected patient.

The API then:

1. Loads the patient in that tenant.
2. Creates an `Encounter` at the patient’s facility. The visit type is Follow-up, the status is In Progress, and the attending provider id comes from `Documentation:DefaultAttendingProviderUserId`.
3. Collapses extra spaces and blank lines in the transcript.
4. Asks the model for a SOAP note (`subjective`, `objective`, `assessment`, `plan`, `chiefComplaint`).
5. Asks the model for a primary ICD-10-CM code, additional diagnosis lines, and CPT line items.
6. In one database transaction, inserts or updates the SOAP note, replaces prior AI code links for that encounter, and stores the transcript with transcription service `Manual`.

`POST /api/encounters/{encounterId}/ai-pipeline` runs steps 3–6 for an encounter that already exists. The current screen does not call it. It creates a new visit instead.

## Architecture overview

The solution is split into four .NET projects, a React client, and SQL scripts.

| Project | Responsibility |
| --- | --- |
| `Ehr.Domain` | Entities and enums. No dependencies on the other projects. |
| `Ehr.Application` | Use cases, DTOs, FluentValidation, and the documentation pipeline. |
| `Ehr.Infrastructure` | EF Core, repositories, and the OpenAI client. |
| `Ehr.Api` | HTTP controllers, CORS, and startup. |
| `web/ehr-ui` | React UI. |
| `db/postgres` | PostgreSQL schema and fictional development seed data. |

Controllers stay thin: they check the tenant header, call an application service, and map a few exceptions to HTTP status codes. Services depend on repository interfaces. EF Core is only referenced from Infrastructure.

There is no `Migrations` folder. The database shape is the SQL in `db/postgres/ehr_db.sql`. `EhrDbContext` maps that schema with snake_case column names. `EhrDbContextFactory` exists so `dotnet ef` can construct the context, but this repository does not ship EF migrations.

## Technology stack

- C# and ASP.NET Core, `net9.0`
- Entity Framework Core 9 and Npgsql
- PostgreSQL
- FluentValidation
- React 19 and TypeScript
- Vite 8
- jsPDF for the note download
- OpenAI .NET SDK 2.1.0

## AI integration

`OpenAiService` is the registered `IAIService`. It uses `ChatClient` from the official OpenAI SDK. The model name is `OpenAI:Model` (`gpt-4o` in the checked-in settings). Both calls request a JSON object and parse it in the service.

The SOAP prompt asks for narrative sections and tells the model not to invent vitals or test results that are absent from the text. The coding prompt asks for draft ICD-10-CM and CPT/HCPCS codes for a person to review. If the primary code is missing, the service stores `R69`.

The pipeline does not send procedure or prescription lists. Those prompt fields are fixed sentences saying they were not supplied. The facility setting sent to the model defaults to `Skilled Nursing Facility (SNF)` because the form does not collect a setting.

`NoOpAiService` is a deterministic stub in the same folder. It is not registered in dependency injection.

Speech-to-text is not an API call. The microphone button uses the browser `SpeechRecognition` API. `TranscriptionServiceKind` includes Deepgram, Whisper, Azure, and Google values for the database column, and the pipeline writes `Manual`.

## Project structure

```text
Ehr.slnx
src/Ehr.Api/                 ASP.NET Core host and controllers
src/Ehr.Application/         Services, DTOs, validation
src/Ehr.Domain/              Entities and enums
src/Ehr.Infrastructure/      EF Core, repositories, OpenAI
db/postgres/                 Schema, reset script, and dev seed
web/ehr-ui/                  React client
```

Main HTTP routes:

| Method | Route | Used by the UI |
| --- | --- | --- |
| `GET` | `/health` | No |
| `GET` | `/api/patients` | Yes |
| `GET` | `/api/patients/{id}` | No |
| `GET` | `/api/patients/{id}/visits` | Yes |
| `POST` | `/api/patients/{id}/visits` | Yes |
| `GET` | `/api/encounters/{id}` | No |
| `POST` | `/api/encounters/{id}/ai-pipeline` | No |
| `GET` | `/api/soap/{id}` | No |

Routes other than `GET /api/patients` and `GET /health` require a non-empty `X-Tenant-Id` header.

## Prerequisites

- .NET 9 SDK, or a newer SDK that can build `net9.0`
- Node.js and npm
- PostgreSQL running locally
- An OpenAI API key, only if you want to generate notes

## Configuration

`src/Ehr.Api/appsettings.json` is safe to commit. The API key is empty, and the database password is the placeholder `YOUR_PASSWORD`. `src/Ehr.Api/appsettings.Example.json` has the same shape.

Local secrets belong in the .NET user-secrets store. The user-secrets id is already set on `Ehr.Api.csproj`. From the repository root:

```bash
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_OPENAI_API_KEY" --project src/Ehr.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ehr_db;Username=postgres;Password=YOUR_PASSWORD" --project src/Ehr.Api
```

Environment variables work as well:

```text
OpenAI__ApiKey=YOUR_OPENAI_API_KEY
OpenAI__Model=gpt-4o
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=ehr_db;Username=postgres;Password=YOUR_PASSWORD
```

`Program.cs` adds user secrets again after the host configuration is built. When a value exists in both places, the user-secrets value is the one the API uses.

`Documentation:DefaultAttendingProviderUserId` is `a0000000-0000-0000-0000-0000000000ff`, which matches the clinician id in the development seed.

The Vite dev server proxies `/api` to `http://localhost:5010`. CORS on the API allows `http://localhost:5173` and `http://127.0.0.1:5173`.

## How to run locally

Create the database and load the schema plus fictional seed data:

```bash
psql -U postgres -c "CREATE DATABASE ehr_db;"
psql -U postgres -d ehr_db -f db/postgres/ehr_db_reset_and_seed.sql
```

The reset script drops and recreates the `public` schema, and it refuses to run unless the current database is named `ehr_db`. More detail is in `db/postgres/CONNECTION.txt`. `seed_dev_sample.sql` is a deprecated no-op. `seed_patients_and_history.sql` is an alternate seed for an existing schema.

Start the API with the HTTP profile so it listens on port 5010, which is what the UI proxy expects:

```bash
dotnet run --project src/Ehr.Api --launch-profile http
```

In a second terminal:

```bash
cd web/ehr-ui
npm install
npm run dev
```

Open `http://localhost:5173`.

`src/Ehr.Api/Ehr.Api.http` has sample calls for health, the patient list, and visit history. The seeded tenant id is `a0000000-0000-0000-0000-000000000001`.

## How to configure the OpenAI API key safely

Put the key in user secrets or in `OpenAI__ApiKey`, as shown above. Leave `OpenAI:ApiKey` empty in `appsettings.json`.

Do not paste a real key into source files, the README, screenshots, or sample settings. The placeholder used in this repository is `YOUR_OPENAI_API_KEY`.

Without a key, the API still starts and can list patients. The first generate request constructs `OpenAiService` and returns an error explaining that `OpenAI:ApiKey` is missing.

## How to run tests

There is no test project. `dotnet test Ehr.slnx` restores the solution and does not run any tests.

Frontend checks:

```bash
cd web/ehr-ui
npm run lint
npm run build
```

## Example usage

1. Apply `db/postgres/ehr_db_reset_and_seed.sql`.
2. Set the database connection and OpenAI key in user secrets.
3. Start the API and the Vite dev server.
4. Choose a seeded patient, such as Demo Patient.
5. Type a short visit conversation, or use the microphone in Chrome or Edge.
6. Click **Generate SOAP & codes**.
7. Read the four SOAP sections and the code suggestions, then download the PDF if you want a local copy.
8. Open **Visit history** on the same patient to read the stored note and transcript.

The seed data uses invented names, a fictional facility address, and sample clinical notes. It is not a real chart.

## Screenshots

Screenshots are not included yet.

## Security notes

- The API has no authentication or authorization. Anyone who can reach it can read patients and create visits.
- `GET /api/patients` returns every active patient and includes each row’s tenant id. Other routes trust the `X-Tenant-Id` header supplied by the caller.
- CORS is limited to the local Vite origins. `AllowedHosts` is `*`, which is the ASP.NET Core default in this project.
- Database access goes through EF Core. The visit request is validated for a required timestamp, a title of at most 200 characters, and a transcript of at most 200,000 characters.
- Generated codes are model suggestions stored for review. They are not a coded claim.
- Keep real patient information out of this database. The scripts under `db/postgres` are sample data.
- Run this on localhost. Do not expose the API to a public network in its current form.
- A failed PostgreSQL login for SQL state `28P01` is returned as JSON with setup instructions. The message does not include the configured password.

## Known limitations

- Saved SOAP text is displayed read-only. The screen does not edit a note after it is stored.
- Running the pipeline again for the same encounter updates that SOAP note and adds another transcript row. The UI avoids that by creating a new encounter for each generation.
- New visits are always stored as Follow-up. Vitals on the encounter entity are unused.
- Audio columns exist on `encounter_transcripts` and are left empty. There is no audio upload.
- The patient list is ordered by last name, then first name.
- Age is calculated from date of birth when the SOAP request is built. It is not a database column.
- Startup logs an HTTPS redirect warning when the HTTP launch profile is used, because that profile does not define an HTTPS port. The API still serves `http://localhost:5010`.
- EF Core logs sentinel-value warnings for `Encounter.Type` and `EncounterMedicalCode.Source` on first use. The patient list and visit history requests still completed during review.
- There is no CI configuration, Docker setup, or automated test suite.

## Future improvements

These are not implemented:

- Authentication, and a tenant filter on the patient list
- Server-side transcription and stored audio
- Editing a saved note, and choosing visit type or care setting in the form
- Capturing procedures and prescriptions instead of the fixed “not supplied” prompt text
- Automated tests
- A license file, if you want to grant reuse rights

## License

This repository does not include a license file. All rights are reserved until one is added.

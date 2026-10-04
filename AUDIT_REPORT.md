# Publication audit

Review date: 2026-10-04. No git repository was present, so there is no commit history to rewrite.

## What the project does

Clinical Documentation is a local demonstration. The React page loads patients, accepts a typed or browser-dictated conversation, and posts it to an ASP.NET Core API. The API creates a visit, cleans the text, calls OpenAI for a SOAP note and draft ICD-10-CM/CPT suggestions, and stores the encounter, note, transcript, and code links in PostgreSQL. The page can download the result as a PDF and can show prior visits.

## Technologies found

- C#, ASP.NET Core, `net9.0`
- EF Core 9, Npgsql, PostgreSQL
- FluentValidation
- React 19, TypeScript, Vite 8, jsPDF
- OpenAI .NET SDK 2.1.0, chat model configured as `gpt-4o`
- Browser Web Speech API for the microphone

Not present: authentication, EF migrations, automated tests, Docker, CI, audio upload, and server-side speech-to-text. Enum values for Deepgram, Whisper, Azure, and Google exist on the transcript column and are not called. `NoOpAiService` is not registered.

## Build and test results

- `dotnet build Ehr.slnx`: succeeded, 0 warnings, 0 errors.
- `dotnet test Ehr.slnx`: exit code 0. There is no test project, so no tests ran.
- `npm run lint` in `web/ehr-ui`: passed after the page stopped updating state synchronously inside effects.
- `npm run build` in `web/ehr-ui`: passed. Vite reported a chunk larger than 500 kB because the PDF libraries are bundled.
- API `dotnet run --launch-profile http`: listening on `http://localhost:5010`.
- `GET /health`: 200 `{"status":"ok"}`.
- `GET /api/patients`: 200, 5 rows from the local database.
- `GET /api/patients/{id}/visits` with the seeded tenant header: 200, 1 visit for the first seeded patient.
- SOAP generation was not executed. That call needs the live OpenAI API.
- The React screen was not clicked through in a browser. Lint and the production bundle were the UI checks.

Startup also logged an HTTPS redirect warning for the HTTP profile, and EF Core sentinel warnings for `Encounter.Type` and `EncounterMedicalCode.Source`. The checked endpoints still succeeded.

## Security issues found

- A live OpenAI API key was in `src/Ehr.Api/appsettings.json` and in the build output copy of that file.
- A local PostgreSQL password was in the same connection string. It was not the placeholder the database notes described.
- Source comments named an external Python model and a third-party EHR vendor.
- `Ehr.Api.http` still called a template `/weatherforecast/` route that does not exist.
- `web/ehr-ui/src/api/pipelineClient.ts` was unused.
- The API has no authentication. `GET /api/patients` is not filtered by tenant. Callers choose `X-Tenant-Id` themselves.
- There was no root `.gitignore`, so `bin/`, `obj/`, `node_modules/`, and `dist/` could have been committed.

No certificate keys, SMTP secrets, cloud credentials, or real patient records were found. The SQL seed uses fictional patients.

## Security issues fixed

- The API key and connection string were removed from `appsettings.json` and from the existing `bin` copy.
- The same values were written to the local .NET user-secrets store for this project, outside the repository. The values are not repeated in this report.
- `appsettings.json` and `appsettings.Example.json` now use an empty API key and `Password=YOUR_PASSWORD`.
- A root `.gitignore` ignores build output, `.env` files, local appsettings overrides, and `.cursor/`.
- Comments that pointed at a private source model or a vendor product were rewritten so they describe this code only.
- The HTTP sample file matches routes that exist.
- The unused pipeline client was removed. `POST /api/encounters/{id}/ai-pipeline` remains on the API.
- A scan of `src`, `db`, and `web/ehr-ui/src` found no `sk-` key material afterward. The built `appsettings.json` has an empty API key and the `YOUR_PASSWORD` placeholder.

CORS was already limited to the local Vite origins. The PostgreSQL login error response does not echo the password. Visit creation is validated. Queries go through EF Core.

## Remaining risks

- Publish the source only. Do not put this API on the public internet. There is no login, and the patient list returns every active patient.
- Model output is stored as clinical text and codes. Treat the codes as drafts.
- `Program.cs` loads user secrets after environment variables, so a user-secret value wins when both are set.
- The microphone depends on the browser speech engine. That audio is handled by the browser, not by this API.
- Sample charts in the seed scripts are fictional, but the database will hold whatever conversation you submit.

## Manual steps before public publication

1. Revoke and rotate the OpenAI API key. It was stored in plaintext in `appsettings.json` in this working tree. This folder is not a git repository, so that key is not in git history here. Rotate it anyway, because the file may have been copied, backed up, or indexed, and because local tool output during this review contained it. After rotation, update the user-secrets value. Do not put the new key in a file under this folder.
2. Change the local PostgreSQL password for the same reason, then update `ConnectionStrings:DefaultConnection` in user secrets.
3. Confirm `appsettings.json` still has an empty `OpenAI:ApiKey` and `Password=YOUR_PASSWORD` immediately before `git add`.
4. Initialize git from this folder so the root `.gitignore` applies. Do not force-add `bin/`, `obj/`, `node_modules/`, `dist/`, or `.cursor/`.
5. Add a license file if you want to allow reuse. None is included.
6. Add screenshots only if they show sample data and no secrets.

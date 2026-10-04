using System.ClientModel;
using System.Text;
using System.Text.Json;
using Ehr.Application.Abstractions.AI;
using Ehr.Application.DTOs.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Ehr.Infrastructure.AI;

public sealed class OpenAiService : IAIService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly ChatClient _chat;
    private readonly ILogger<OpenAiService> _log;

    public OpenAiService(IOptions<OpenAiOptions> options, ILogger<OpenAiService> log)
    {
        _log = log;
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.ApiKey))
        {
            throw new InvalidOperationException(
                "OpenAI:ApiKey is not configured. For local dev: " +
                "dotnet user-secrets set \"OpenAI:ApiKey\" \"YOUR_OPENAI_API_KEY\" --project src/Ehr.Api. " +
                "Or set environment variable OpenAI__ApiKey.");
        }

        if (string.IsNullOrWhiteSpace(o.Model))
        {
            throw new InvalidOperationException("OpenAI:Model is not configured (e.g. gpt-4o).");
        }

        _chat = new ChatClient(o.Model, new ApiKeyCredential(o.ApiKey));
    }

    public async Task<SoapGenerationResult> GenerateSoapAsync(
        SoapGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var system = """
            You are a U.S. clinical documentation assistant. From the visit context and transcript, write a clear SOAP note.
            Respond with a single JSON object only (no markdown code fences), keys:
            "subjective", "objective", "assessment", "plan", "chiefComplaint".
            Style: narrative prose. Subjective: patient-reported symptoms and goals. Objective: exam findings, imaging, or
            data stated in the transcript; if only inferred from context, label as clinical correlation or chart review.
            Assessment: diagnoses in clinical language. Plan: numbered list (1. 2. 3.) with procedures, referrals, and follow-up.
            chiefComplaint: one or two short sentences. Do not fabricate specific vitals or test results not in the text.
            """;

        var user = new StringBuilder();
        user.AppendLine($"Patient: {request.PatientName} | Age: {request.Age} | Sex: {request.Gender} | DOB: {request.DateOfBirth}");
        user.AppendLine($"Visit date: {request.VisitDate} | Type: {request.VisitType} | Setting: {request.FacilityType}");
        user.AppendLine($"Chief complaint (from chart): {request.ChiefComplaint}");
        user.AppendLine($"Visit notes: {request.VisitNotes}");
        if (!string.IsNullOrWhiteSpace(request.Transcript))
        {
            user.AppendLine("--- Transcript (doctor–patient or dictation) ---");
            user.AppendLine(request.Transcript);
        }
        else
        {
            user.AppendLine("No live transcript; use visit notes and chart context only.");
        }
        user.AppendLine($"Procedures (narrative): {request.ProceduresNarrative}");
        user.AppendLine($"Prescriptions (narrative): {request.PrescriptionsNarrative}");
        user.AppendLine($"Previous visits: {request.PreviousVisitsNarrative}");

        var json = await CompleteJsonAsync(
            [new SystemChatMessage(system), new UserChatMessage(user.ToString())],
            cancellationToken);
        var dto = JsonSerializer.Deserialize<SoapJsonDto>(json, Json)
            ?? throw new InvalidOperationException("SOAP JSON deserialize returned null.");

        return new SoapGenerationResult
        {
            Subjective = Coalesce(dto.Subjective),
            Objective = Coalesce(dto.Objective),
            Assessment = Coalesce(dto.Assessment),
            Plan = Coalesce(dto.Plan),
            ChiefComplaint = string.IsNullOrWhiteSpace(dto.ChiefComplaint) ? request.ChiefComplaint : dto.ChiefComplaint,
        };
    }

    public async Task<MedicalCodingResult> ExtractCodesAsync(
        MedicalCodingRequest request,
        CancellationToken cancellationToken = default)
    {
        var system = """
            You are a U.S. medical coding assistant. Suggest plausible ICD-10-CM and CPT/HCPCS codes (draft for human review only).
            Respond with a single JSON object (no markdown), keys:
            "primaryIcd10" (one primary diagnosis code, e.g. M50.22),
            "additionalNotes" (REQUIRED to list additional supporting ICD-10-CM lines as plain text: each line
            "CODE: short label" e.g. "M48.02: Spinal stenosis, cervical region" for related conditions mentioned),
            "lineItems" (array: "cptCode" as 5-digit CPT, "icd10Codes" string[] linking diagnoses to the procedure, "quantity",
            "modifier1", "modifier2", "reasoning" — include a line for the main interventional/visit procedure, e.g. ESI/fluoroscopy
            as applicable from the note).
            Prefer specific cervical/neuro/MSK codes when the note indicates level (e.g. C6-C7) or procedure (epidural steroid).
            If unsure, say so in reasoning and use conservative R69 only as last resort.
            """;

        var user = $"""
            Visit type: {request.VisitType}
            Chief complaint: {request.ChiefComplaint}
            Service date: {request.ServiceDate}
            Setting: {request.FacilityType}
            --- Clinical SOAP (for context) ---
            {request.ClinicalNoteSoap}
            --- Procedures narrative ---
            {request.ProceduresNarrative}
            --- Prescriptions narrative ---
            {request.PrescriptionsNarrative}
            """;

        var json = await CompleteJsonAsync(
            [new SystemChatMessage(system), new UserChatMessage(user)],
            cancellationToken);
        var dto = JsonSerializer.Deserialize<CodingJsonDto>(json, Json)
            ?? throw new InvalidOperationException("Coding JSON deserialize returned null.");

        var lines = (dto.LineItems ?? [])
            .Where(l => l is { CptCode: not null })
            .Select(
                l => new MedicalCodingLineItem
                {
                    CptCode = l.CptCode!.Trim(),
                    Icd10Codes = l.Icd10Codes ?? [],
                    Quantity = l.Quantity is > 0 ? l.Quantity.Value : 1,
                    Modifier1 = l.Modifier1?.Trim() ?? string.Empty,
                    Modifier2 = l.Modifier2?.Trim() ?? string.Empty,
                    Reasoning = l.Reasoning?.Trim() ?? string.Empty,
                })
            .ToList();

        return new MedicalCodingResult
        {
            PrimaryIcd10 = string.IsNullOrWhiteSpace(dto.PrimaryIcd10) ? "R69" : dto.PrimaryIcd10.Trim(),
            AdditionalNotes = dto.AdditionalNotes?.Trim() ?? string.Empty,
            LineItems = lines,
        };
    }

    private async Task<string> CompleteJsonAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            MaxOutputTokenCount = 4096,
        };
        var result = await _chat.CompleteChatAsync([.. messages], options, cancellationToken);
        var completion = result.Value;
        if (!string.IsNullOrEmpty(completion.Refusal))
        {
            _log.LogWarning("OpenAI refusal: {Refusal}", completion.Refusal);
            throw new InvalidOperationException($"The model refused to produce output: {completion.Refusal}");
        }

        if (completion.Content is not { Count: > 0 })
        {
            throw new InvalidOperationException("OpenAI returned no content.");
        }

        var text = new StringBuilder();
        foreach (var part in completion.Content)
        {
            if (part.Kind == ChatMessageContentPartKind.Text && part.Text is { Length: > 0 } t)
            {
                text.Append(t);
            }
        }

        if (text.Length == 0)
        {
            throw new InvalidOperationException("OpenAI returned no text in the completion.");
        }

        return text.ToString();
    }

    private static string Coalesce(string? s) => string.IsNullOrWhiteSpace(s) ? "Not documented." : s.Trim();

    private sealed class SoapJsonDto
    {
        public string? Subjective { get; set; }
        public string? Objective { get; set; }
        public string? Assessment { get; set; }
        public string? Plan { get; set; }
        public string? ChiefComplaint { get; set; }
    }

    private sealed class CodingJsonDto
    {
        public string? PrimaryIcd10 { get; set; }
        public string? AdditionalNotes { get; set; }
        public List<CodingLineJsonDto>? LineItems { get; set; }
    }

    private sealed class CodingLineJsonDto
    {
        public string? CptCode { get; set; }
        public List<string>? Icd10Codes { get; set; }
        public int? Quantity { get; set; }
        public string? Modifier1 { get; set; }
        public string? Modifier2 { get; set; }
        public string? Reasoning { get; set; }
    }
}

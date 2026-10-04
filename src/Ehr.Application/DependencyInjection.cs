using Ehr.Application.Abstractions.Clinical;
using Ehr.Application.Abstractions.Coding;
using Ehr.Application.Abstractions.Pipeline;
using Ehr.Application.Abstractions.Processing;
using Ehr.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Ehr.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddEhrApplication(this IServiceCollection services)
    {
        services.AddSingleton<IConversationProcessingService, ConversationProcessingService>();
        services.AddScoped<ISoapGenerationService, SoapGenerationService>();
        services.AddScoped<IMedicalCodingService, MedicalCodingService>();
        services.AddScoped<IEncounterAiPipelineService, EncounterAiPipelineService>();

        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IEncounterService, EncounterService>();
        services.AddScoped<ISoapNoteService, SoapNoteService>();
        return services;
    }
}

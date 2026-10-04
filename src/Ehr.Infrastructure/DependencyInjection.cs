using Ehr.Application.Abstractions.AI;
using Ehr.Application.Abstractions.Persistence;
using Ehr.Application.Abstractions.Repositories;
using Ehr.Infrastructure.AI;
using Ehr.Infrastructure.Persistence;
using Ehr.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ehr.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the EF Core <see cref="EhrDbContext"/> against the named PostgreSQL connection string.</summary>
    public static IServiceCollection AddEhrPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionName = "DefaultConnection")
    {
        var connectionString = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionName}' is not configured. " +
                "Add it under 'ConnectionStrings' in appsettings, an environment variable " +
                $"'ConnectionStrings__{connectionName}', or user secrets for local development.");
        }

        services.AddDbContext<EhrDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                b => b.MigrationsHistoryTable("__ef_migrations_history", "public"));
            options.UseSnakeCaseNamingConvention();
        });

        return services;
    }

    /// <summary>Repository implementations; call after <see cref="AddEhrPersistence"/>.</summary>
    public static IServiceCollection AddEhrRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IEncounterRepository, EncounterRepository>();
        services.AddScoped<ISoapNoteRepository, SoapNoteRepository>();
        services.AddScoped<IEncounterPipelinePersistence, EncounterPipelinePersistence>();
        return services;
    }

    /// <summary>Registers <see cref="OpenAiService"/> (OpenAI chat, JSON). Set <c>OpenAI:ApiKey</c> and <c>OpenAI:Model</c> (e.g. gpt-4o).</summary>
    public static IServiceCollection AddEhrAI(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.AddSingleton<IAIService, OpenAiService>();
        return services;
    }
}

using Ehr.Api;
using Ehr.Application;
using Ehr.Application.Options;
using Ehr.Application.Validation;
using Ehr.Infrastructure;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);
builder.Services.Configure<DocumentationOptions>(
    builder.Configuration.GetSection(DocumentationOptions.SectionName));
builder.Services.AddEhrAI(builder.Configuration);
builder.Services.AddEhrApplication();
builder.Services.AddEhrPersistence(builder.Configuration);
builder.Services.AddEhrRepositories();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePatientVisitRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UsePostgresAuthErrorJson();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

using Npgsql;

namespace Ehr.Api;

public static class PostgresExceptionMiddlewareExtensions
{
    public static WebApplication UsePostgresAuthErrorJson(this WebApplication app)
    {
        app.Use(
            async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (PostgresException ex) when (ex.SqlState is "28P01")
                {
                    if (context.Response.HasStarted)
                    {
                        throw;
                    }

                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsJsonAsync(
                        new
                        {
                            error =
                                "PostgreSQL rejected the password. Your ConnectionStrings:DefaultConnection password does not match the postgres user. "
                                + "Set your real install password, for example: "
                                + "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" "
                                + "\"Host=localhost;Port=5432;Database=ehr_db;Username=postgres;Password=YOUR_PASSWORD\" "
                                + "--project src/Ehr.Api (replace YOUR_PASSWORD; never commit it).",
                        });
                }
            });
        return app;
    }
}

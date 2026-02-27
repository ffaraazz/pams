using PAMS.API.Middleware;

namespace PAMS.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UsePamsMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        return app;
    }
}

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PAMS.Application.Exceptions;
using PAMS.Domain.Exceptions;

namespace PAMS.API.Middleware;

public sealed class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, title) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "ERR_NOT_FOUND", "Not Found"),
            ForbiddenException ex => (HttpStatusCode.Forbidden, ex.ErrorCode, "Forbidden"),
            ConflictException ex => (HttpStatusCode.Conflict, ex.ErrorCode, "Conflict"),
            CapacityExceededException => (HttpStatusCode.UnprocessableEntity, "ERR_CAPACITY_EXCEEDED", "Capacity Exceeded"),
            CircularReportingException => (HttpStatusCode.UnprocessableEntity, "ERR_CIRCULAR_REPORTING", "Circular Reporting"),
            InvalidSortException => (HttpStatusCode.BadRequest, "ERR_INVALID_SORT", "Invalid Sort Parameter"),
            FluentValidation.ValidationException => (HttpStatusCode.BadRequest, "ERR_VALIDATION", "Validation Error"),
            DomainException ex => (HttpStatusCode.UnprocessableEntity, ex.ErrorCode, "Domain Error"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "ERR_UNAUTHORIZED", "Unauthorized"),
            _ => (HttpStatusCode.InternalServerError, "ERR_INTERNAL", "Internal Server Error")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Type = $"https://pams.internal/errors/{errorCode}",
            Title = title,
            Status = (int)statusCode,
            Detail = statusCode == HttpStatusCode.InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            Instance = context.Request.Path
        };

        // Add extensions for CapacityExceededException
        if (exception is CapacityExceededException capacityEx)
        {
            problemDetails.Extensions["currentTotal"] = capacityEx.CurrentTotal;
            problemDetails.Extensions["requested"] = capacityEx.Requested;
            problemDetails.Extensions["available"] = capacityEx.Available;
        }

        // Add extensions for InvalidSortException
        if (exception is InvalidSortException sortEx)
        {
            problemDetails.Extensions["field"] = sortEx.Field;
            problemDetails.Extensions["allowedFields"] = sortEx.AllowedFields;
        }

        // Add validation errors for FluentValidation
        if (exception is FluentValidation.ValidationException validationEx)
        {
            var errors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
            problemDetails.Extensions["errors"] = errors;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsJsonAsync(problemDetails, options);
    }
}

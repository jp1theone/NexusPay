using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using NexusPay.Domain.Common;

namespace NexusPay.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (status, code, title) = exception switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "validation_error", "Request validation failed"),
                ConflictException => (StatusCodes.Status409Conflict, "conflict", "The request conflicts with current data"),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "concurrency_conflict", "The resource changed concurrently"),
                DomainRuleException => (StatusCodes.Status422UnprocessableEntity, "business_rule_violation", "The request violates a business rule"),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "not_found", "The requested resource was not found"),
                DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
                    (StatusCodes.Status409Conflict, "duplicate_resource", "A resource with the same unique value already exists"),
                _ => (StatusCodes.Status500InternalServerError, "unexpected_error", "An unexpected error occurred")
            };

            if (status >= 500)
                logger.LogError("Request failed with {ExceptionType}; correlation {CorrelationId}; message {Message}",
                    exception.GetType().Name, context.Items[CorrelationIdMiddleware.ItemName],
                    SensitiveDataRedactor.Redact(exception.Message));
            else if (exception is DbUpdateConcurrencyException concurrency)
                logger.LogWarning("Concurrency conflict for entities {Entities}; correlation {CorrelationId}",
                    concurrency.Entries.Select(entry => entry.Metadata.ClrType.Name).ToArray(),
                    context.Items[CorrelationIdMiddleware.ItemName]);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails { Status = status, Title = title, Type = $"https://httpstatuses.com/{status}" };
            problem.Extensions["code"] = code;
            problem.Extensions["correlationId"] = context.Items[CorrelationIdMiddleware.ItemName];
            if (exception is ValidationException validation)
                problem.Extensions["errors"] = validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray());
            await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
        }
    }
}

using System.Net.Mime;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MARS_BE.Common.Errors;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddProblemDetailsPolicies(this IServiceCollection services)
    {
        services.AddProblemDetails(opts =>
        {
            // Je kunt hier een global formatter aanpassen indien gewenst
            opts.CustomizeProblemDetails = ctx =>
            {
                // voeg correlation id, trace id, etc.
                var traceId = ctx.HttpContext.TraceIdentifier;
                ctx.ProblemDetails.Extensions["traceId"] = traceId;
            };
        });
        return services;
    }

    public static IApplicationBuilder UseAppExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(builder =>
        {
            builder.Run(async context =>
            {
                context.Response.ContentType = MediaTypeNames.Application.Json;

                var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
                var ex = exceptionHandler?.Error;

                ProblemDetails problem;

                switch (ex)
                {
                    case AppException appEx:
                        context.Response.StatusCode = appEx.Status;
                        problem = new ProblemDetails
                        {
                            Title  = appEx.Title,
                            Status = appEx.Status,
                            Detail = appEx.Message,
                            Instance = context.Request.Path
                        };
                        break;

                    case ArgumentException arg:
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        problem = new ProblemDetails
                        {
                            Title  = "Bad Request",
                            Status = StatusCodes.Status400BadRequest,
                            Detail = arg.Message,
                            Instance = context.Request.Path
                        };
                        break;

                    default:
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        problem = new ProblemDetails
                        {
                            Title  = "Internal Server Error",
                            Status = StatusCodes.Status500InternalServerError,
                            Detail = "An unexpected error occurred.",
                            Instance = context.Request.Path
                        };
                        break;
                }

                await context.Response.WriteAsJsonAsync(problem);
            });
        });

        return app;
    }
}

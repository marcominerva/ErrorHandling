using System.ComponentModel.DataAnnotations;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiniValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// This is necessary for Minimal API projects, that don't register an implementation for IActionResultExecutor<ObjectResult>
// (that is internally used by ProblemDetails).
builder.Services.TryAddSingleton<IActionResultExecutor<ObjectResult>, ProblemDetailsResultExecutor>();

builder.Services.AddProblemDetails(options =>
{
    /* These configurations are optional. */
    options.Map<HttpRequestException>
     (ex => new StatusCodeProblemDetails(StatusCodes.Status503ServiceUnavailable));

    options.Map<ApplicationException>(ex => new StatusCodeProblemDetails(StatusCodes.Status400BadRequest));

    // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is why it's added last.
    options.Map<Exception>(ex =>
    {
        var error = new StatusCodeProblemDetails(StatusCodes.Status500InternalServerError)
        {
            Title = "Internal error",
            Detail = ex.Message
        };

        return error;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseProblemDetails();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/errors/notfound", () =>
{
    // Thanks to the ProblemDetails NuGet package, this status code is returned using a ProblemDetails response.
    return Results.NotFound();
});

app.MapPost("/api/errors/people", (Person person) =>
{
    var isValid = MiniValidator.TryValidate(person, out var errors);
    if (!isValid)
    {
        return Results.ValidationProblem(errors);
    }

    return Results.NoContent();
});

app.MapGet("/api/errors/exception", () =>
{
    // Thanks to the ProblemDetails NuGet package, this error is returned using a ProblemDetails response.
    throw new Exception("Error 42");
});

app.Run();

public class Person
{
    [Required]
    public string FirstName { get; set; }

    public string LastName { get; set; }
}

/// <summary>
/// Executes an <see cref="ProblemDetailsResultExecutor"/> to write to the response.
/// </summary>
public class ProblemDetailsResultExecutor : IActionResultExecutor<ObjectResult>
{
    /// <summary>
    /// Executes the <see cref="ProblemDetailsResultExecutor"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/> for the current request.</param>
    /// <param name="result">The <see cref="ObjectResult"/>.</param>
    /// <returns>
    /// A <see cref="Task"/> which will complete once the <see cref="ObjectResult"/> is written to the response.
    /// </returns>
    public virtual Task ExecuteAsync(ActionContext context, ObjectResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        var executor = Results.Json(result.Value, null, "application/problem+json", result.StatusCode);
        return executor.ExecuteAsync(context.HttpContext);
    }
}
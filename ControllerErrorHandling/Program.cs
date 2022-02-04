using System.Diagnostics;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Redefine the factory method that is used to create a 400 Bad Request response when Model validation fails.
    // In this example, the status code is replaced using 422 instead of 400.
    options.InvalidModelStateResponseFactory = actionContext =>
    {
        var errors = actionContext.ModelState.Where(e => e.Value?.Errors.Any() ?? false)
            .Select(e => new ValidationError(e.Key, e.Value.Errors.First().ErrorMessage));

        var httpContext = actionContext.HttpContext;
        var statusCode = StatusCodes.Status422UnprocessableEntity;
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext.Request.Path,
            Title = "Validation errors occurred"
        };

        problemDetails.Extensions.Add("traceId", Activity.Current?.Id ?? httpContext.TraceIdentifier);
        problemDetails.Extensions.Add("errors", errors);

        var result = new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };

        return result;
    };
});

builder.Services.AddProblemDetails(options =>
{
    // If you just want to change the status code for Model validation problem, using the ProblemDetails NuGet package
    // you can use this line of code, that add a call to AddProblemDetailsConventions (line 65).
    //options.ValidationProblemStatusCode = StatusCodes.Status422UnprocessableEntity;

    /* These configurations are optional and can be used to map specific exceptions to custom status codes. */
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
})
//.AddProblemDetailsConventions()
;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseProblemDetails();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();

internal record ValidationError(string Name, string Message);
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace ControllerErrorHandling.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class ErrorsController : ControllerBase
{
    [HttpGet("notfound")]
    public IActionResult Get()
    {
        return NotFound();

        // If you want to customize for example the title of the NotFound response, you can use the Problem method.
        return Problem(statusCode: StatusCodes.Status404NotFound, title: "Resource not found");
    }

    [HttpPost("people")]
    public IActionResult Post(Person person) => Ok("Try not to set the FirstName");

    [HttpGet("exception")]
    // Thanks to the ProblemDetails NuGet package, this error is returned using a ProblemDetails response.
    public IActionResult GetException() => throw new Exception("Error 42");

    [HttpGet("httprequestexception")]
    // Thanks to the ProblemDetails NuGet package, this error is returned using a ProblemDetails response.
    public IActionResult GetHttpException() => throw new HttpRequestException("Extenal API calling error");
}

public class Person
{
    [Required]
    public string FirstName { get; set; }

    public string LastName { get; set; }

    [EmailAddress]
    [Required]
    public string Email { get; set; }
}

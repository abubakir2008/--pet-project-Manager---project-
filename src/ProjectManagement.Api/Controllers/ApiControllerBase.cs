using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.Common;

namespace ProjectManagement.Api.Controllers;

/// <summary>
/// Shared plumbing of the presentation layer: it builds the CurrentUser from the JWT
/// claims and translates the logic layer results into HTTP responses. Keeping the
/// mapping here is what allows the controllers to stay free of business rules.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected CurrentUser CurrentUser => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray());

    /// <summary>Maps a result without a value: success becomes 204 No Content.</summary>
    protected IActionResult FromResult(Result result) =>
        result.IsSuccess ? NoContent() : Error(result);

    /// <summary>Maps a result with a value: success becomes 200 OK with the value.</summary>
    protected ActionResult<T> FromResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : Error(result);

    /// <summary>Maps a created resource: success becomes 201 Created with the value.</summary>
    protected ActionResult<T> FromCreatedResult<T>(Result<T> result, string actionName, object routeValues) =>
        result.IsSuccess
            ? CreatedAtAction(actionName, routeValues, result.Value)
            : Error(result);

    private ObjectResult Error(Result result)
    {
        var (statusCode, title) = result.ErrorType switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status400BadRequest, "Invalid request")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = result.ErrorMessage,
            Instance = HttpContext.Request.Path
        };

        // "message" duplicates the first error for the client code, "errors" carries them all.
        problem.Extensions["message"] = result.ErrorMessage;
        problem.Extensions["errors"] = result.Errors;

        return StatusCode(statusCode, problem);
    }
}

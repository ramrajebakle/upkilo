
using Microsoft.AspNetCore.Mvc;

namespace Upkilo.API.Controllers;

/// <summary>
/// Base controller for API v1 endpoints.
/// All controllers inheriting from this will automatically be versioned under /api/v1/
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    // Common controller functionality can be added here
    protected IActionResult ErrorResponse(string message, string? errorCode = null, int statusCode = 400)
    {
        return StatusCode(statusCode, Upkilo.API.Middleware.ApiResponse.Fail(message, errorCode));
    }

    protected IActionResult SuccessResponse<T>(T data, string? message = null)
    {
        return Ok(Upkilo.API.Middleware.ApiResponse<T>.Ok(data, message));
    }
}

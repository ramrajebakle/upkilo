using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Upkilo.API.Controllers;

/// <summary>
/// Files controller for managing file uploads and media assets.
///
/// FEAT-01 FIX: this controller previously returned hardcoded fake data for every operation —
/// GET returned a fabricated file, LIST returned fake files, DELETE/MOVE/BULK-DELETE reported
/// success without doing anything, and STORAGE returned invented usage numbers. That silently
/// misled callers (e.g. uploads "succeeded" while files were lost; deletes "succeeded" while
/// nothing was removed). Until real object storage (Azure Blob / S3) is integrated, ALL endpoints
/// return 501 Not Implemented so the API never lies about persistence.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly ILogger<FilesController> _logger;

    public FilesController(ILogger<FilesController> logger)
    {
        _logger = logger;
    }

    private IActionResult NotImplementedStorage(string operation)
    {
        _logger.LogWarning("File {Operation} attempted but object storage is not integrated.", operation);
        return StatusCode(StatusCodes.Status501NotImplemented,
            new { error = "File storage is not yet configured. Please contact support." });
    }

    /// <summary>Upload file</summary>
    [HttpPost("upload")]
    public IActionResult UploadFile([FromForm] FileUploadRequest request) => NotImplementedStorage("upload");

    /// <summary>Get file by ID</summary>
    [HttpGet("{id}")]
    public IActionResult GetFile(Guid id) => NotImplementedStorage("get");

    /// <summary>Delete file</summary>
    [HttpDelete("{id}")]
    public IActionResult DeleteFile(Guid id) => NotImplementedStorage("delete");

    /// <summary>List files in folder</summary>
    [HttpGet]
    public IActionResult ListFiles([FromQuery] string? folder = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => NotImplementedStorage("list");

    /// <summary>Create folder</summary>
    [HttpPost("folders")]
    public IActionResult CreateFolder([FromBody] CreateFolderRequest request) => NotImplementedStorage("create-folder");

    /// <summary>Get folders</summary>
    [HttpGet("folders")]
    public IActionResult GetFolders() => NotImplementedStorage("list-folders");

    /// <summary>Move file to folder</summary>
    [HttpPost("{id}/move")]
    public IActionResult MoveFile(Guid id, [FromBody] MoveFileRequest request) => NotImplementedStorage("move");

    /// <summary>Get storage usage</summary>
    [HttpGet("storage")]
    public IActionResult GetStorageUsage() => NotImplementedStorage("storage-usage");

    /// <summary>Bulk delete files</summary>
    [HttpPost("bulk-delete")]
    public IActionResult BulkDelete([FromBody] FileBulkDeleteRequest request) => NotImplementedStorage("bulk-delete");
}

// Request DTOs
public class FileUploadRequest
{
    public IFormFile? File { get; set; }
    public string? Folder { get; set; }
    public string? Description { get; set; }
}

public class CreateFolderRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ParentPath { get; set; }
}

public class MoveFileRequest
{
    public string FolderPath { get; set; } = string.Empty;
}

public class FileBulkDeleteRequest
{
    public List<Guid> FileIds { get; set; } = new();
}

using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.Dtos;

namespace ProjectManagement.Api.Infrastructure;

/// <summary>
/// Stores project documents on the local disk under wwwroot/uploads/{projectId}.
/// Files are served back by UseStaticFiles.
/// </summary>
public class FileStorageService : IFileStorageService
{
    /// <summary>Extensions accepted for project documents.</summary>
    private static readonly string[] AllowedExtensions =
        { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".zip", ".txt" };

    public const long MaxSizeBytes = 25 * 1024 * 1024;

    private readonly string _root;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
    {
        _logger = logger;
        _root = ResolveRoot(env);
    }

    /// <summary>
    /// Storage root of the uploaded documents. The same path is handed to the static file
    /// middleware, and it is created up front because a freshly cloned repository has no
    /// wwwroot folder yet.
    /// </summary>
    public static string ResolveRoot(IWebHostEnvironment env)
    {
        var root = Path.Combine(
            string.IsNullOrEmpty(env.WebRootPath) ? Path.Combine(env.ContentRootPath, "wwwroot") : env.WebRootPath,
            "uploads");

        Directory.CreateDirectory(root);
        return root;
    }

    public async Task<Result<(string StoredFileName, long Size)>> SaveAsync(FileUploadDto file, int projectId, CancellationToken ct = default)
    {
        if (file.Length <= 0)
            return Result<(string, long)>.Validation("The file is empty.");

        if (file.Length > MaxSizeBytes)
            return Result<(string, long)>.Validation($"The file is larger than the {MaxSizeBytes / 1024 / 1024} MB limit.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return Result<(string, long)>.Validation($"File type '{extension}' is not allowed.");

        var projectDirectory = Path.Combine(_root, projectId.ToString());
        Directory.CreateDirectory(projectDirectory);

        // A generated name keeps the original file name out of the file system,
        // so uploads cannot overwrite each other or escape the storage root.
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(projectDirectory, storedFileName);

        try
        {
            await using var stream = File.Create(fullPath);
            await file.Content.CopyToAsync(stream, ct);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Could not store the uploaded file for project {ProjectId}.", projectId);
            return Result<(string, long)>.Failure(ErrorType.Conflict, "The file could not be saved. Please try again.");
        }

        return Result<(string, long)>.Success(($"{projectId}/{storedFileName}", file.Length));
    }

    public void Delete(string storedFileName)
    {
        var fullPath = Path.Combine(_root, storedFileName);

        // Never follow a path that points outside of the storage root.
        if (!Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch (IOException ex)
        {
            // A locked or already removed file must not break the database operation.
            _logger.LogWarning(ex, "Could not delete the stored file {StoredFileName}.", storedFileName);
        }
    }

    public string GetPublicUrl(string storedFileName) => $"/uploads/{storedFileName}";
}

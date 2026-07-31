namespace ProjectManagement.DataAccess.Entities;

/// <summary>Metadata of a file uploaded on the last step of the project wizard.</summary>
public class ProjectDocument
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    /// <summary>Original file name as it was uploaded by the user.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Relative path inside the storage root; generated, so it is safe to use on disk.</summary>
    public string StoredFileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

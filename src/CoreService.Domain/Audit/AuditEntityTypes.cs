namespace CoreService.Domain.Audit;

/// <summary>
/// Audit entities as consts
/// </summary>
public static class AuditEntityTypes
{
    #region core
    public const string User = "user";
    public const string Role = "role";
    public const string Token = "token";
    public const string Audit = "audit";
    #endregion

    #region study
    public const string Course = "course";
    public const string Group = "group";
    public const string Subgroup = "subgroup";
    public const string Enrollment = "enrollment";
    public const string Checkpoint = "checkpoint";
    public const string Submission = "submission";
    public const string Artifact = "artifact";
    public const string Grade = "grade";
    #endregion
}

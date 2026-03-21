namespace CoreService.Domain.Audit;

/// <summary>
/// Minimal list of events
/// </summary>
public static class AuditEventTypes
{
    #region core
    public const string CoreAuthLoginSucceeded = "core.auth.login.succeeded";
    public const string CoreAuthLoginFailed = "core.auth.login.failed";
    public const string CoreAuthRefreshSucceeded = "core.auth.refresh.succeeded";
    public const string CoreAuthLogout = "core.auth.logout";

    public const string CoreUserCreated = "core.user.created";
    public const string CoreUserUpdated = "core.user.updated";
    public const string CoreUserDeactivated = "core.user.deactivated";
    public const string CoreUserRoleChanged = "core.user.role.changed";
    #endregion core

    #region study
    public const string StudyEnrollmentAdded = "study.enrollment.added";
    public const string StudyEnrollmentMoved = "study.enrollment.moved";
    public const string StudyEnrollmentRemoved = "study.enrollment.removed";

    public const string StudyCheckpointCreated = "study.checkpoint.created";
    public const string StudyCheckpointUpdated = "study.checkpoint.updated";

    public const string StudySubmissionCreated = "study.submission.created";
    public const string StudySubmissionStatusChanged = "study.submission.status.changed";

    public const string StudyArtifactAdded = "study.artifact.added";

    public const string StudyGradeCreated = "study.grade.created";
    public const string StudyGradeUpdated = "study.grade.updated";
    public const string StudyGradeStatusChanged = "study.grade.status.changed";
    #endregion study

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        CoreAuthLoginSucceeded,
        CoreAuthLoginFailed,
        CoreAuthRefreshSucceeded,
        CoreAuthLogout,
        CoreUserCreated,
        CoreUserUpdated,
        CoreUserDeactivated,
        CoreUserRoleChanged,

        StudyEnrollmentAdded,
        StudyEnrollmentMoved,
        StudyEnrollmentRemoved,
        StudyCheckpointCreated,
        StudyCheckpointUpdated,
        StudySubmissionCreated,
        StudySubmissionStatusChanged,
        StudyArtifactAdded,
        StudyGradeCreated,
        StudyGradeUpdated,
        StudyGradeStatusChanged
    };

    public static bool IsKnown(string eventType) =>
        !string.IsNullOrWhiteSpace(eventType) && All.Contains(eventType);
}

using CoreService.Domain.Common;

namespace CoreService.Domain.Security;

/// <summary>
/// Mapper from enum into string for DB and JWT
/// </summary>
public static class RoleCodeMapper
{
    public static string ToDb(RoleCode code) => code switch
    {
        RoleCode.Student => "student",
        RoleCode.Assistant => "assistant",
        RoleCode.Teacher => "teacher",
        RoleCode.Manager => "manager",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown RoleCode")
    };

    public static RoleCode FromDb(string value)
    {
        Guard.NotNullOrWhiteSpace(value, nameof(value));

        return value.Trim().ToLowerInvariant() switch
        {
            "student" => RoleCode.Student,
            "assistant" => RoleCode.Assistant,
            "teacher" => RoleCode.Teacher,
            "manager" => RoleCode.Manager,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown role code")
        };
    }

    /// <summary>
    /// Validation QoL
    /// </summary>
    public static IReadOnlyList<RoleCode> All { get; } =
        new[] { RoleCode.Student, RoleCode.Assistant, RoleCode.Teacher, RoleCode.Manager };

    /// <summary>
    /// Displayed names of roles
    /// </summary>
    public static string ToDisplay(RoleCode code) => code switch
    {
        RoleCode.Student => "Студент",
        RoleCode.Assistant => "Учебный ассистент",
        RoleCode.Teacher => "Преподаватель",
        RoleCode.Manager => "Менеджер дисциплины",
        _ => code.ToString()
    };
}

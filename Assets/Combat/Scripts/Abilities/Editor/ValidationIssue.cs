using System.Collections.Generic;

public enum ValidationSeverity
{
    Error,
    Warning,
    Info
}

public enum ValidationCategory
{
    Config,
    Composition,
    Parameters,
    RuntimeReadiness,
    Production,
    Scene
}

public struct ValidationIssue
{
    public ValidationSeverity severity;
    public ValidationCategory category;
    public string message;

    public ValidationIssue(ValidationSeverity severity, ValidationCategory category, string message)
    {
        this.severity = severity;
        this.category = category;
        this.message = message;
    }

    public override string ToString() => message;

    public static ValidationIssue Error(ValidationCategory category, string message) =>
        new ValidationIssue(ValidationSeverity.Error, category, message);

    public static ValidationIssue Warning(ValidationCategory category, string message) =>
        new ValidationIssue(ValidationSeverity.Warning, category, message);

    public static ValidationIssue Info(ValidationCategory category, string message) =>
        new ValidationIssue(ValidationSeverity.Info, category, message);

    public static bool ListHasErrors(IList<ValidationIssue> issues)
    {
        for (int i = 0; i < issues.Count; i++)
            if (issues[i].severity == ValidationSeverity.Error) return true;
        return false;
    }

    public static bool ListHasWarnings(IList<ValidationIssue> issues)
    {
        for (int i = 0; i < issues.Count; i++)
            if (issues[i].severity == ValidationSeverity.Warning) return true;
        return false;
    }
}
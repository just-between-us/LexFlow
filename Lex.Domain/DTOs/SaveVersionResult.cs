namespace Lex.Domain.DTOs;

public class SaveVersionResult
{
    public bool CreateNewVersion { get; set; }
    public string? ChangeSummary { get; set; }
}
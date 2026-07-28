using Lex.Domain.Enums;
using MudBlazor;

namespace Lex.Components.Utils;

public static class DisplayHelper
{
    public static string GetActivityTypeDisplay(UserActivityType type) => type switch
    {
        UserActivityType.Guest => "Гость",
        UserActivityType.IndividualEntrepreneur => "ИП",
        UserActivityType.OOO => "ООО",
        UserActivityType.SelfEmployed => "Самозанятый",
        UserActivityType.Startup => "Стартап",
        UserActivityType.Freelancer => "Фрилансер",
        UserActivityType.TeamMember => "Участник команды",
        _ => "Прочее"
    };
     public static string GetStatusDisplayName(DocumentStatus status) => status switch
    {
        DocumentStatus.Draft => "Черновик",
        DocumentStatus.InReview => "На проверке",
        DocumentStatus.Ready => "Готов",
        DocumentStatus.Signed => "Подписан",
        DocumentStatus.Archived => "Архив",
        _ => "Неизвестно"
    };

    public static Color GetStatusColor(DocumentStatus status) => status switch
    {
        DocumentStatus.Draft => Color.Default,
        DocumentStatus.InReview => Color.Info,
        DocumentStatus.Ready => Color.Primary,
        DocumentStatus.Signed => Color.Success,
        DocumentStatus.Archived => Color.Dark,
        DocumentStatus.Deleted => Color.Error,
        _ => Color.Default
    };

    public static string GetPrivacyDisplayName(DocumentPrivacy privacy) => privacy switch
    {
        DocumentPrivacy.Private => "Только автор",
        DocumentPrivacy.Public => "Публичный",
        DocumentPrivacy.Protected => "Защищённый",
        _ => "Неизвестно"
    };

    public static string GetTypeDisplayName(DocumentType type) => type switch
    {
        DocumentType.Contract => "Договор",
        DocumentType.Claim => "Иск",
        DocumentType.Policy => "Политика",
        DocumentType.Agreement => "Соглашение",
        DocumentType.Consent => "Согласие",
        _ => "Прочее"
    };

    public static Color GetTypeColor(DocumentType type) => type switch
    {
        DocumentType.Contract => Color.Primary,
        DocumentType.Claim => Color.Error,
        DocumentType.Policy => Color.Warning,
        DocumentType.Agreement => Color.Info,
        DocumentType.Consent => Color.Success,
        _ => Color.Dark
    };

    public static string GetTypeIcon(DocumentType type) => type switch
    {
        DocumentType.Contract => Icons.Material.Filled.Description,
        DocumentType.Claim => Icons.Material.Filled.Gavel,
        DocumentType.Policy => Icons.Material.Filled.Policy,
        DocumentType.Agreement => Icons.Material.Filled.Handshake,
        DocumentType.Consent => Icons.Material.Filled.HowToReg,
        _ => Icons.Material.Filled.InsertDriveFile
    };
    public static string GetStatusDescription(DocumentStatus status) => status switch
    {
        DocumentStatus.Draft => "Черновик — документ в разработке, не готов к использованию",
        DocumentStatus.InReview => "На проверке — ожидает согласования",
        DocumentStatus.Ready => "Готов — прошёл проверку, можно использовать",
        DocumentStatus.Signed => "Подписан — юридически финализирован, дата фиксируется автоматически",
        DocumentStatus.Archived => "В архиве — скрыт из активных списков, но доступен по прямой ссылке",
        DocumentStatus.Deleted => "Удалён",
        _ => ""
    };
    public static string GetRelativeTime(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        return span switch
        {
            { TotalMinutes: < 1 } => "только что",
            { TotalMinutes: < 60 } => $"{(int)span.TotalMinutes} мин. назад",
            { TotalHours: < 24 } => $"{(int)span.TotalHours} ч. назад",
            { TotalDays: < 7 } => $"{(int)span.TotalDays} дн. назад",
            _ => utc.ToLocalTime().ToString("dd.MM.yyyy")
        };
    }
    public static Color GetColorFromEmail(string email)
    {
        var hash = Math.Abs(email.GetHashCode()) % 5;
        return hash switch
        {
            0 => Color.Primary,
            1 => Color.Secondary,
            2 => Color.Info,
            3 => Color.Success,
            _ => Color.Tertiary
        };
    }
    
}
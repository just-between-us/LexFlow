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
        DocumentStatus.Draft => Color.Secondary,
        DocumentStatus.InReview => Color.Info,
        DocumentStatus.Ready => Color.Success,
        DocumentStatus.Signed => Color.Primary,
        DocumentStatus.Archived => Color.Warning,
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
}
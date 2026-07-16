using Lex.Domain.Enums;

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
}
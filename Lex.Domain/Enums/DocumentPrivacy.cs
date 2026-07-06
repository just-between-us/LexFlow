namespace Lex.Domain.Enums;

public enum DocumentPrivacy
{
    Private, //Видно только автору
    Public, //Видно всем, могут менять все
    Protected, //Видно всем, но ножут менять только 1 пользователь
}
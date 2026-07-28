namespace Lex.Domain.Enums;

public enum DocumentLifecycleFilter
{
    NotDeleted = 0, // по умолчанию — активные и архивные, без удалённых
    ActiveOnly = 1, // не удалён и не в архиве
    ArchivedOnly = 2, // не удалён, в архиве
    DeletedOnly = 3 // удалён 
}
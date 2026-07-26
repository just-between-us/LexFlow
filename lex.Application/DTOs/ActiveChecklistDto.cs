namespace lex.Application.DTOs;

public class ActiveChecklistDto
{
    public Guid Id { get; set; }
    public Guid ChecklistId { get; set; }
    public string ChecklistTitle { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
}
public class ActiveChecklistDetailsDto
{
    public Guid Id { get; set; }
    public Guid ChecklistId { get; set; }
    public string ChecklistTitle { get; set; } = string.Empty;
    public string ChecklistDescription { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public List<ActiveChecklistItemDto> Items { get; set; } = new();

    public Guid OwnerUserId { get; set; }
    public string OwnerFullName { get; set; } = string.Empty;
    public bool CurrentUserIsOwner { get; set; }
    public bool CurrentUserCanEdit { get; set; } // владелец или редактор

    public List<ActiveChecklistEditorDto> Editors { get; set; } = new();

    public Guid? ClientOrganizationId { get; set; }
    public string? ClientOrganizationName { get; set; }
    public bool OwnerHasOrganization { get; set; } // чтобы показать кнопку "прикрепить" даже когда сейчас не прикреплено
}

public class ActiveChecklistEditorDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class ActiveChecklistItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsCompleted { get; set; }
    public string? Note { get; set; }
}
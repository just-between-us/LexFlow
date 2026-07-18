using System.ComponentModel.DataAnnotations;

namespace Lex.Domain.DTOs;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TaxId { get; set; }
    public string? RegistrationNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Guid OwnerUserId { get; set; }
    public string OwnerFullName { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }

    public bool IsCurrentUserOwner { get; set; }

    public List<StaffMemberDto> Staff { get; set; } = new(); // без владельца — он показывается отдельно
}

public class StaffMemberDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class OrganizationEditModel
{
    [Required(ErrorMessage = "Введите название организации")]
    [MaxLength(300, ErrorMessage = "Максимум 300 символов")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Максимум 1000 символов")]
    public string? Description { get; set; }

    [MaxLength(200, ErrorMessage = "Максимум 200 символов")]
    public string? TaxId { get; set; }

    [MaxLength(200, ErrorMessage = "Максимум 200 символов")]
    public string? RegistrationNumber { get; set; }
}
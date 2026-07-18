using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lex.Domain.Entities;

public class ClientOrganization : BaseEntity
{
    [Required, MaxLength(300)] public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)] public string? Description { get; set; }
    
    [MaxLength(200)] public string? TaxId { get; set; } // ИНН
    
    [MaxLength(200)] public string? RegistrationNumber { get; set; } // ОГРН/ОГРНИП
    
    public bool IsActive { get; set; } = true;


    public Guid OwnerUserId { get; set; } //Создатель/Владелец организации (Один-ко-многим: у организации один владелец)
    [ForeignKey("OwnerUserId")] public User OwnerUser { get; set; } = null!; //Как я понимаю в случае с полями не совпадающими с сущностью по имени нужен FK
    
    public ICollection<User> Staff { get; set; } = new List<User>(); //Сотрудники организации (Один-ко-многим: в организации много сотрудников)
    
    //Документы и активные чек-листы организации
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<ActiveChecklist> ActiveChecklists { get; set; } = new List<ActiveChecklist>();
}
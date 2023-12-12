using WebApplication1.Authentication.Model;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Data.Entity;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Count { get; set; }
    public int Kaina { get; set; }
    public DateTime CreationDate { get; set; }
    
    public required Category Category { get; set; }

    [Required]
    public required string UserId { get; set; }
    public ForumRestUser User { get; set; }
}

public record ProductDto(int Id, string Name, int Count, int Kaina, DateTime CreationData);
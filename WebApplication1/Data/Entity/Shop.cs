using WebApplication1.Authentication.Model;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Data.Entity;

public class Shop
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string City { get; set; }
    public string Adresas { get; set; }
}

public record ShopDto(int Id, string Name, string City, string Adresas);
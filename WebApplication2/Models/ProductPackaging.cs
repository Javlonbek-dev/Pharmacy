namespace WebApplication2.Models;

public class ProductPackaging
{
    public int Id { get; set; }
    public int ProductId { get; set; } 
    public Product Product { get; set; } 
    public string Type { get; set; } 
    public string Description { get; set; } 
    public string MaterialCode { get; set; } 
}
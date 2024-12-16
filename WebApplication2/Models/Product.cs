namespace WebApplication2.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public float Price { get; set; }
    public float? OldPrice { get; set; }
    public int Stock { get; set; }
    public string Quantity { get; set; }
    
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    public string ImageUrl { get; set; }
    public DateTime Validaty_date { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ICollection<ProductSpecification> Specifications { get; set; } 
    public ICollection<ProductPackaging> Packagings { get; set; } 
}

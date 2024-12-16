using X.PagedList;

namespace WebApplication2.Models.ViewModels;

public class MalumotViewModels
{
    public List<Product>? Products { get; set; }
    public Product? Product { get; set; }
    public List<Product> NewProducts { get; set; } 
    public List<Review> Reviews { get; set; }
    public IPagedList<Product> ProductPagedList { get; set; }
    public List<Category> Categories { get; set; }
    public IEnumerable<ProductPackaging> Packagings { get; set; }
    public ProductSpecification? Specifications { get; set; }
    public List<Cart> CartItems { get; set; }
    public float Subtotal { get; set; }
    public float Total { get; set; }
    public List<Order> Orders { get; set; }
}
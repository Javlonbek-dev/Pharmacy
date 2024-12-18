using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using X.PagedList;
using WebApplication2.Models.ViewModels;
using X.PagedList.Extensions;

namespace WebApplication2.Controllers;

public class HomeController : Controller
{

    AppDbContext db;

    public HomeController(AppDbContext _context)
    {
        db = _context;
    }
    
    public IActionResult BoshSahifa()
    {
        var products = db.Products.ToList();
        var reviews = db.Reviews
            .Include(r => r.User)
            .Take(6)
            .ToList();
        var newProducts = db.Products
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToList();

        MalumotViewModels malumot = new()
        {
            Products = products,
            NewProducts = newProducts,
            Reviews = reviews
        };

        return View(malumot);
    }

    public IActionResult Card()
    {
        var userId = 1;
        var cartItems = db.Carts
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToList();
        var count = cartItems.Count;
        var subtotal = cartItems.Sum(c => (c.Product.OldPrice ?? 0) * c.Quantity);
        var total = cartItems.Sum(c => (c.Product.Price) * c.Quantity);

        MalumotViewModels malumot = new MalumotViewModels
        {
            CartItems = cartItems,
            Subtotal = subtotal,
            Total = total,
            CartItemCount= count
        };

        return View(malumot);
    }

    public IActionResult Contact()
    {
        var viewModel = new MalumotViewModels
        {
            Contact = new Contact() 
        };

        return View(viewModel);
    }


    [HttpPost]
    public IActionResult Contact(MalumotViewModels malumot)
    {
        var model1 = new Contact
        {
            First_Name = malumot.Contact.First_Name,
            Last_Name = malumot.Contact.Last_Name,
            Email = malumot.Contact.Email,
            Subject = malumot.Contact.Subject,
            Message = malumot.Contact.Message
        };
        db.Contacts.Add(model1);
        db.SaveChanges();
            
        return RedirectToAction("ThankYou", "Home");
    }
    public IActionResult Haqida()
    {
        var team = db.Users
            .Where(t => t.Role == "Team")
            .Take(4)
            .ToList();
           

        MalumotViewModels malumot = new()
        {
            Users = team
        };
        
        return View(malumot);
    }

    public IActionResult MahsulotShow(int id)
    {
        var product = db.Products
            .Include(p => p.Packagings)
            .Include(p => p.Specifications)
            .FirstOrDefault(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        MalumotViewModels malumot = new()
        {
            Product = product,
        };

        return View(malumot);
    }

    public IActionResult Xarid(int? page, float? minPrice, float? maxPrice, string sortOrder, int categoryId)
    {
        int pageSize = 9;
        int pageNumber = page ?? 1;

        var products = db.Products.AsQueryable();
        var category = db.Categories.AsQueryable();
        
        if (minPrice != null && maxPrice != null)
        {
            products = products.Where(p => p.Price >= minPrice && p.Price <= maxPrice);
        }
        if (categoryId != null)
        {
            products = products.Where(p => p.CategoryId == categoryId);
        }

        switch (sortOrder)
        {
            case "name_asc":
                products = products.OrderBy(p => p.Name);
                break;
            case "name_desc":
                products = products.OrderByDescending(p => p.Name);
                break;
            case "price_low_high":
                products = products.OrderBy(p => p.Price);
                break;
            case "price_high_low":
                products = products.OrderByDescending(p => p.Price);
                break;
            default:
                products = products.OrderBy(p => p.Id);
                break;
        }

        var pagedProducts = products.ToPagedList(pageNumber, pageSize);

        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.SortOrder = sortOrder;
        ViewBag.CategoryId = categoryId;

        MalumotViewModels model = new()
        {
            ProductPagedList = pagedProducts,
            Categories = db.Categories.ToList() 

        };

        return View(model);
    }


    [HttpPost]
    public IActionResult AddToCart(int productId, int quantity)
    {
        int userId = 1;

        var product = db.Products.FirstOrDefault(p => p.Id == productId);
        if (product == null)
        {
            return NotFound("Mahsulot topilmadi.");
        }

        var existingCartItem = db.Carts
            .FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);

        if (existingCartItem != null)
        {
            existingCartItem.Quantity += quantity;
            existingCartItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var cartItem = new Cart
            {
                UserId = userId,
                ProductId = productId,
                Quantity = quantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Carts.Add(cartItem);
        }

        db.SaveChanges();

        return RedirectToAction("Card", "Home");
    }


    public IActionResult RemoveFromCart(int id)
    {
        var cartItem = db.Carts.FirstOrDefault(c => c.Id == id);

        if (cartItem != null)
        {
            db.Carts.Remove(cartItem);
            db.SaveChanges();
        }

        return RedirectToAction("Card");
    }

    [HttpPost]
    public IActionResult UpdateCart(List<int> cartIds, List<int> quantities)
    {
        for (int i = 0; i < cartIds.Count; i++)
        {
            var cartItem = db.Carts.FirstOrDefault(c => c.Id == cartIds[i]);
            if (cartItem != null)
            {
                cartItem.Quantity = quantities[i];
                cartItem.UpdatedAt = DateTime.UtcNow;
            }
        }

        db.SaveChanges();

        return RedirectToAction("Card");
    }

    public IActionResult Order()
    {
        int userId = 1; 
        var orders = db.Orders
            .Include(o => o.Product)
            .Where(o => o.UserId == userId && o.IsDeleted == false)
            .ToList();

        var total = orders.Sum(o => o.TotalPrice);

        MalumotViewModels malumot = new()
        {
            Orders = orders,
            Total = total
        };
        return View(malumot);
    }


    [HttpPost]
    public IActionResult ProceedToCheckout()
    {
        int userId = 1; 

        var cartItems = db.Carts
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToList();

        if (!cartItems.Any())
        {
            return RedirectToAction("Card"); 
        }

        foreach (var cartItem in cartItems)
        {
            var order = new Order
            {
                UserId = userId,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                TotalPrice = cartItem.Product.Price * cartItem.Quantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Orders.Add(order);
        }

        db.Carts.RemoveRange(cartItems);
        db.SaveChanges();

        return RedirectToAction("Order", "Home"); 
    }

    [HttpPost]
    public IActionResult PlaceOrder(string PaymentMethod, string TransactionId)
    {
            int userId = 1;

            var oldOrders = db.Orders
                .Where(o => o.UserId == userId && !o.IsDeleted)
                .ToList();

            if (!oldOrders.Any())
            {
                ModelState.AddModelError("", "Hech qanday buyurtma topilmadi.");
                return View("Order");
            }

            var orderToPay = oldOrders.FirstOrDefault();
            
            var payment = new Payment
            {
                OrderId = orderToPay.Id, 
                PaymentMethod = PaymentMethod,
                TransactionId = TransactionId,
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Payments.Add(payment);
            db.SaveChanges();
            
            foreach (var order in oldOrders)
            {
                order.IsDeleted = true;
                order.DeletedAt = DateTime.UtcNow;
            }

            db.SaveChanges();

            return RedirectToAction("ThankYou", "Home");
        }

    public IActionResult ThankYou()
    {
        return View();
    }
}
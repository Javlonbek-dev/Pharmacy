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

        var subtotal = cartItems.Sum(c => (c.Product.OldPrice ?? 0) * c.Quantity);
        var total = cartItems.Sum(c => (c.Product.Price) * c.Quantity);

        MalumotViewModels malumot = new MalumotViewModels
        {
            CartItems = cartItems,
            Subtotal = subtotal,
            Total = total
        };

        return View(malumot);
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult Haqida()
    {
        return View();
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

    public IActionResult Xarid(int? page, float? minPrice, float? maxPrice, string sortOrder)
    {
        int pageSize = 9;
        int pageNumber = page ?? 1;

        var products = db.Products.AsQueryable();

        if (minPrice != null && maxPrice != null)
        {
            products = products.Where(p => p.Price >= minPrice && p.Price <= maxPrice);
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

        MalumotViewModels model = new()
        {
            ProductPagedList = pagedProducts
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
            .Where(o => o.UserId == userId)
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

        var oldOrders = db.Orders.Where(o => o.UserId == userId).ToList();
        if (oldOrders.Any())
        {
            db.Orders.RemoveRange(oldOrders);
            db.SaveChanges();
        }

        var cartItems = db.Carts
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToList();

        if (!cartItems.Any() || string.IsNullOrWhiteSpace(PaymentMethod))
        {
            ModelState.AddModelError("", "Please fill in all billing details and select products.");
            return View("Order", new MalumotViewModels
            {
                Orders = oldOrders,
                Total = oldOrders.Sum(o => o.TotalPrice)
            });
        }

        foreach (var cart in cartItems)
        {
            var newOrder = new Order
            {
                UserId = userId,
                ProductId = cart.ProductId,
                Quantity = cart.Quantity,
                TotalPrice = cart.Product.Price * cart.Quantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Orders.Add(newOrder);
        }

        var payment = new Payment
        {
            OrderId = 0,
            PaymentMethod = PaymentMethod,
            TransactionId = TransactionId,
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);

        db.Carts.RemoveRange(cartItems);

        db.SaveChanges();

        TempData["Success"] = "Your order has been successfully placed!";
        return RedirectToAction("ThankYou", "Home");
    }

    public IActionResult ThankYou()
    {
        return View();
    }
}
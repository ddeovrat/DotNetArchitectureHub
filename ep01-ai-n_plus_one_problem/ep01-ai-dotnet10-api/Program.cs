using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// 1. Database Setup & Logging Configuration
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite("Data Source=app.db")
    .LogTo(Console.WriteLine, LogLevel.Information) // Logs raw SQL queries directly to console
    .Options;

using var db = new AppDbContext(options);
await db.Database.EnsureCreatedAsync();
await SeedDataAsync(db);

Console.WriteLine("\n--- N+1 QUERY DEMO ---\n");
var stopwatch = Stopwatch.StartNew();

// ------------------------------------------------------------------
// ❌ BAD CODE: N+1 Query
// ------------------------------------------------------------------
var orders = await db.Orders.ToListAsync(); // 1 query for orders

foreach (var order in orders)
{
    // Triggers a separate query for each order’s customer
    var customer = await db.Customers
        .Where(c => c.Id == order.CustomerId)
        .FirstAsync();

    Console.WriteLine($"Order {order.Id} by {customer.Name}");
}

stopwatch.Stop();
Console.WriteLine($"\n[N+1 Version Execution Time]: {stopwatch.ElapsedMilliseconds} ms");


// ------------------------------------------------------------------
// ✅ FIXED CODE: Eager Loading / Projection
// ------------------------------------------------------------------
Console.WriteLine("\n--- OPTIMIZED QUERY DEMO ---\n");
stopwatch.Restart();

var orderSummaries = await db.Orders
    .Include(o => o.Customer) // eager load customers in one query
    .AsNoTracking()
    .Select(o => new
    {
        o.Id,
        CustomerName = o.Customer.Name
    })
    .ToListAsync();

foreach (var summary in orderSummaries)
{
    Console.WriteLine($"Order {summary.Id} by {summary.CustomerName}");
}

stopwatch.Stop();
Console.WriteLine($"\n[Optimized Version Execution Time]: {stopwatch.ElapsedMilliseconds} ms");

// ------------------------------------------------------------------
// Data Models & DbContext
// ------------------------------------------------------------------
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Order> Orders { get; set; } = [];
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
}

// ------------------------------------------------------------------
// Data Seeding Helper
// ------------------------------------------------------------------
partial class Program
{
    static async Task SeedDataAsync(AppDbContext db)
    {
        if (await db.Customers.AnyAsync()) return;

        var customers = Enumerable.Range(1, 50).Select(i => new Customer
        {
            Name = $"Customer {i}",
            Orders = Enumerable.Range(1, 3)
                .Select(j => new Order { })
                .ToList()
        }).ToList();

        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();
    }
}

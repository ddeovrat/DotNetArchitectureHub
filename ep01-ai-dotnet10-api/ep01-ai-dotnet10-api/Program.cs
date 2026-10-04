using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

// 1. Database Setup & Logging Configuration
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite("Data Source=app.db")
    .LogTo(Console.WriteLine, LogLevel.Information) // Logs raw SQL queries directly to the console
    .Options;

using var db = new AppDbContext(options);
await db.Database.EnsureCreatedAsync();
await SeedDataAsync(db);

Console.WriteLine("\n--- EXECUTING AI-GENERATED LINQ QUERY ---\n");
var stopwatch = Stopwatch.StartNew();

// ------------------------------------------------------------------
// AI-GENERATED FLOCKED CODE (The N+1 Query)
// ------------------------------------------------------------------
//Not Optimized
var customers = await db.Customers.ToListAsync(); // 1 Query to fetch Customers

foreach (var customer in customers)
{
    // AI creates a separate query inside the loop for EVERY customer!
    var totalSpent = await db.Orders
        .Where(o => o.CustomerId == customer.Id)
        .SumAsync(o => o.TotalAmount);

    Console.WriteLine($"Customer: {customer.Name} | Total Spent: ${totalSpent}");
}

stopwatch.Stop();
Console.WriteLine($"\n[AI Version Execution Time]: {stopwatch.ElapsedMilliseconds} ms");
//Optimized below one

Console.WriteLine("\n--- EXECUTING SENIOR REFACTORED LINQ QUERY ---\n");
stopwatch.Restart();

// ------------------------------------------------------------------
// SENIOR REFACTORED CODE (Single SQL Projection)
// ------------------------------------------------------------------
var customerSummaries = await db.Customers
    .AsNoTracking() // Disables change tracking overhead for read-only query
    .Select(c => new
    {
        c.Name,
        TotalSpent = c.Orders.Sum(o => o.TotalAmount) // Evaluated ENTIRELY inside SQL
    })
    .ToListAsync();

foreach (var summary in customerSummaries)
{
    Console.WriteLine($"Customer: {summary.Name} | Total Spent: ${summary.TotalSpent}");
}

stopwatch.Stop();
Console.WriteLine($"\n[Senior Version Execution Time]: {stopwatch.ElapsedMilliseconds} ms");

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
    public decimal TotalAmount { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
}

// Data Seeding Helper
partial class Program
{
    static async Task SeedDataAsync(AppDbContext db)
    {
        if (await db.Customers.AnyAsync()) return;

        var customers = Enumerable.Range(1, 100).Select(i => new Customer
        {
            Name = $"Customer {i}",
            Orders = Enumerable.Range(1, 5).Select(j => new Order { TotalAmount = j * 10.5m }).ToList()
        }).ToList();

        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();
    }
}
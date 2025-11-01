using System;
using System.Collections.Generic;
using System.Linq;

namespace ProcurementManagement.Domain
{
    // Role-based access for users
    public enum UserRole { Manager = 0, Admin = 1 }

    // Order lifecycle statuses
    public enum OrderStatus { InProgress = 1, Completed = 2, Cancelled = 3 }


    // ---- Users ----
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // demo only (plaintext)
        public UserRole Role { get; set; } = UserRole.Manager;

        // Convenience factory for quick seeding
        public static User Create(string login, string password, UserRole role) =>
            new() { Login = login, Password = password, Role = role };
    }

    // ---- Suppliers & Customers ----
    public class Supplier
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
        public string? Notes { get; set; }
    }


    // ---- Orders ----
    public class OrderItem
    {
        public Guid StockItemId { get; set; }               // link to Stock.Id (GUID)
        public decimal Quantity { get; set; }               // ordered amount
        public decimal UnitPrice { get; set; }              // must be >= 0
    }

    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SupplierId { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.InProgress;

        // Timestamps in UTC (ISO 8601 in JSON)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<OrderItem> Items { get; set; } = new();

        // Computed total (not stored in JSON; calculated on read)
        public decimal Total => Items.Sum(i => i.UnitPrice * i.Quantity);
    }


    public class Stock
    {
        public Guid Id { get; set; } = Guid.NewGuid();   // internal unique ID (GUID)
        public string Name { get; set; } = string.Empty; // human-readable name
        public decimal QuantityOnHand { get; set; } = 0; // current stock balance
        public decimal? TargetLevel { get; set; }        // optional guidance
        public decimal? LastPurchasePrice { get; set; }  // optional reference
    }


    // ---- Single-file database root ----
    public class Database
    {
        public int SchemaVersion { get; set; } = 1;
        public List<User> Users { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<Stock> Stock { get; set; } = new();
    }
}

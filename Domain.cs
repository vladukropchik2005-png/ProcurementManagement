using System;
using System.Collections.Generic;
using System.Linq;

namespace ProcurementManagement.Domain
{
    
    public enum UserRole { Manager = 0, Admin = 1 }

    
    public enum OrderStatus { InProgress = 1, Completed = 2, Cancelled = 3 }


    
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; 
        public UserRole Role { get; set; } = UserRole.Manager;

        
        public static User Create(string login, string password, UserRole role) =>
            new() { Login = login, Password = password, Role = role };
    }

    
    public class Supplier
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
        public string? Notes { get; set; }
    }


    
    public class OrderItem
    {
        public Guid StockItemId { get; set; }               
        public decimal Quantity { get; set; }               
        public decimal UnitPrice { get; set; }              
    }

    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SupplierId { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.InProgress;

        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<OrderItem> Items { get; set; } = new();

        
        public decimal Total => Items.Sum(i => i.UnitPrice * i.Quantity);
    }


    public class Stock
    {
        public Guid Id { get; set; } = Guid.NewGuid();   
        public string Name { get; set; } = string.Empty; 
        public decimal QuantityOnHand { get; set; } = 0; 
        public decimal? TargetLevel { get; set; }        
        public decimal? LastPurchasePrice { get; set; }  
    }


    
    public class Database
    {
        public int SchemaVersion { get; set; } = 1;
        public List<User> Users { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<Stock> Stock { get; set; } = new();
    }
}

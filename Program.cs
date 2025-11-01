using ProcurementManagement.Data;
using ProcurementManagement.Domain;
using ProcurementManagement.Services;
using ProcurementManagement.UI;

// Build a safe, portable path (next to the executable)
var dbPath = Path.Combine(AppContext.BaseDirectory, "Storage", "database.json");

var store = new JsonStore(dbPath);
await store.LoadAsync();

// Seed minimal users if empty
if (!store.Db.Users.Any())
{
    store.Db.Users.Add(User.Create("admin", "admin", UserRole.Admin));
    store.Db.Users.Add(User.Create("manager", "manager", UserRole.Manager));
    await store.SaveAsync();
}

// Build services
var auth = new AuthService(store);
var orders = new OrderService(store);
var suppliers = new SupplierService(store);
var stock = new StockService(store);

// Run the app
var app = new MainMenu(auth, orders, suppliers, stock, defaultPageSize: 10);
app.Run();

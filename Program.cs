using ProcurementManagement.Data;
using ProcurementManagement.Domain;
using ProcurementManagement.Services;
using ProcurementManagement.UI;


var dbPath = Path.Combine(AppContext.BaseDirectory, "Storage", "database.json");

var store = new JsonStore(dbPath);
await store.LoadAsync();


if (!store.Db.Users.Any())
{
    store.Db.Users.Add(User.Create("admin", "admin", UserRole.Admin));
    store.Db.Users.Add(User.Create("manager", "manager", UserRole.Manager));
    await store.SaveAsync();
}


var auth = new AuthService(store);
var orders = new OrderService(store);
var suppliers = new SupplierService(store);
var stock = new StockService(store);


var app = new MainMenu(auth, orders, suppliers, stock, defaultPageSize: 10);
app.Run();

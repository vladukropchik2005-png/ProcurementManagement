using ProcurementManagement.Domain;
using ProcurementManagement.Services;
using static ProcurementManagement.UI.ConsoleHelpers;

namespace ProcurementManagement.UI
{
    /// <summary>
    /// Console main menu with role-based options and index-based selection UX.
    /// </summary>
    public class MainMenu
    {
        private readonly AuthService _auth;
        private readonly OrderService _orders;
        private readonly SupplierService _suppliers;
        private readonly StockService _stock;
        private readonly int _defaultPageSize;

        public MainMenu(
            AuthService auth,
            OrderService orders,
            SupplierService suppliers,
            StockService stock,
            int defaultPageSize = 10)
        {
            _auth = auth;
            _orders = orders;
            _suppliers = suppliers;
            _stock = stock;
            _defaultPageSize = defaultPageSize;
        }

        public void Run()
        {
            Console.Clear();
            Console.WriteLine("=== Procurement Control ===");
            var login = Prompt("Login: ");
            var pwd = Prompt("Password: ");

            var user = _auth.Login(login, pwd);
            if (user is null)
            {
                Console.WriteLine("Invalid credentials.");
                Pause();
                return;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Logged in as: {user.Login} ({user.Role})");
                Console.WriteLine("1) Stock");
                Console.WriteLine("2) Orders");
                Console.WriteLine("3) Suppliers");
                if (user.Role == UserRole.Admin)
                    Console.WriteLine("4) Create User (Admin)");
                Console.WriteLine("0) Exit");

                var key = Prompt("Select: ");
                if (key == "0") break;

                switch (key)
                {
                    case "1": StockMenu(user); break;
                    case "2": OrdersMenu(user); break;
                    case "3": SuppliersMenu(user); break;
                    case "4": if (user.Role == UserRole.Admin) CreateUserMenu(); break;
                    default: break;
                }
            }
        }

        // ----------------- STOCK -----------------
        private void StockMenu(User current)
        {
            var page = 1;
            var pageSize = _defaultPageSize;
            string? lastQuery = null;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("== Stock ==");
                if (lastQuery is null) lastQuery = Prompt("Search by name (empty = all): ");

                var list = _stock.Search(lastQuery).ToList();

                // render page with 1-based indexes
                var pageData = ConsoleHelpers.Page(list, page, pageSize).ToList();
                for (int i = 0; i < pageData.Count; i++)
                {
                    var s = pageData[i];
                    Console.WriteLine($"[{i + 1}] {s.Id} | {s.Name,-30} | Qty: {s.QuantityOnHand,8:0.##} | LastPrice: {(s.LastPurchasePrice?.ToString("0.##") ?? "-")}");
                }

                Console.WriteLine();
                Console.WriteLine("[N]ext  [P]rev  [S]ize  [F]ilter  [B]ack");
                if (current.Role == UserRole.Admin)
                    Console.WriteLine("[C]reate item  [E] <index>  |  [E] ID <guid>");

                var cmdLine = (Console.ReadLine() ?? "").Trim();
                if (string.Equals(cmdLine, "b", StringComparison.OrdinalIgnoreCase)) break;
                if (string.Equals(cmdLine, "n", StringComparison.OrdinalIgnoreCase)) { page++; continue; }
                if (string.Equals(cmdLine, "p", StringComparison.OrdinalIgnoreCase)) { page = Math.Max(1, page - 1); continue; }
                if (string.Equals(cmdLine, "s", StringComparison.OrdinalIgnoreCase)) { pageSize = PromptInt("Page size", pageSize); continue; }
                if (string.Equals(cmdLine, "f", StringComparison.OrdinalIgnoreCase)) { lastQuery = null; page = 1; continue; }

                if (current.Role == UserRole.Admin)
                {
                    if (string.Equals(cmdLine, "c", StringComparison.OrdinalIgnoreCase))
                    {
                        var name = Prompt("Name: ");
                        var tgt = PromptDecimal("Target level (optional, 0 = skip)", 0m);
                        decimal? tl = tgt > 0m ? tgt : null;
                        var lp = PromptDecimal("Last purchase price (optional, 0 = skip)", 0m);
                        decimal? lpp = lp > 0m ? lp : null;

                        var created = _stock.CreateAsync(name, tl, lpp).GetAwaiter().GetResult();
                        Console.WriteLine($"Created: {created.Id} | {created.Name}");
                        Pause();
                        continue;
                    }

                    // Commands: "e 3" or "e id <guid>"
                    var parts = cmdLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && string.Equals(parts[0], "e", StringComparison.OrdinalIgnoreCase))
                    {
                        // by index
                        if (parts.Length == 2 && int.TryParse(parts[1], out var idx))
                        {
                            var item = (idx >= 1 && idx <= pageData.Count) ? pageData[idx - 1] : null;
                            if (item is null) { Console.WriteLine("Invalid index."); Pause(); continue; }
                            var newQty = PromptDecimal($"New quantity for '{item.Name}'", item.QuantityOnHand);
                            if (_stock.SetQuantityAsync(item.Id, newQty).GetAwaiter().GetResult())
                                Console.WriteLine("Quantity updated.");
                            else
                                Console.WriteLine("Failed to update.");
                            Pause(); continue;
                        }

                        // by GUID
                        if (parts.Length == 3 && string.Equals(parts[1], "id", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!Guid.TryParse(parts[2], out var gid)) { Console.WriteLine("Invalid GUID."); Pause(); continue; }
                            var item = _stock.GetById(gid);
                            if (item is null) { Console.WriteLine("Not found."); Pause(); continue; }
                            var newQty = PromptDecimal($"New quantity for '{item.Name}'", item.QuantityOnHand);
                            if (_stock.SetQuantityAsync(item.Id, newQty).GetAwaiter().GetResult())
                                Console.WriteLine("Quantity updated.");
                            else
                                Console.WriteLine("Failed to update.");
                            Pause(); continue;
                        }
                    }
                }
            }
        }

        // ----------------- ORDERS -----------------
        private void OrdersMenu(User current)
        {
            var page = 1;
            var pageSize = _defaultPageSize;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("== Orders ==");

                var supplierId = PromptGuid("Filter: SupplierId (empty = any): ");
                var stockId = PromptGuid("Filter: StockItemId (empty = any): ");
                var statusStr = Prompt("Filter: Status [InProgress/Completed/Cancelled or empty]: ");
                var sortBy = Prompt("SortBy [date_desc/date_asc]: ");
                if (string.IsNullOrWhiteSpace(sortBy)) sortBy = "date_desc";

                OrderStatus? st = Enum.TryParse<OrderStatus>(statusStr, true, out var stv) ? stv : null;

                var data = _orders.Query(
                    supplierId: supplierId,
                    stockItemId: stockId,
                    status: st,
                    sortBy: sortBy
                );

                var pageData = ConsoleHelpers.Page(data, page, pageSize).ToList();
                for (int i = 0; i < pageData.Count; i++)
                {
                    var o = pageData[i];
                    var supName = _suppliers.GetById(o.SupplierId)?.Name ?? "(unknown)";
                    Console.WriteLine($"[{i + 1}] {o.Id} | {o.CreatedAt:u} | {o.Status,-10} | Supplier: {supName,-20} | Items: {o.Items.Count,2} | Total: {o.Total,10:0.00}");
                }

                Console.WriteLine();
                Console.WriteLine("[N]ext  [P]rev  [S]ize  [B]ack");
                if (current.Role == UserRole.Admin)
                    Console.WriteLine("[A]dd order  [U]pdate status (by index or 'id <GUID>')");

                var cmd = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
                if (cmd == "b") break;
                if (cmd == "n") { page++; continue; }
                if (cmd == "p") { page = Math.Max(1, page - 1); continue; }
                if (cmd == "s") { pageSize = PromptInt("Page size", pageSize); continue; }

                if (current.Role == UserRole.Admin)
                {
                    if (cmd == "a")
                    {
                        // 1) select supplier via indexed list
                        var supplier = SelectSupplier();
                        if (supplier is null) { Console.WriteLine("Cancelled."); Pause(); continue; }

                        // 2) select stock lines via indexed list loop
                        var lines = new List<(Guid stockId, decimal qty, decimal price)>();
                        while (true)
                        {
                            var chosen = SelectStockItem();
                            if (chosen is null) break; // empty to finish
                            var qty = PromptDecimal("Quantity", 1m);
                            var price = PromptDecimal("Unit price", 0m);
                            lines.Add((chosen.Value, qty, price));
                            Console.WriteLine("Added line. Press ENTER to add more, or type 'done' to finish.");
                            var r = Console.ReadLine();
                            if (string.Equals(r, "done", StringComparison.OrdinalIgnoreCase)) break;
                        }

                        if (!lines.Any())
                        {
                            Console.WriteLine("No lines added. Cancelled.");
                            Pause(); continue;
                        }

                        try
                        {
                            var created = _orders.CreateAsync(supplier.Value, lines).GetAwaiter().GetResult();
                            Console.WriteLine($"Order created: {created.Id}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed: {ex.Message}");
                        }
                        Pause(); continue;
                    }
                    else if (cmd == "u")
                    {
                        // Allow picking order by visible index on current page OR by GUID
                        var pick = Prompt("Enter order index on page or 'id <GUID>': ").Trim();

                        Guid? orderId = null;

                        // Try index first
                        if (int.TryParse(pick, out var idx))
                        {
                            var chosen = (idx >= 1 && idx <= pageData.Count) ? pageData[idx - 1] : null;
                            if (chosen is null)
                            {
                                Console.WriteLine("Invalid index.");
                                Pause();
                                continue;
                            }
                            orderId = chosen.Id;
                        }
                        else
                        {
                            // Maybe "id <GUID>"
                            var parts = pick.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2 &&
                                parts[0].Equals("id", StringComparison.OrdinalIgnoreCase) &&
                                Guid.TryParse(parts[1], out var gid))
                            {
                                orderId = gid;
                            }
                        }

                        if (orderId is null)
                        {
                            Console.WriteLine("Invalid input. Use index (e.g., '3') or 'id <GUID>'.");
                            Pause();
                            continue;
                        }

                        var newStatusStr = Prompt("New status [InProgress/Completed/Cancelled]: ");
                        if (!Enum.TryParse<OrderStatus>(newStatusStr, true, out var ns))
                        {
                            Console.WriteLine("Invalid status.");
                            Pause();
                            continue;
                        }

                        var ok = _orders.ChangeStatusAsync(orderId.Value, ns).GetAwaiter().GetResult();
                        Console.WriteLine(ok ? "Status updated." : "Order not found.");
                        Pause();
                        continue;
                    }

                }
            }
        }

        // Let the user choose a supplier by index (with search & pagination).
        // Returns the SupplierId (Guid) or null if cancelled.
        private Guid? SelectSupplier()
        {
            var page = 1;
            var pageSize = _defaultPageSize;
            string? query = "";

            while (true)
            {
                Console.Clear();
                Console.WriteLine("== Select Supplier ==");
                if (query is null) query = Prompt("Search by name (empty = all): ");

                var list = _suppliers.Search(query).ToList();
                var pageData = ConsoleHelpers.Page(list, page, pageSize).ToList();

                for (int i = 0; i < pageData.Count; i++)
                {
                    var s = pageData[i];
                    Console.WriteLine($"[{i + 1}] {s.Id} | {s.Name} | {s.ContactEmail} | {s.Phone} | {s.Notes}");
                }

                Console.WriteLine();
                Console.WriteLine("Enter index to choose, or:");
                Console.WriteLine("[N]ext  [P]rev  [S]ize  [F]ilter  [B]ack (cancel)");
                var cmd = (Console.ReadLine() ?? "").Trim();

                if (string.Equals(cmd, "b", StringComparison.OrdinalIgnoreCase)) return null;
                if (string.Equals(cmd, "n", StringComparison.OrdinalIgnoreCase)) { page++; continue; }
                if (string.Equals(cmd, "p", StringComparison.OrdinalIgnoreCase)) { page = Math.Max(1, page - 1); continue; }
                if (string.Equals(cmd, "s", StringComparison.OrdinalIgnoreCase)) { pageSize = PromptInt("Page size", pageSize); continue; }
                if (string.Equals(cmd, "f", StringComparison.OrdinalIgnoreCase)) { query = null; page = 1; continue; }

                if (int.TryParse(cmd, out var idx))
                {
                    var chosen = (idx >= 1 && idx <= pageData.Count) ? pageData[idx - 1] : null;
                    if (chosen is null) { Console.WriteLine("Invalid index."); Pause(); continue; }
                    return chosen.Id;
                }
            }
        }

        // Let the user choose a stock item by index (with search & pagination).
        // Returns the StockId (Guid) or null if cancelled/finished.
        private Guid? SelectStockItem()
        {
            var page = 1;
            var pageSize = _defaultPageSize;
            string? query = "";

            while (true)
            {
                Console.Clear();
                Console.WriteLine("== Select Stock Item ==");
                if (query is null) query = Prompt("Search by name (empty = all): ");

                var list = _stock.Search(query).ToList();
                var pageData = ConsoleHelpers.Page(list, page, pageSize).ToList();

                for (int i = 0; i < pageData.Count; i++)
                {
                    var s = pageData[i];
                    Console.WriteLine($"[{i + 1}] {s.Id} | {s.Name,-30} | Qty: {s.QuantityOnHand,8:0.##}");
                }

                Console.WriteLine();
                Console.WriteLine("Enter index to choose, or:");
                Console.WriteLine("[N]ext  [P]rev  [S]ize  [F]ilter  [B]ack (finish)");
                var cmd = (Console.ReadLine() ?? "").Trim();

                if (string.Equals(cmd, "b", StringComparison.OrdinalIgnoreCase)) return null;
                if (string.Equals(cmd, "n", StringComparison.OrdinalIgnoreCase)) { page++; continue; }
                if (string.Equals(cmd, "p", StringComparison.OrdinalIgnoreCase)) { page = Math.Max(1, page - 1); continue; }
                if (string.Equals(cmd, "s", StringComparison.OrdinalIgnoreCase)) { pageSize = PromptInt("Page size", pageSize); continue; }
                if (string.Equals(cmd, "f", StringComparison.OrdinalIgnoreCase)) { query = null; page = 1; continue; }

                if (int.TryParse(cmd, out var idx))
                {
                    var chosen = (idx >= 1 && idx <= pageData.Count) ? pageData[idx - 1] : null;
                    if (chosen is null) { Console.WriteLine("Invalid index."); Pause(); continue; }
                    return chosen.Id;
                }
            }
        }

        // ----------------- SUPPLIERS -----------------
        private void SuppliersMenu(User current)
        {
            var page = 1;
            var pageSize = _defaultPageSize;
            string? lastQuery = null;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("== Suppliers ==");
                if (lastQuery is null) lastQuery = Prompt("Search by name (empty = all): ");
                var list = _suppliers.Search(lastQuery).ToList();

                var pageData = ConsoleHelpers.Page(list, page, pageSize).ToList();
                for (int i = 0; i < pageData.Count; i++)
                {
                    var s = pageData[i];
                    Console.WriteLine($"[{i + 1}] {s.Id} | {s.Name} | {s.ContactEmail} | {s.Phone} | {s.Notes}");
                }

                Console.WriteLine();
                Console.WriteLine("[N]ext  [P]rev  [S]ize  [F]ilter  [B]ack");
                if (current.Role == UserRole.Admin)
                    Console.WriteLine("[C]reate supplier");

                var cmd = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
                if (cmd == "b") break;
                if (cmd == "n") { page++; continue; }
                if (cmd == "p") { page = Math.Max(1, page - 1); continue; }
                if (cmd == "s") { pageSize = PromptInt("Page size", pageSize); continue; }
                if (cmd == "f") { lastQuery = null; page = 1; continue; }

                if (current.Role == UserRole.Admin && cmd == "c")
                {
                    var name = Prompt("Name: ");
                    var email = Prompt("Email (optional): ");
                    var phone = Prompt("Phone (optional): ");
                    var notes = Prompt("Notes (optional): ");

                    try
                    {
                        var s = _suppliers.CreateAsync(name, email, phone, notes).GetAwaiter().GetResult();
                        Console.WriteLine($"Supplier created: {s.Id} | {s.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed: {ex.Message}");
                    }
                    Pause();
                }
            }
        }

        // ----------------- CREATE USER (admin) -----------------
        private void CreateUserMenu()
        {
            Console.Clear();
            Console.WriteLine("== Create User ==");
            var login = Prompt("Login: ");
            var pwd = Prompt("Password: ");
            var roleStr = Prompt("Role [Admin/Manager]: ");
            if (!Enum.TryParse<UserRole>(roleStr, true, out var role))
                role = UserRole.Manager;

            try
            {
                var created = _auth.CreateUserAsync(login, pwd, role).GetAwaiter().GetResult();
                Console.WriteLine($"User created: {created.Login} ({created.Role})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed: {ex.Message}");
            }
            Pause();
        }
    }
}

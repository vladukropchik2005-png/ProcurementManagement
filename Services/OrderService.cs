using ProcurementManagement.Data;
using ProcurementManagement.Domain;

namespace ProcurementManagement.Services
{
    /// <summary>
    /// Orders: query with filters/sorting, create, and status updates (with stock update on Completed).
    /// </summary>
    public class OrderService
    {
        private readonly JsonStore _store;

        public OrderService(JsonStore store) => _store = store;

        // Query with optional filters and sorting
        public IEnumerable<Order> Query(
            Guid? supplierId = null,
            Guid? stockItemId = null,      // filter orders containing this stock item
            OrderStatus? status = null,
            DateTime? from = null,
            DateTime? to = null,
            string sortBy = "date_desc")
        {
            IEnumerable<Order> q = _store.Db.Orders;

            if (supplierId is not null) q = q.Where(o => o.SupplierId == supplierId.Value);
            if (status is not null) q = q.Where(o => o.Status == status.Value);
            if (from is not null) q = q.Where(o => o.CreatedAt >= from.Value);
            if (to is not null) q = q.Where(o => o.CreatedAt < to.Value);
            if (stockItemId is not null) q = q.Where(o => o.Items.Any(i => i.StockItemId == stockItemId.Value));

            q = sortBy switch
            {
                "date_asc" => q.OrderBy(o => o.CreatedAt),
                _ => q.OrderByDescending(o => o.CreatedAt) // date_desc (default)
            };

            return q;
        }

        // Create new order
        public async Task<Order> CreateAsync(Guid supplierId, IEnumerable<(Guid stockId, decimal qty, decimal price)> lines)
        {
            var items = new List<OrderItem>();

            foreach (var (stockId, qty, price) in lines)
            {
                if (qty <= 0m || price < 0m) throw new ArgumentException("Invalid qty/price.");
                // Validate references
                if (!_store.Db.Stock.Any(s => s.Id == stockId))
                    throw new ArgumentException("Stock item not found.");

                items.Add(new OrderItem
                {
                    StockItemId = stockId,
                    Quantity = qty,
                    UnitPrice = price
                });
            }

            if (!_store.Db.Suppliers.Any(s => s.Id == supplierId))
                throw new ArgumentException("Supplier not found.");

            var order = new Order
            {
                SupplierId = supplierId,
                Status = OrderStatus.InProgress, // or Pending if you keep it
                CreatedAt = DateTime.UtcNow,
                Items = items
            };

            _store.Db.Orders.Add(order);
            await _store.SaveAsync();
            return order;
        }

        // Change status; when Completed -> add quantities to stock once
        public async Task<bool> ChangeStatusAsync(Guid orderId, OrderStatus newStatus)
        {
            var order = _store.Db.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order is null) return false;

            if (order.Status == newStatus) return true;

            // If moving to Completed, update stock quantities
            if (newStatus == OrderStatus.Completed && order.Status != OrderStatus.Completed)
            {
                foreach (var line in order.Items)
                {
                    var stock = _store.Db.Stock.FirstOrDefault(s => s.Id == line.StockItemId);
                    if (stock is null) continue; // inconsistent data, skip safely
                    stock.QuantityOnHand += line.Quantity;
                    stock.LastPurchasePrice = line.UnitPrice;
                }
            }

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;
            await _store.SaveAsync();
            return true;
        }
    }
}

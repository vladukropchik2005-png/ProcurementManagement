using ProcurementManagement.Data;
using ProcurementManagement.Domain;

namespace ProcurementManagement.Services
{
    /// <summary>
    /// Stock service: listing, search, create, and manual quantity edits.
    /// </summary>
    public class StockService
    {
        private readonly JsonStore _store;

        public StockService(JsonStore store) => _store = store;

        // Simple search by name (contains). If query null/empty -> return all.
        public IEnumerable<Stock> Search(string? query = null)
        {
            var q = _store.Db.Stock.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(query))
                q = q.Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            return q.OrderBy(s => s.Name);
        }

        // Create a new stock item (quantity starts at 0)
        public async Task<Stock> CreateAsync(string name, decimal? targetLevel = null, decimal? lastPurchasePrice = null)
        {
            var item = new Stock
            {
                Name = name.Trim(),
                QuantityOnHand = 0m,
                TargetLevel = targetLevel,
                LastPurchasePrice = lastPurchasePrice
            };
            _store.Db.Stock.Add(item);
            await _store.SaveAsync();
            return item;
        }

        // Manual quantity edit (admin action): sets absolute value
        public async Task<bool> SetQuantityAsync(Guid stockId, decimal newQuantity)
        {
            if (newQuantity < 0m) return false;
            var item = _store.Db.Stock.FirstOrDefault(s => s.Id == stockId);
            if (item is null) return false;

            item.QuantityOnHand = newQuantity;
            await _store.SaveAsync();
            return true;
        }

        public Stock? GetById(Guid id) => _store.Db.Stock.FirstOrDefault(s => s.Id == id);
    }
}

using ProcurementManagement.Domain;
using ProcurementManagement.Data;

namespace ProcurementManagement.Services
{
    /// <summary>
    /// Basic lookup/search for suppliers + create.
    /// </summary> 
    public class SupplierService
    {
        private readonly JsonStore _store;

        public SupplierService(JsonStore store) => _store = store;

        public IEnumerable<Supplier> Search(string? name = null)
        {
            var q = _store.Db.Suppliers.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(name))
                q = q.Where(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            return q.OrderBy(s => s.Name);
        }

        public Supplier? GetById(Guid id) => _store.Db.Suppliers.FirstOrDefault(s => s.Id == id);

        public async Task<Supplier> CreateAsync(string name, string? email = null, string? phone = null, string? notes = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.");

            var s = new Supplier
            {
                Name = name.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };
            _store.Db.Suppliers.Add(s);
            await _store.SaveAsync();
            return s;
        }
    }
}

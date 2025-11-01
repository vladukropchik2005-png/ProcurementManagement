using ProcurementManagement.Domain;
using ProcurementManagement.Data;

namespace ProcurementManagement.Services
{
    /// <summary>
    /// Trivial authentication for demo purposes (plaintext passwords).
    /// </summary>
    public class AuthService
    {
        private readonly JsonStore _store;

        public AuthService(JsonStore store) => _store = store;

        public User? Login(string login, string password)
        {
            // NOTE: For production use a proper password hashing strategy.
            return _store.Db.Users.FirstOrDefault(u =>
                string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);
        }

        // --- Added: simple user creation for Admin menu ---
        public async Task<User> CreateUserAsync(string login, string password, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Login is required.");
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.");

            if (_store.Db.Users.Any(u => string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("User with this login already exists.");

            var user = User.Create(login.Trim(), password, role);
            _store.Db.Users.Add(user);
            await _store.SaveAsync();
            return user;
        }
    }
}

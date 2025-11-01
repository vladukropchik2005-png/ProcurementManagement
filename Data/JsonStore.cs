using System.Text.Json;
using ProcurementManagement.Domain;

namespace ProcurementManagement.Data
{
    /// <summary>
    /// Single-file JSON storage for the whole Database object.
    /// Keeps an in-memory Db and provides async load/save with atomic writes.
    /// </summary>
    public class JsonStore
    {
        // Absolute or relative path to database.json
        private readonly string _path;

        // JSON serializer options: case-insensitive, indented output
        private readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        // In-memory state
        public Database Db { get; private set; } = new();

        public JsonStore(string path) => _path = path;

        /// <summary>
        /// Load database from disk; if file is missing or broken, create a fresh one.
        /// </summary>
        public async Task LoadAsync()
        {
            EnsureDirectory();

            if (!File.Exists(_path))
            {
                // First run: start with an empty DB and persist it
                Db = new Database();
                await SaveAsync();
                return;
            }

            try
            {
                using var fs = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var db = await JsonSerializer.DeserializeAsync<Database>(fs, _opts);
                Db = db ?? new Database();
            }
            catch
            {
                // If the file is corrupted/unreadable — start fresh (demo-friendly behavior)
                Db = new Database();
                await SaveAsync();
            }
        }

        /// <summary>
        /// Save current Db to disk using a temp file and atomic replace.
        /// </summary>
        public async Task SaveAsync()
        {
            EnsureDirectory();

            var tmp = _path + ".tmp";

            // Write to a temp file first
            using (var fs = File.Open(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(fs, Db, _opts);
                await fs.FlushAsync(); // ensure buffered bytes are pushed to OS
            }

            // Replace the target file atomically (fallback to Move if needed)
            try
            {
#if NET6_0_OR_GREATER
                File.Replace(tmp, _path, destinationBackupFileName: null);
#else
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(tmp, _path);
#endif
            }
            catch
            {
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(tmp, _path);
            }
        }


        // --------------- helpers ---------------
        private void EnsureDirectory()
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir); // safe if already exists
        }
    }
}

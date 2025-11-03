using System.Text.Json;
using ProcurementManagement.Domain;

namespace ProcurementManagement.Data
{
    
    public class JsonStore
    {
        
        private readonly string _path;

        
        private readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        
        public Database Db { get; private set; } = new();

        public JsonStore(string path) => _path = path;

        
        public async Task LoadAsync()
        {
            EnsureDirectory();

            if (!File.Exists(_path))
            {
                
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
                
                Db = new Database();
                await SaveAsync();
            }
        }

        
        public async Task SaveAsync()
        {
            EnsureDirectory();

            var tmp = _path + ".tmp";

            
            using (var fs = File.Open(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(fs, Db, _opts);
                await fs.FlushAsync(); 
            }

            
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


        
        private void EnsureDirectory()
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir); 
        }
    }
}

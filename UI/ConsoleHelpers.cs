namespace ProcurementManagement.UI
{
    public static class ConsoleHelpers
    {
        public static string Prompt(string label)
        {
            Console.Write(label);
            return Console.ReadLine() ?? string.Empty;
        }

        public static int PromptInt(string label, int defaultValue)
        {
            Console.Write($"{label} [{defaultValue}]: ");
            var s = Console.ReadLine();
            return int.TryParse(s, out var v) ? v : defaultValue;
        }

        public static decimal PromptDecimal(string label, decimal defaultValue)
        {
            Console.Write($"{label} [{defaultValue}]: ");
            var s = Console.ReadLine();
            return decimal.TryParse(s, out var v) ? v : defaultValue;
        }

        public static Guid? PromptGuid(string label)
        {
            Console.Write(label);
            var s = Console.ReadLine();
            return Guid.TryParse(s, out var g) ? g : null;
        }

        public static void Pause(string msg = "Press ENTER to continue...")
        {
            Console.WriteLine(msg);
            Console.ReadLine();
        }

        public static IEnumerable<T> Page<T>(IEnumerable<T> source, int page, int pageSize)
            => source.Skip((page - 1) * pageSize).Take(pageSize);
    }
}

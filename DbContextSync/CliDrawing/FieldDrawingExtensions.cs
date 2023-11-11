using DbContextSync.Models;

namespace DbContextSync.CliDrawing
{
    public static class FieldDrawingExtensions
    {
        public static string LengthDisplay(this DataType type)
            => $"ℓ{type.MaxLength?.ToString() ?? "null"}";

        public static string KeyDisplay(this Field field)
            => field.PrimaryKey ? "KEY" : "";

        public static string UniqueDisplay(this Field field)
            => field.IsUnique ? "UNIQUE" : "";

        public static string RequiredDisplay(this Field field)
            => field.Required ? "NOT_NULL" : "NULL";
    }
}
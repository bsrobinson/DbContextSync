using System.Linq;
using CommandLine;
using DbContextSync.Enums;
using DbContextSync.Helpers;

namespace DbContextSync.Models
{
    public class Arguments
    {
        [Option('d', "database", Required = true, HelpText = "Database connection string")]
        public required ConnectionString ConnectionString { get; set; }

        [Option('c', "context", Required = true, HelpText = "Glob file path pattern to the DbContext class and the table class files")]
        public required string DbContextFileGlob { get; set; }

        [Option(Default = null, HelpText = "DBContext class name to use if multiple found")]
        public string? ContextClass { get; set; }

        [Option(Default = null, HelpText = "Usually derived from the connection string, but can be set here if required to [MySql]")]
        public DatabaseType? DatabaseType { get; set; } = null;

        [Option("count", Default = false, HelpText = "Return the only the change count only.")]
        public bool ChangeCountOnly { get; set; }

        [Option("preview", Default = false, HelpText = "Return the only the change preview.")]
        public bool PreviewOnly { get; set; }

        [Option(Default = null, HelpText = "Selects direction of copy without asking.  If provided, value must be [ToDbContext | ToDatabase | ToDatabaseWithDeletes]")]
        public Direction Direction { get; set; } = Direction.NoneSelected;

        [Option(Default = false, HelpText = "Start copy process without confirming.")]
        public bool NoConfirm { get; set; }

        [Option(Default = false, HelpText = "Action deletes in database without confrming.")]
        public bool NoConfirmDelete { get; set; }

        [Option("script", Default = false, HelpText = "Output database changes to a sql scripts, printed to screen unless --scriptfile is provided.")]
        public bool ScriptDatabaseChanges { get; set; }

        [Option("scriptfile", Default = null, HelpText = "File to save script to, instead of dumping to console")]
        public string? ScriptDatabaseChangesToFile { get; set; } = null;

        
    }

    public static class ArgumentExtensions
    {
        public static Arguments Parse(this string[] args)
        {
            ParserResult<Arguments> parsedArg = Parser.Default.ParseArguments<Arguments>(args);
            if (parsedArg.Errors.Count() == 0)
            {
                SetDatabaseType(parsedArg.Value);
                return parsedArg.Value;
            }

            throw ErrorState.ArgumentError.Kill();
        }

        public static void SetDatabaseType(Arguments args)
        {
            if (args.DatabaseType == null)
            {
                if (args.ConnectionString.Port == "3306") { args.DatabaseType = DatabaseType.MySql; }
            }

            if (args.DatabaseType == null)
            {
                throw ErrorState.UnknownDatabaseType.Kill(spaceBefore: true);
            }
        }
    }
}
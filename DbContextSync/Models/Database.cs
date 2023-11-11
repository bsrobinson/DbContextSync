using System.Collections.Generic;
using System.Data;
using System.Linq;
using DbContextSync.Extensions;
using DbContextSync.Helpers;
using Microsoft.CodeAnalysis.Text;

namespace DbContextSync.Models
{
    public class Database
    {
        public string Name { get; set; }
        public List<Table> Tables { get; set; } = new();

        public Database(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }

    public class DbContextDatabase : Database
    {
        public required string Path { get; set; }
        public required string Namespace { get; set; }
        public TextSpan CloseBraceSpan { get; set; }

        public DbContextDatabase(string name) : base(name) { }
    }

    public class PhysicalDatabase : Database
    {
        public bool DatabaseExists { get; set; } = true;

        public PhysicalDatabase(Arguments args, bool databaseExits = false) : base(args.ConnectionString.DatabaseName ?? "")
        {
            DatabaseExists = databaseExits;
        }

        public PhysicalDatabase(Arguments args, DataTable dataTable) : base(args.ConnectionString.DatabaseName ?? "")
        {
            foreach (DataRow row in dataTable.Rows)
            {
                string? tableName = row[0].ToString();
                if (tableName != null)
                {
                    Tables.Add(new DatabaseTable() { Name = tableName });
                }
            }
        }
    }

    public class MergedDatabase
    {
        public List<MergedTable> Tables { get; set; } = new();

        public DbContextDatabase DbContextDatabase { get; set; }
        public PhysicalDatabase PhysicalDatabase { get; set; }

        public MergedDatabase(DbContextDatabase dbContextDatabase, PhysicalDatabase physicalDatabase)
        {
            foreach (DbContextTable table in dbContextDatabase.Tables)
            {
                Tables.Add(new(table, MatchingTable<DatabaseTable>(physicalDatabase, table)));
            }
            foreach (DatabaseTable table in physicalDatabase.Tables)
            {
                if (Tables.FirstOrDefault(f => f.Name.EqualsNoCase(table.Name)) == null)
                {
                    MergedTable newTable = new(MatchingTable<DbContextTable>(dbContextDatabase, table), table);
                    int binarySearchIndex = Tables.BinarySearch(newTable);
                    Tables.Insert(binarySearchIndex < 0 ? ~binarySearchIndex : binarySearchIndex, newTable);
                }
            }

            DbContextDatabase = dbContextDatabase;
            PhysicalDatabase = physicalDatabase;
        }

        private T? MatchingTable<T>(Database database, Table table) where T : Table
        {
            return (T?)database.Tables.FirstOrDefault(f => f.Name.EqualsNoCase(table.Name));
        }
    }
}
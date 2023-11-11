using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DbContextSync.Extensions;
using Microsoft.CodeAnalysis.Text;
using MySqlX.XDevAPI.Relational;

namespace DbContextSync.Models
{
    public class Table
    {
        public required string Name { get; set; }
        public List<Field> Fields { get; set; } = new();

        public override string ToString() => Name;
    }

    public class DbContextTable : Table
    {
        public required string ClassName { get; set; }
        public string? ClassPath { get; set; } = null;
        public TextSpan ClassSpan { get; set; }
        public TextSpan ClassFullSpan { get; set; }
        public TextSpan PositionInContextClass { get; set; }
        public TextSpan FullPositionInContextClass { get; set; }
        public TextSpan? PrimryKeyAttributePosition { get; set; } = null;
    }

    public class DatabaseTable : Table
    {

    }

    public class MergedTable : IComparable
    {
        public string Name { get; set; }

        public List<MergedField> Fields { get; set; } = new();

        public DbContextTable? DbContextTable { get; set; }
        public DatabaseTable? DatabaseTable { get; set; }

        public bool Differs => DbContextTable == null || DatabaseTable == null;
        [MemberNotNullWhen(true, new string[] { nameof(DbContextTable), nameof(DatabaseTable) })]
        public bool SameInBoth => DbContextTable != null && DatabaseTable != null;
        [MemberNotNullWhen(true, nameof(DbContextTable))]
        public bool DbContextOnly => DbContextTable != null && DatabaseTable == null;
        [MemberNotNullWhen(true, nameof(DatabaseTable))]
        public bool DatabaseOnly => DbContextTable == null && DatabaseTable != null;

        public MergedTable(DbContextTable? dbContextTable, DatabaseTable? databaseTable)
        {
            Name = dbContextTable?.Name ?? databaseTable?.Name ?? "error";

            foreach (DbContextField field in dbContextTable?.Fields ?? new())
            {
                Fields.Add(new(field, MatchingField<DatabaseField>(databaseTable, field)));
            }
            if (databaseTable != null)
            {
                int previousMatchIndex = -1;
                for (int i = 0; i < databaseTable.Fields.Count; i++)
                {
                    DatabaseField field = (DatabaseField)databaseTable.Fields[i];
                    if (Fields.FirstOrDefault(f => f.Name.EqualsNoCase(field.Name)) == null)
                    {
                        int insertIndex = previousMatchIndex + 1;
                        if (insertIndex > Fields.Count) { insertIndex = Fields.Count; }
                        Fields.Insert(insertIndex, new(MatchingField<DbContextField>(dbContextTable, field), field));
                        if (insertIndex >= 0) { previousMatchIndex++; }
                    }
                    else
                    {
                        previousMatchIndex = i;
                    }
                }
            }

            DbContextTable = dbContextTable;
            DatabaseTable = databaseTable;
        }

        private T? MatchingField<T>(Table? table, Field field) where T : Field
        {
            return (T?)table?.Fields?.FirstOrDefault(f => f.Name.EqualsNoCase(field.Name));
        }

        public override string ToString() => Name;

        public int CompareTo(object? obj)
        {
            return string.Compare(Name.ToLower(), ((MergedTable?)obj)?.Name.ToLower());
        }
    }
}
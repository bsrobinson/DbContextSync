using System;
using System.Diagnostics.CodeAnalysis;
using DbContextSync.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace DbContextSync.Models
{
    public class Field
    {
        private bool _required;

        public required string Name { get; set; }
        public required DataType Type { get; set; }
        public required bool PrimaryKey { get; set; }
        public required bool IsUnique { get; set; }
        public required bool Required { get => PrimaryKey ? true : _required; set { _required = value; } }

        public bool SameAs(Field field)
        {
            return Name.EqualsNoCase(field.Name)
                && Type.SameAs(field.Type)
                && PrimaryKey == field.PrimaryKey
                && IsUnique == field.IsUnique
                && Required == field.Required;
        }

        public override string ToString() => Name;
    }

    public class DbContextField : Field
    {
        public required TextSpan PositionInClass { get; set; }
        public required TextSpan FullPositionInClass { get; set; }
        public TextSpan? IndexAttributePosition { get; set; } = null;
    }

    public class DatabaseField : Field
    {
        public required bool AutoIncrement { get; set; }
    }

    public class MergedField : IComparable
    {
        public string Name { get; set; }

        public DbContextField? DbContextField { get; set; }
        public DatabaseField? DatabaseField { get; set; }

        public bool PropertiesSame => DbContextField != null && DatabaseField != null && DbContextField.SameAs(DatabaseField);
        public bool Differs => DbContextField == null || DatabaseField == null || !PropertiesSame;
        [MemberNotNullWhen(true, new string[] { nameof(DbContextField), nameof(DatabaseField) })]
        public bool ExistsInBoth => DbContextField != null && DatabaseField != null;
        [MemberNotNullWhen(true, nameof(DbContextField))]
        public bool DbContextOnly => DbContextField != null && DatabaseField == null;
        [MemberNotNullWhen(true, nameof(DatabaseField))]
        public bool DatabaseOnly => DbContextField == null && DatabaseField != null;

        public MergedField(DbContextField? dbContextField, DatabaseField? databaseField)
        {
            Name = dbContextField?.Name ?? databaseField?.Name ?? "error";

            DbContextField = dbContextField;
            DatabaseField = databaseField;
        }

        public override string ToString() => Name;

        public int CompareTo(object? obj)
        {
            return string.Compare(Name.ToLower(), ((MergedField?)obj)?.Name.ToLower());
        }
    }
}


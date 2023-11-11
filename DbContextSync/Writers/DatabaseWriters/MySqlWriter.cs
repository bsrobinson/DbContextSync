using System.Collections.Generic;
using System.Data;
using System.Linq;
using DbContextSync.Extensions;
using DbContextSync.Helpers;
using DbContextSync.Models;
using MySql.Data.MySqlClient;

namespace DbContextSync.Writers.DatabaseWriters
{
    internal class MySqlWriter : IDatabaseWriter
    {
        public List<string> Scripts { get; set; } = new();

        public void BuildUseCreateScript(string databaseName, bool exists)
        {
            if (!exists)
            {
                Scripts.Add($"CREATE DATABASE {databaseName}");
            }
            Scripts.Add($"USE {databaseName}");
        }

        public void BuildAddTableScript(DbContextTable table)
        {
            string? autoIncField = null;
            if (table.Fields.Count(f => f.PrimaryKey) == 1) { autoIncField = table.Fields.FirstOrDefault(f => f.PrimaryKey)?.Name; }
            Scripts.Add($"CREATE TABLE {FormattedTableName(table)} ({string.Join(", ", table.Fields.Select(f => ColumnDefination(f, f.Name == autoIncField)))}{PrimaryKeyDefination(table)}{UniqueDefination(table)})");
        }

        public void BuildDeleteTableScript(DatabaseTable table)
        {
            Scripts.Add($"DROP TABLE {FormattedTableName(table)}");
        }

        public void BuildAddFieldScript(MergedTable table, DbContextField field, int addAfterIndex)
        {
            Scripts.Add($"ALTER TABLE {FormattedTableName(table)} ADD COLUMN {ColumnDefination(field)} {(addAfterIndex < 0 ? "FIRST" : $"AFTER {FormattedFieldName(table.Fields[addAfterIndex])}")}");
        }

        public void BuildDeleteFieldScript(DatabaseTable table, DatabaseField field)
        {
            Scripts.Add($"ALTER TABLE {FormattedTableName(table)} DROP COLUMN {FormattedFieldName(field)}");
        }

        public void BuildAlterFieldScript(DatabaseTable table, DbContextField field)
        {
            Scripts.Add($"ALTER TABLE {FormattedTableName(table)} MODIFY COLUMN {ColumnDefination(field)}");
        }



        public void BuildAlterPrimaryKeyScript(MergedTable table, IEnumerable<DbContextField> keysToAdd, IEnumerable<DatabaseField> keysToDrop)
        {
            if (keysToAdd.Count() > 0 || keysToDrop.Count() > 0)
            {
                Scripts.Add($"ALTER TABLE {FormattedTableName(table)} DROP PRIMARY KEY{PrimaryKeyDefination(keysToAdd.Cast<Field>().ToList(), add: true)}");
                if (keysToAdd.Count() == 1)
                {
                    Scripts.Add($"ALTER TABLE {FormattedTableName(table)} MODIFY COLUMN {ColumnDefination(keysToAdd.First(), autoIncrement: true)}");
                }
            }
        }

        public void BuildAlterUniqueScript(MergedTable table, IEnumerable<DbContextField> indexesToAdd, IEnumerable<DatabaseField> indexesToDrop)
        {
            if (indexesToAdd.Count() > 0)
            {
                Scripts.Add($"ALTER TABLE {FormattedTableName(table)} ADD UNIQUE INDEX ({string.Join(", ", indexesToAdd.Select(FormattedFieldName))})");
            }
            foreach (Field field in indexesToDrop)
            {
                Scripts.Add($"DROP INDEX {FormattedFieldName(field)} ON {FormattedTableName(table)}");
            }
        }

        public void Run(Arguments args)
        {
            MySqlConnection connection = new(args.ConnectionString.ToStringWithoutDatabase());
            connection.Open();

            foreach (string sql in Scripts)
            {
                ExecuteNonQuery(connection, sql);
            }

            connection.Close();
        }

        private string FormattedTableName(MergedTable table) => FormattedName(table.Name);
        private string FormattedTableName(Table table) => FormattedName(table.Name);
        private string FormattedFieldName(MergedField field) => FormattedName(field.Name);
        private string FormattedFieldName(Field field) => FormattedName(field.Name);
        private string FormattedName(string name) => name.ToCamelCase().Quote('`');

        private string ColumnDefination(Field field)
        {
            return ColumnDefination(field, false);
        }
        private string ColumnDefination(Field field, bool autoIncrement)
        {
            if (field.Type.Name.EqualsNoCase("string")) { autoIncrement = false; }
            return $"{FormattedFieldName(field)} {ConvertDataType(field.Type)} {(field.Required ? "NOT NULL" : "NULL")}{(autoIncrement ? " AUTO_INCREMENT" : "")}";
        }

        private string PrimaryKeyDefination(Table table)
        {
            List<Field> primaryKeys = table.Fields.Where(f => f.PrimaryKey).ToList();
            return PrimaryKeyDefination(primaryKeys, add: false);
        }

        private string PrimaryKeyDefination(List<Field> fields, bool add)
        {
            return fields.Count() > 0 ? $",{(add ? " ADD" : "")} PRIMARY KEY ({string.Join(", ", fields.Select(FormattedFieldName))})" : "";
        }

        private string UniqueDefination(Table table)
        {
            List<Field> uniqueFields = table.Fields.Where(f => f.IsUnique).ToList();
            return uniqueFields.Count() > 0 ? $", UNIQUE ({string.Join(", ", uniqueFields.Select(FormattedFieldName))})" : "";
        }

        private string ConvertDataType(DataType dataType)
        {
            switch (dataType.Name.ToLower())
            {
                case "string":
                    if (dataType.MaxLength == null)
                    {
                        return $"LONGTEXT";
                    }
                    else
                    {
                        return $"VARCHAR{DataTypeLength(dataType, 255)}";
                    }
                case "bool":
                    return "TINYINT(1)";
                case "short":
                    return $"TINYINT{DataTypeLength(dataType, 4)}";
                case "ushort":
                    return $"TINYINT{DataTypeLength(dataType, 4)} UNSIGNED";
                case "int":
                    return $"INT{DataTypeLength(dataType, 11)}";
                case "uint":
                    return $"INT{DataTypeLength(dataType, 11)} UNSIGNED";
                case "long":
                    return $"BIGINT{DataTypeLength(dataType, 20)}";
                case "ulong":
                    return $"BIGINT{DataTypeLength(dataType, 20)} UNSIGNED";
                case "decimal":
                    return $"DEC{DataTypeLength(dataType, 10)}";
                case "datetime":
                    return "DATETIME";
                case "dateonly":
                    return "DATE";
                case "timeonly":
                    return "TIME";
            }

            throw ErrorState.CannotConvertDataTyoe.Kill(dataType.Name);
        }

        private string DataTypeLength(DataType dataType, int defaultValue)
        {
            return $"({dataType.MaxLength ?? defaultValue})";
        }

        private void ExecuteNonQuery(MySqlConnection connection, string sql)
        {
            MySqlCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            command.ExecuteNonQuery();
        }
    }
}
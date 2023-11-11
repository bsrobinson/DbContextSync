using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DbContextSync.Extensions;
using DbContextSync.Models;
using Microsoft.CodeAnalysis.Text;

namespace DbContextSync.Writers
{
    public class DbContextWriter
    {
        public void Run(CompareDatabases compare, Arguments args)
        {
            foreach (MergedTable table in compare.Merged.Tables.Reverse<MergedTable>())
            {
                string dbContextFileContent = File.ReadAllText(compare.Merged.DbContextDatabase.Path);

                if (table.DatabaseOnly)
                {
                    //Add table
                    string tableName = table.DatabaseTable.Name.ToProperCase();
                    string className = ClassName(tableName);

                    int thisTableIndex = compare.Merged.Tables.FindIndex(t => t.Name.EqualsNoCase(table.Name));
                    int previousTableIndex = compare.Merged.Tables.FindLastIndex(thisTableIndex, t => !t.DatabaseOnly);

                    int insertPosition = previousTableIndex >= 0 ? compare.Merged.Tables[previousTableIndex].DbContextTable?.PositionInContextClass.End ?? 0 : 0;
                    string tableProperty = $"\n        public virtual DbSet<{className}> {tableName} {{ get; set; }}";

                    if (insertPosition == 0)
                    {
                        if (compare.Merged.DbContextDatabase.Tables.Count > 0)
                        {
                            DbContextTable firstTable = compare.Merged.DbContextDatabase.Tables.Cast<DbContextTable>().OrderBy(t => t.PositionInContextClass.Start).First();
                            insertPosition = firstTable.PositionInContextClass.Start;
                            tableProperty = $"{tableProperty.Trim()}\n        ";
                        }
                        else
                        {
                            insertPosition = compare.Merged.DbContextDatabase.CloseBraceSpan.Start;
                        }
                    }

                    dbContextFileContent = dbContextFileContent.Insert(insertPosition, tableProperty);

                    string filePath = NewClassFilePath(compare.Merged.DbContextDatabase.Path, className);
                    File.WriteAllText(filePath, TableClass(table.DatabaseTable, className));
                }
                else if (table.DbContextOnly)
                {
                    //Delete table
                    TextSpan span = table.DbContextTable.PositionInContextClass;
                    int end = span.End;
                    if (dbContextFileContent[span.End..(span.End + 3)] == "\n") { end = span.End + 1; }
                    if (dbContextFileContent[span.End..(span.End + 3)] == "\n\t\t") { end = span.End + 3; }
                    if (dbContextFileContent[span.End..(span.End + 9)] == "\n        ") { end = span.End + 9; }
                    dbContextFileContent = dbContextFileContent.ReplaceInString(span.Start, end, "");

                    if (table.DbContextTable.ClassPath != null)
                    {
                        if (compare.ClassesAtPath(table.DbContextTable.ClassPath).Count > 1)
                        {
                            string fileContent = File.ReadAllText(table.DbContextTable.ClassPath);
                            File.WriteAllText(table.DbContextTable.ClassPath, fileContent.ReplaceInString(table.DbContextTable.ClassFullSpan, ""));
                        }
                        else
                        {
                            File.Delete(table.DbContextTable.ClassPath);
                        }
                    }
                }
                else
                {
                    if (table.DbContextTable?.ClassPath != null)
                    {
                        bool classInContextFile = table.DbContextTable.ClassPath == compare.Merged.DbContextDatabase.Path;

                        string classFileContent = classInContextFile ? dbContextFileContent : File.ReadAllText(table.DbContextTable.ClassPath);

                        foreach (MergedField field in table.Fields.Reverse<MergedField>())
                        {
                            if (field.DatabaseOnly)
                            {
                                //Insert field
                                int thisFieldIndex = table.Fields.FindIndex(f => f.Name.EqualsNoCase(field.Name));
                                int previousFieldIndex = table.Fields.FindLastIndex(thisFieldIndex, f => !f.DatabaseOnly);

                                int insertPosition = previousFieldIndex >= 0 ? table.Fields[previousFieldIndex].DbContextField?.PositionInClass.End ?? 0 : 0;
                                string fieldProperty = $"\n\n        {FieldProperty(field.DatabaseField).Trim()}";

                                if (insertPosition == 0)
                                {
                                    insertPosition = table.DbContextTable.ClassSpan.Start;
                                    fieldProperty = table.DbContextTable.Fields.Count == 0 ? $"        {fieldProperty.Trim()}\n\n" : $"{fieldProperty.Trim()}\n\n        ";
                                }

                                classFileContent = classFileContent.Insert(insertPosition, fieldProperty);
                            }
                            if (field.DbContextOnly)
                            {
                                //Delete field
                                classFileContent = classFileContent.ReplaceInString(field.DbContextField.FullPositionInClass, "");
                            }
                            else if (field.ExistsInBoth && !field.PropertiesSame)
                            {
                                //Update field
                                classFileContent = classFileContent.ReplaceInString(field.DbContextField.PositionInClass, FieldProperty(field.DatabaseField).Trim());
                            }
                        }

                        foreach (MergedField field in table.Fields.OrderByDescending(f => f.DbContextField?.IndexAttributePosition?.Start ?? 0))
                        {
                            if ((field.DbContextField == null || !field.DbContextField.IsUnique) && field.DatabaseField?.IsUnique == true)
                            {
                                //add unique index
                                classFileContent = classFileContent.Insert(table.DbContextTable.ClassFullSpan.Start, UniqueAttribute(field.DatabaseField));
                            }
                            if (field.DbContextField?.IsUnique == true && (field.DatabaseField == null || !field.DatabaseField.IsUnique) && field.DbContextField.IndexAttributePosition != null)
                            {
                                //remove unique index
                                classFileContent = classFileContent.ReplaceInString((TextSpan)field.DbContextField.IndexAttributePosition, "");
                            }
                        }

                        IEnumerable<MergedField> differingKeys = table.Fields.Where(f => f.DbContextField?.PrimaryKey != f.DatabaseField?.PrimaryKey);
                        if (differingKeys.Count() > 0 && table.DatabaseTable != null)
                        {
                            string primaryKeyAttribute = PrimaryKeyAttribute(table.DatabaseTable);
                            if (table.DbContextTable.PrimryKeyAttributePosition == null)
                            {
                                classFileContent = classFileContent.Insert(table.DbContextTable.ClassFullSpan.Start, primaryKeyAttribute);
                            }
                            else
                            {
                                TextSpan span = (TextSpan)table.DbContextTable.PrimryKeyAttributePosition;
                                if (primaryKeyAttribute.Length > 0) { primaryKeyAttribute = primaryKeyAttribute.Trim()[1..^1]; }
                                classFileContent = classFileContent.ReplaceInString(span, primaryKeyAttribute);
                            }
                        }

                        classFileContent = classFileContent.Replace("\n    []", "");
                        if (classInContextFile)
                        {
                            dbContextFileContent = classFileContent;
                        }
                        else
                        {
                            File.WriteAllText(table.DbContextTable.ClassPath, classFileContent);
                        }
                    }
                    else if (table.DatabaseTable != null)
                    {
                        //Add missing class file
                        string className = ClassName(table.DatabaseTable.Name.ToProperCase());
                        string filePath = NewClassFilePath(compare.Merged.DbContextDatabase.Path, className);
                        File.WriteAllText(filePath, TableClass(table.DatabaseTable, className));
                    }
                }

                File.WriteAllText(compare.Merged.DbContextDatabase.Path, dbContextFileContent);
            }
        }

        public string TableClass(DatabaseTable table, string className)
        {
            List<string> lines = new()
            {
                $"using System;",
                $"using System.ComponentModel.DataAnnotations;",
                $"using Microsoft.EntityFrameworkCore;",
                $"",

                $"namespace MediaLife.Library.DAL",
                $"{{",
                $"    {PrimaryKeyAttribute(table).Trim()}"
            };
            foreach (DatabaseField field in table.Fields.Where(f => f.IsUnique))
            {
                lines.Add($"    {UniqueAttribute(field).Trim()}");
            }
            lines.AddRange(new List<string>() {
                $"    public class {className}",
                $"    {{",
            });

            foreach (DatabaseField field in table.Fields)
            {
                lines.Add(FieldProperty(field));
            }

            lines.AddRange(new List<string>()
            {
                "    }",
                "}"
            });

            return string.Join("\n", lines);
        }

        private string FieldProperty(DatabaseField field)
        {
            string s = "";
            if (field.Type.MaxLength != null) { s += $"        [MaxLength({field.Type.MaxLength})]\n"; }

            s += $"        public {(field.Required && !field.AutoIncrement ? "required " : "")}{field.Type.Name}{(field.Required ? "" : "?")} {field.Name.ToProperCase()} {{ get; set; }}\n";

            return s;
        }

        private string PrimaryKeyAttribute(DatabaseTable table)
        {
            IEnumerable<DatabaseField> databaseKeys = table.Fields.Where(f => f.PrimaryKey == true).Select(f => f).Cast<DatabaseField>();
            if (databaseKeys.Count() > 0)
            {
                return $"    [PrimaryKey({string.Join(", ", databaseKeys.Select(k => $"nameof({k.Name.ToProperCase()})"))})]\n";
            }
            return "";
        }

        private string UniqueAttribute(DatabaseField field)
        {
            return $"    [Index(nameof({field.Name.ToProperCase()}), IsUnique = true)]\n";
        }

        public string NewClassFilePath(string contextPath, string className)
        {
            string? filePath = $"{Path.GetDirectoryName(contextPath) ?? ""}/{{0}}{{1}}.cs";
            int copy = 0;

            while (File.Exists(string.Format(filePath, className, copy == 0 ? "" : $"_{copy}")))
            {
                copy++;
            }

            return string.Format(filePath, className, copy == 0 ? "" : $"_{copy}");
        }

        public string ClassName(string tableName)
        {
            string className = tableName;
            if (className.EndsWith("ies")) { className = $"{className[..^3]}y"; }
            if (className.EndsWith("s")) { className = className[..^1]; }
            return className;
        }
    }
}
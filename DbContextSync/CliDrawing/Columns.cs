using DbContextSync.Enums;
using DbContextSync.Extensions;
using DbContextSync.Helpers;
using DbContextSync.Models;
using PastelExtended;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbContextSync.CliDrawing
{
    public class Columns
    {
        public string Left { get; set; }
        public string Right { get; set; }

        public bool Header { get; set; }
        public bool? PointLeft { get; set; }

        public int MaxColumnWidth => Math.Max(Left.ConsoleCharacters(), Right.ConsoleCharacters());

        public Columns(string left, string right, bool? pointLeft = null, bool header = false)
        {
            Left = left;
            Right = right;
            Header = header;
            PointLeft = pointLeft;
        }
    }

    public static class ColumnsExtensions
    {
        public static void Draw(this List<Columns> columns, int columnWidth, bool replace = false)
        {
             if (columnWidth + 3 > Console.WindowWidth / 2)
            {
                throw ErrorState.WindowTooNarrow.Kill($"{(columnWidth + 3) * 2}");
            }

            if (replace)
            {
                int top = Console.CursorTop - columns.Count - 2;
                if (top > 0)
                {
                    Console.SetCursorPosition(0, top);
                }
            }

            Console.WriteLine(new string('─', columnWidth) + "─┬──┬─" + new string('─', columnWidth));
            foreach (Columns column in columns)
            {
                string center = $"│{(column.Header ? "<>" : "  ")}│";
                if (column.PointLeft != null)
                {
                    if (column.Header)
                    {
                        center = column.PointLeft == true ? "<<--" : "-->>";
                    }
                    else
                    {
                        center = "│" + (column.PointLeft == true ? "<-" : "->") + "│";
                    }
                }
                Console.WriteLine($"{column.Left.Pad(columnWidth)} {center} {column.Right.Pad(columnWidth)}");
            }
            Console.WriteLine(new string('─', columnWidth) + "─┴──┴─" + new string('─', columnWidth));
        }

        public static List<Columns> PrintableDifferences(this Direction direction, CompareDatabases compare, DatabaseType? databaseType)
        {
            List<Columns> columns = new();
            bool? copyToDatabase = direction == Direction.NoneSelected ? null : (direction == Direction.ToDatabase || direction == Direction.ToDatabaseWithDeletes);
            bool withDeletes = direction == Direction.ToDatabaseWithDeletes;

            List<MergedTable> tables = compare.Merged.Tables.Where(t => t.Differs || t.Fields.Any(f => f.Differs)).ToList();
            if (tables.Count > 0)
            {
                columns.Add(new($"DBContext ({compare.Merged.DbContextDatabase.Name})", $"{databaseType} ({compare.Merged.PhysicalDatabase.Name})", !copyToDatabase, true));

                for (int i = 0; i < tables.Count; i++)
                {
                    MergedTable table = tables[i];
                    Table? dbContextTable = table.DbContextTable != null ? compare.Merged.DbContextDatabase.Tables.First(t => t.Name.EqualsNoCase(table.Name)) : null;
                    Table? databaseTable = table.DatabaseTable != null ? compare.Merged.PhysicalDatabase.Tables.First(t => t.Name.EqualsNoCase(table.Name)) : null;
                    bool lastTable = i == tables.Count - 1;

                    bool? tableToLeft = table.Differs ? !copyToDatabase : null;
                    if (tableToLeft == false && !withDeletes && table.DatabaseOnly) { tableToLeft = null; }
                    columns.Add(new($"{LeftTableEdge(lastTable)} {MissingName(dbContextTable?.Name, databaseTable?.Name, !copyToDatabase)}", $"{LeftTableEdge(lastTable)} {MissingName(databaseTable?.Name, dbContextTable?.Name, copyToDatabase, withDeletes)}", tableToLeft));

                    if (table.SameInBoth)
                    {
                        List<MergedField> differingFields = table.Fields.Where(f => f.Differs).ToList();
                        for (int j = 0; j < differingFields.Count; j++)
                        {
                            MergedField field = differingFields[j];

                            Field? dbContextField = field.DbContextField != null ? dbContextTable?.Fields.First(f => f.Name.EqualsNoCase(field.Name)) : null;
                            Field? databaseField = field.DatabaseField != null ? databaseTable?.Fields.First(f => f.Name.EqualsNoCase(field.Name)) : null;
                            bool lastField = j == differingFields.Count - 1;

                            string dbContextFieldName = MissingName(dbContextField?.Name, databaseField?.Name, !copyToDatabase)
                                + FieldPropertyDiffers(dbContextField?.Type.Name, databaseField?.Type.Name, !copyToDatabase)
                                + FieldPropertyDiffers(dbContextField?.Type.LengthDisplay(), databaseField?.Type.LengthDisplay(), !copyToDatabase)
                                + FieldPropertyDiffers(dbContextField?.KeyDisplay(), databaseField?.KeyDisplay(), !copyToDatabase)
                                + FieldPropertyDiffers(dbContextField?.UniqueDisplay(), databaseField?.UniqueDisplay(), !copyToDatabase)
                                + FieldPropertyDiffers(dbContextField?.RequiredDisplay(), databaseField?.RequiredDisplay(), !copyToDatabase);

                            string databaseFieldName = MissingName(databaseField?.Name, dbContextField?.Name, copyToDatabase, withDeletes)
                                + FieldPropertyDiffers(databaseField?.Type.Name, dbContextField?.Type.Name, copyToDatabase)
                                + FieldPropertyDiffers(databaseField?.Type.LengthDisplay(), dbContextField?.Type.LengthDisplay(), copyToDatabase)
                                + FieldPropertyDiffers(databaseField?.KeyDisplay(), dbContextField?.KeyDisplay(), copyToDatabase)
                                + FieldPropertyDiffers(databaseField?.UniqueDisplay(), dbContextField?.UniqueDisplay(), copyToDatabase)
                                + FieldPropertyDiffers(databaseField?.RequiredDisplay(), dbContextField?.RequiredDisplay(), copyToDatabase);

                            bool? fieldToLeft = !copyToDatabase;
                            if (fieldToLeft == false && !withDeletes && field.DatabaseOnly) { fieldToLeft = null; }
                            columns.Add(new($"{LeftFieldEdge(lastTable, lastField)} {dbContextFieldName}", $"{LeftFieldEdge(lastTable, lastField)} {databaseFieldName}", fieldToLeft));
                        }
                    }
                }
            }
            return columns;
        }

        private static string MissingName(string? name, string? otherName, bool? copyToName, bool includeDeletes = true)
        {
            string s = name ?? "missing".Deco(Decoration.Italic).Fg(ConsoleColor.DarkYellow);
            if (copyToName == true)
            {
                if (otherName == null)
                {
                    s = includeDeletes ? name?.Deco(Decoration.Strikethrough).Fg(ConsoleColor.Red) ?? "" : name ?? "";
                }
                else
                {
                    s = name == null ? otherName?.Fg(ConsoleColor.Green) ?? "" : name;
                }
            }
            if (copyToName == false)
            {
                s = name ?? "";
            }

            return s;
        }

        private static string FieldPropertyDiffers(string? property, string? otherProperty, bool? copyToProperty)
        {
            string s = "";
            if (property != null && otherProperty != null)
            {
                if (property != otherProperty)
                {
                    if (copyToProperty == null)
                    {
                        s = $" {property.Deco(Decoration.Italic).Fg(ConsoleColor.DarkYellow)}";
                    }
                    if (copyToProperty == true)
                    {
                        s = $" {property.Deco(Decoration.Strikethrough).Fg(ConsoleColor.Red)}{otherProperty.Fg(ConsoleColor.Green)}";
                    }
                }
            }
            return s;
        }


        private static char LeftTableEdge(bool isLast)
        {
            return isLast ? '┗' : '┣';
        }

        private static string LeftFieldEdge(bool isLastTable, bool isLastField)
        {
            return $"{(isLastTable ? ' ' : '┃')} {(isLastField ? '┗' : '┣')}";
        }
    }
}
using System.Collections.Generic;
using DbContextSync.Models;
using DbContextSync.Writers.DatabaseWriters;
using System;
using System.IO;
using DbContextSync.Prompts;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DbContextSync.Extensions;
using DbContextSync.Enums;
using Microsoft.CodeAnalysis.Text;
using CommandLine;

namespace DbContextSync.Writers
{
    public interface IDatabaseWriter
    {
        public abstract List<string> Scripts { get; set; }

        public string ScriptsBlock => string.Join(";\n", Scripts) + ";";

        public abstract void BuildUseCreateScript(string databaseName, bool exists);
        public abstract void BuildAddTableScript(DbContextTable table);
        public abstract void BuildDeleteTableScript(DatabaseTable table);
        public abstract void BuildAddFieldScript(MergedTable table, DbContextField field, int addAfterIndex);
        public abstract void BuildDeleteFieldScript(DatabaseTable table, DatabaseField field);
        public abstract void BuildAlterFieldScript(DatabaseTable table, DbContextField field);
        public abstract void BuildAlterPrimaryKeyScript(MergedTable table, IEnumerable<DbContextField> keysToAdd, IEnumerable<DatabaseField> keysToDrop);
        public abstract void BuildAlterUniqueScript(MergedTable table, IEnumerable<DbContextField> indexesToAdd, IEnumerable<DatabaseField> indexesToDrop);
        public abstract void Run(Arguments args);
    }

    public static class DatabaseWriterExtensions
    {
        public static void Write(this CompareDatabases compare, Arguments args)
        {
            if (args.Direction == Direction.ToDbContext)
            {
                ConfirmPrompt prompt = new("Start copying?");
                if (args.NoConfirm) { prompt.AutoSelectValue = true; }
                if (prompt.Ask())
                {
                    new DbContextWriter().Run(compare, args);
                }
            }
            else
            {
                string question = compare.Merged.PhysicalDatabase.DatabaseExists
                    ? "Ready to start copying the above changes? (you may not be able to undo this)"
                    : "Database does not exist, create and populate?";

                SelectPrompt<ConfirmCopyOption> prompt = new(question);
                if (args.NoConfirm) { prompt.AutoSelectValue = ConfirmCopyOption.Copy; }
                if (args.ScriptDatabaseChanges) { prompt.AutoSelectValue = ConfirmCopyOption.Scripts; }
                if (!compare.Merged.PhysicalDatabase.DatabaseExists) { prompt.SpaceBefore = true; }
                ConfirmCopyOption confirmOption = prompt.Ask();

                if (confirmOption == ConfirmCopyOption.ConfirmScripts)
                {
                    Console.WriteLine(compare.BuildScripts(args).ScriptsBlock);
                    confirmOption = new ConfirmPrompt("Start copying?") { SpaceBefore = true }.Ask()
                        ? ConfirmCopyOption.Copy
                        : ConfirmCopyOption.Cancel;
                }

                if (confirmOption != ConfirmCopyOption.Cancel)
                {
                    ConfirmPrompt confirmDeletePrompt = new("You are deleting database objects, this may cause un-retrivable data loss.  Are you sure?");
                    if (args.NoConfirmDelete) { confirmDeletePrompt.AutoSelectValue = true; }

                    if (args.Direction == Direction.ToDatabase || confirmOption == ConfirmCopyOption.Scripts || confirmDeletePrompt.Ask())
                    {
                        if (confirmOption == ConfirmCopyOption.Scripts && (!args.ScriptDatabaseChanges || args.ScriptDatabaseChangesToFile != null))
                        {
                            InputPrompt scriptPrompt = new("Enter path to save sql scripts");
                            scriptPrompt.AutoSelectValue = args.ScriptDatabaseChangesToFile;
                            if (scriptPrompt.AutoSelectValue.IsNull) { scriptPrompt.Question += " (or leave blank to output below)"; }
                            args.ScriptDatabaseChangesToFile = scriptPrompt.Ask();
                        }

                        IDatabaseWriter writer = compare.BuildScripts(args);
                        if (confirmOption == ConfirmCopyOption.Scripts || args.ScriptDatabaseChanges)
                        {
                            if (string.IsNullOrEmpty(args.ScriptDatabaseChangesToFile))
                            {
                                Console.WriteLine(writer.ScriptsBlock);
                            }
                            else
                            {
                                FileInfo file = new FileInfo(args.ScriptDatabaseChangesToFile);
                                file.Directory?.Create();
                                File.WriteAllText(args.ScriptDatabaseChangesToFile, writer.ScriptsBlock);
                            }
                        }
                        else
                        {
                            writer.Run(args);
                        }
                    }
                }
            }
        }

        private static IDatabaseWriter BuildScripts(this CompareDatabases compare, Arguments args)
        {
            IDatabaseWriter databaseWriter = GetDatabaseWriter(args);

            databaseWriter.BuildUseCreateScript(compare.Merged.PhysicalDatabase.Name, compare.Merged.PhysicalDatabase.DatabaseExists);

            foreach (MergedTable table in compare.Merged.Tables)
            {
                if (table.DbContextOnly)
                {
                    databaseWriter.BuildAddTableScript(table.DbContextTable);
                }
                else if (table.DatabaseOnly && args.Direction == Direction.ToDatabaseWithDeletes)
                {
                    databaseWriter.BuildDeleteTableScript(table.DatabaseTable);
                }
                else if (table.DbContextTable != null && table.DatabaseTable != null)
                {
                    foreach (MergedField field in table.Fields)
                    {
                        if (field.DbContextOnly)
                        {
                            int thisFieldIndex = table.Fields.FindIndex(f => f.Name.EqualsNoCase(field.Name));
                            databaseWriter.BuildAddFieldScript(table, field.DbContextField, thisFieldIndex - 1);
                        }
                        if (field.DatabaseOnly && args.Direction == Direction.ToDatabaseWithDeletes)
                        {
                            databaseWriter.BuildDeleteFieldScript(table.DatabaseTable, field.DatabaseField);
                        }
                        if (field.ExistsInBoth && !field.PropertiesSame && field.DbContextField != null)
                        {
                            databaseWriter.BuildAlterFieldScript(table.DatabaseTable, field.DbContextField);
                        }
                    }

                    //IEnumerable<DbContextField> allPrimaryKeys = table.Fields.Where(f => f.DbContextField?.PrimaryKey == true).Select(f => f.DbContextField).Cast<DbContextField>();
                    //IEnumerable<DbContextField> primaryKeysToAdd = table.Fields.Where(f => f.DbContextField?.IsUnique == true && (f.DatabaseField == null || !f.DatabaseField.IsUnique)).Select(f => f.DbContextField).Cast<DbContextField>();
                    //IEnumerable<DatabaseField> primaryKeysToDrop = table.Fields.Where(f => (f.DbContextField == null || !f.DbContextField.IsUnique) && f.DatabaseField?.IsUnique == true).Select(f => f.DatabaseField).Cast<DatabaseField>();




                    //IEnumerable<MergedField> differingKeys = table.Fields.Where(f => f.DbContextField?.PrimaryKey != f.DatabaseField?.PrimaryKey);
                    //if (differingKeys.Count() > 0)
                    //{
                    //    databaseWriter.BuildAlterPrimaryKeyScript(table.DatabaseTable, table.Fields.Where(f => f.DbContextField?.PrimaryKey == true).Select(f => f.DbContextField).Cast<DbContextField>());
                    //}

                    IEnumerable<DbContextField> primaryKeysToAdd = table.Fields.Where(f => f.DbContextField?.PrimaryKey == true && (f.DatabaseField == null || !f.DatabaseField.PrimaryKey)).Select(f => f.DbContextField).Cast<DbContextField>();
                    IEnumerable<DatabaseField> primaryKeysToDrop = table.Fields.Where(f => (f.DbContextField == null || !f.DbContextField.PrimaryKey) && f.DatabaseField?.PrimaryKey == true).Select(f => f.DatabaseField).Cast<DatabaseField>();
                    databaseWriter.BuildAlterPrimaryKeyScript(table, primaryKeysToAdd, primaryKeysToDrop);


                    IEnumerable<DbContextField> fieldIndexesToAdd = table.Fields.Where(f => f.DbContextField?.IsUnique == true && (f.DatabaseField == null || !f.DatabaseField.IsUnique)).Select(f => f.DbContextField).Cast<DbContextField>();
                    IEnumerable<DatabaseField> fieldIndexesToDrop = table.Fields.Where(f => (f.DbContextField == null || !f.DbContextField.IsUnique) && f.DatabaseField?.IsUnique == true).Select(f => f.DatabaseField).Cast<DatabaseField>();
                    databaseWriter.BuildAlterUniqueScript(table, fieldIndexesToAdd, fieldIndexesToDrop);
                }
            }

            return databaseWriter;
        }

        public static IDatabaseWriter GetDatabaseWriter(Arguments args)
        {
            switch (args.DatabaseType)
            {
                case DatabaseType.MySql: return new MySqlWriter();
            }

            throw new NotImplementedException();
        }
    }

    public enum ConfirmCopyOption
    {
        [Display(Name = "Cancel")]
        Cancel,
        [Display(Name = "Start copying")]
        Copy,
        [Display(Name = "Preview scripts and ask to confirm")]
        ConfirmScripts,
        [Display(Name = "Just output sql scripts")]
        Scripts,
    }
}
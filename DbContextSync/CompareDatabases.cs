using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using DbContextSync.CliDrawing;
using DbContextSync.Enums;
using DbContextSync.Extensions;
using DbContextSync.Helpers;
using DbContextSync.Models;
using DbContextSync.Prompts;
using DbContextSync.Walkers;

namespace DbContextSync
{
    public class CompareDatabases
    {
        private PhysicalDatabase _database;
        private List<DbContextDatabase> _dbContexts;
        public MergedDatabase Merged => new(_activeDbContext, _database);

        private int _activeContextIndex = 0;
        private DbContextDatabase _activeDbContext => _dbContexts[_activeContextIndex];

        public CompareDatabases(Arguments args)
        {
            _database = args.GetDatabase();
            _dbContexts = args.GetDbContext();

            if (_dbContexts.Count > 1 || args.ContextClass != null)
            {
                if (args.ContextClass == null && (args.ChangeCountOnly || args.PreviewOnly))
                {
                    ErrorState.SingleContextNeeded.Kill(spaceBefore: true);
                }

                SelectPrompt prompt = new("Found multiple instances of DBContext, please choose one", _dbContexts.Select(c => c.Name));
                if (_dbContexts.Count == 1) { prompt.Question = "Select DBContext class"; }
                prompt.AutoSelectValue = _dbContexts.FindIndex(c => c.Name == args.ContextClass);
                prompt.SilentOnAuto = args.ChangeCountOnly || args.PreviewOnly;
                prompt.SpaceBefore = true;

                if (args.ContextClass != null && prompt.AutoSelectValue == -1)
                {
                    ErrorState.DbContextClassNotFound.Kill(args.ContextClass, true);
                }

                _activeContextIndex = prompt.Ask();
            }
            else if (!args.ChangeCountOnly && !args.PreviewOnly)
            {
                Console.WriteLine();
            }
        }

        public int DifferenceCount(bool databaseDeletesOnly = false)
        {
            int tableDiffs = Merged.Tables
                .Count(t => databaseDeletesOnly ? t.DatabaseOnly : t.Differs);
            int fieldDiffs = Merged.Tables
                .Where(t => t.SameInBoth)
                .SelectMany(t => t.Fields)
                .Count(f => databaseDeletesOnly ? f.DatabaseOnly : f.Differs);

            return tableDiffs + fieldDiffs;
        }

        public void PreviewDifferences(ref Arguments args, bool selectDirection)
        {
            Direction direction = args.Direction;
            DatabaseType? databaseType = args.DatabaseType;
            List<List<Columns>> columns = EnumExtensions.ToList<Direction>().Select(d => d.PrintableDifferences(this, databaseType)).ToList();
            int columnWidth = columns.SelectMany(c => c).Select(c => c.MaxColumnWidth).Max();

            int differenceCount = DifferenceCount();
            int databaseDeleteCount = DifferenceCount(databaseDeletesOnly: true);

            columns[(int)direction].Draw(columnWidth);

            if (selectDirection)
            {
                SelectPrompt<Direction> prompt = new("What would you like to copy?");

                prompt.AutoSelectValue = direction != Direction.NoneSelected ? direction : Prompts.Nullable<Direction>.Null;
                prompt.Validate = validateIndex => validateIndex != 0;
                prompt.SelectedText = value => value[4..];
                if (databaseDeleteCount == 0) { prompt.AddHideOption(Direction.ToDatabaseWithDeletes); }
                if (differenceCount == databaseDeleteCount) { prompt.AddHideOption(Direction.ToDatabase); }

                args.Direction = prompt.Ask(direction => columns[(int)direction].Draw(columnWidth, true));
            }
        }

        public List<DbContextTable> ClassesAtPath(string? path)
        {
            return _dbContexts.SelectMany(c => c.Tables).Cast<DbContextTable>().Where(t => t.ClassPath == path).ToList();
        }
    }
}
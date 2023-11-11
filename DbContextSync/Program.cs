using System;
using System.Linq;
using DbContextSync.Enums;
using DbContextSync.Models;
using DbContextSync.Writers;

namespace DbContextSync
{
    class Program
    {
        static void Main(string[] args)
        {
            Arguments arguments = args.Parse();

            CompareDatabases compare = new(arguments);
            int differenceCount = compare.DifferenceCount();
            if (arguments.ChangeCountOnly)
            {
                Console.WriteLine(differenceCount);
            }
            else
            {
                Console.WriteLine($"Comparing DBContext ({compare.Merged.DbContextDatabase.Name}) to {arguments.DatabaseType} ({compare.Merged.PhysicalDatabase.Name})");
                if (compare.Merged.PhysicalDatabase.DatabaseExists || arguments.PreviewOnly)
                {
                    Console.WriteLine($"Found {differenceCount} differences");
                    if (differenceCount > 0)
                    {
                        compare.PreviewDifferences(ref arguments, selectDirection: !arguments.PreviewOnly);

                        if (!arguments.PreviewOnly)
                        {
                            compare.Write(arguments);
                        }
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
                else
                {
                    arguments.Direction = Direction.ToDatabase;
                    compare.Write(arguments);
                }
            }
        }
    }
}
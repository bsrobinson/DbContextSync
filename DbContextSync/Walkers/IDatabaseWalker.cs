using System;
using DbContextSync.Enums;
using DbContextSync.Helpers;
using DbContextSync.Models;
using DbContextSync.Walkers.DatabaseWalkers;

namespace DbContextSync.Walkers
{
    public interface IDatabaseWalker
    {
        public abstract void Connect();
        public abstract PhysicalDatabase Get();
    }

    public static class DatabaseWalkerExtensions
    {
        public static PhysicalDatabase GetDatabase(this Arguments args)
        {
            IDatabaseWalker databaseWalker = GetDatabaseWalker(args);
            
            try
            {
                databaseWalker.Connect();
            }
            catch (Exception e)
            {
                throw ErrorState.DatabaseConnectionError.Kill(e.Message, true);
            }

            try
            {
                return databaseWalker.Get();
            }
            catch (Exception e)
            {
                throw ErrorState.DatabaseWalkError.Kill(e.Message, true);
            }
        }

        private static IDatabaseWalker GetDatabaseWalker(Arguments args)
        {
            switch (args.DatabaseType)
            {
                case DatabaseType.MySql: return new MySqlWalker(args);
            }

            throw new NotImplementedException();
        }
    }
}
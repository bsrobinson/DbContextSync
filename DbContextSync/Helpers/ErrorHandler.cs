using System;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using PastelExtended;
using DbContextSync.Extensions;

namespace DbContextSync.Helpers
{
	public static class ErrorHandler
	{
		public static Exception Kill(this ErrorState errorState, string? formatArg0 = null, bool spaceBefore = false)
		{
			string message = errorState.DisplayName();

            if (spaceBefore) { Console.WriteLine(); }
			if (message != "")
			{
				if (formatArg0 != null) { message = string.Format(message, formatArg0); }
				Console.WriteLine($"{message.Fg(ConsoleColor.Red)}\n");
			}
            Environment.Exit((int)errorState);
            return new UnreachableException();
        }
	}

	public enum ErrorState
	{
		ArgumentError = -101,

        [Display(Name = "Database type cannot be derived from the connection string; specify using --databasetype=")]
        UnknownDatabaseType = -102,


        [Display(Name = "No DbContexts found in {0}")]
		NoDbContexts = -201,

        [Display(Name = "Provided context class ({0}) not found")]
		DbContextClassNotFound = -202,

        [Display(Name = "There are multiple DbContexts; you must specify one if using --count or --preview")]
        SingleContextNeeded = -203,

		[Display(Name = "Please specify the database name in your connection string (it doesn't have to exist yet)")]
		DatabaseNameMissing = 211,

        [Display(Name = "Unable to connect to database; {0}")]
        DatabaseConnectionError = -212,

        [Display(Name = "Error reading database; {0}")]
        DatabaseWalkError = -213,


        [Display(Name = "Your terminal window is not wide enough to display the differences, please expand to at least {0} and try again")]
        WindowTooNarrow = -301,


        [Display(Name = "Cannot convert the datatype: {0}")]
        CannotConvertDataTyoe = -401,
    }
}


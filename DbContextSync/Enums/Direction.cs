using System.ComponentModel.DataAnnotations;

namespace DbContextSync.Enums
{
    public enum Direction
    {
        [Display(Name = "<-> Preview Differences")]
        NoneSelected,

        [Display(Name = "<-- Copy to DBContext")]
        ToDbContext,

        [Display(Name = "--> Copy to Database (no deletes)")]
        ToDatabase,

        [Display(Name = "--> Copy to Database (including deletes)")]
        ToDatabaseWithDeletes,
    }
}
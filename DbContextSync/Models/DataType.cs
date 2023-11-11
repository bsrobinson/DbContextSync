namespace DbContextSync.Models
{
    public class DataType
    {
        public string Name { get; set; }
        public int? MaxLength { get; set; }

        public DataType(string name, int? maxLength)
        {
            Name = name;
            MaxLength = maxLength;
        }

        public bool SameAs(DataType dataType)
        {
            return Name == dataType.Name
                && MaxLength == dataType.MaxLength;
        }
    }
}
using System.Collections.Generic;

namespace DbContextSync.Helpers
{
    public class ConnectionString
    {
        public Dictionary<string, string> Parsed = new();

        public ConnectionString(string value)
        {
            foreach (string property in value.Split(";"))
            {
                string[] nameValue = property.Split("=");
                if (nameValue[0] != "")
                {
                    Parsed.Add(nameValue[0], nameValue.Length > 1 ? nameValue[1] : "");
                }
            }
        }

        public string? DatabaseName => PropertyValue("database");
        public string? Port => PropertyValue("port");

        public string? PropertyValue(string key)
        {
            Parsed.TryGetValue(key.ToString(), out string? value);
            return value;
        }

        public override string ToString() => ToString(includeDatabase: true);
        public string ToStringWithoutDatabase() => ToString(includeDatabase: false);

        private string ToString(bool includeDatabase)
        {
            List<string> nameValues = new();

            foreach (KeyValuePair<string, string> keyValuePair in Parsed)
            {
                if (keyValuePair.Key != "database" || includeDatabase)
                {
                    nameValues.Add($"{keyValuePair.Key}={keyValuePair.Value}");
                }
            }

            return string.Join(";", nameValues) + ";";
        }
	}
}
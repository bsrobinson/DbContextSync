    using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using DbContextSync.Models;
using DbContextSync.Helpers;
using DbContextSync.Extensions;

namespace DbContextSync.Walkers.DatabaseWalkers
{
    public class MySqlWalker : IDatabaseWalker
	{
        private Arguments _args;

        private bool _databaseExists = false;

        private string _tablesSql = "SHOW TABLES";
        private string _columnsSql = "SHOW COLUMNS IN {0}";


        public MySqlWalker(Arguments args)
        {
            _args = args;
        }

        public void Connect()
        {
            string? databaseName = _args.ConnectionString.DatabaseName;
            if (string.IsNullOrEmpty(databaseName))
            {
                throw ErrorState.DatabaseNameMissing.Kill(spaceBefore: true);
            }

            foreach (DataRow row in ExecuteAsync("SHOW DATABASES", _args.ConnectionString.ToStringWithoutDatabase()).Result.Rows)
            {
                if (row[0].ToString().EqualsNoCase(databaseName))
                {
                    _databaseExists = true;
                }
            }
        }

        public PhysicalDatabase Get()
        {
            if (_databaseExists)
            {
                PhysicalDatabase database = new(_args, ExecuteAsync(_tablesSql).Result);
                Parallel.ForEach(database.Tables, new() { MaxDegreeOfParallelism = 10 }, async table =>
                {

                    DataTable columns = await ExecuteAsync(string.Format(_columnsSql, table.Name));

                    foreach (DataRow row in columns.Rows)
                    {
                        string? fieldName = row[0].ToString();
                        string? type = row[1].ToString();

                        if (fieldName != null && type != null)
                        {
                            table.Fields.Add(new DatabaseField
                            {
                                Name = fieldName,
                                Type = ConvertType(type),
                                PrimaryKey = (row[3].ToString() ?? "") == "PRI",
                                IsUnique = (row[3].ToString() ?? "") == "UNI",
                                Required = row[2].ToString() == "NO",
                                AutoIncrement = row[5].ToString()?.Contains("auto_increment") == true,
                            });
                        }
                    }

                });

                return database;
            }
            else
            {
                return new(_args);
            }
        }


        private async Task<DataTable> ExecuteAsync(string sql, string? connectionString = null)
        {
            MySqlConnection connection = new(connectionString ?? _args.ConnectionString.ToString());
            connection.Open();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            DataTable dataTable = new();
            using (MySqlDataAdapter dataAdapter = new(sql, connection))
            {
                await dataAdapter.FillAsync(dataTable);
            }

            connection.Close();

            return dataTable;
        }

        private DataType ConvertType(string type)
        {
            type = type.ToLower();

            bool unsigned = false;
            if (type.EndsWith(" unsigned"))
            {
                unsigned = true;
                type = type[..^9];
            }

            int? length = null;
            if (type.EndsWith(")"))
            {
                int bracketPos = type.IndexOf("(");
                string lenStr = type[(bracketPos + 1)..^1];
                if (lenStr.Contains(',')) { lenStr = lenStr[..lenStr.IndexOf(",")]; }
                if (lenStr != "")
                {
                    length = int.Parse(lenStr);
                }
                type = type[..bracketPos];
            }

            if (type.Contains("char"))
            {
                type = "string";
            }
            else
            {
                if (type.Contains("binary") || type.Contains("text") || type.Contains("blob"))
                {
                    type = "string";
                }
                else if (type.Contains("bool") || (type.Contains("tinyint") && length == 1))
                {
                    type = "bool";
                }
                else if (type == "bit" || type == "tinyint" || type == "smallint" || type == "mediumint" || type == "year")
                {
                    type = $"{(unsigned ? "u" : "")}short";
                }
                else if (type == "int" || type == "integer")
                {
                    type = $"{(unsigned ? "u" : "")}int";
                }
                else if (type == "bigint")
                {
                    type = $"{(unsigned ? "u" : "")}long";
                }
                //float, double, decimal kept as-is
                else if (type == "dec")
                {
                    type = $"decimal";
                }
                else if (type == "datetime" || type == "timestamp")
                {
                    type = $"DateTime";
                }
                else if (type == "date")
                {
                    type = $"DateOnly";
                }
                else if (type == "time")
                {
                    type = $"TimeOnly";
                }

                length = null;
            }

            return new(type, length);
        }
    }
}


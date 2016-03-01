using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace DbLocalizer
{
    public class DbFunctions
    {
        private static string _connectionString;
        public static string ConnectionString
        {
            get
            {
                return _connectionString ??
                       (_connectionString =
                           ConfigurationManager.ConnectionStrings["localizationConnectionString"].ConnectionString);
            }
            set { _connectionString = value; }
        }

        public DbFunctions()
        {
        }

        public DbFunctions(string connString)
        {
            ConnectionString = connString;
        }

        public ResourceRecord GetResourceValue(string page, string culture, string key)
        {
            ResourceRecord resource = null;

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                using (var cmd = new NpgsqlCommand("dblocalizer_get_by_type_and_culture"))
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();

                    cmd.AddParameters(new Dictionary<string, object>
                    {
                        {"_resource_page", page},
                        {"_culture_code", culture},
                        {"_resource_key", key}
                    });

                    using (var comReader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        while (comReader.Read())
                            resource = new ResourceRecord(comReader);
                }
            }
            return resource;
        }

        public List<ResourceRecord> GetResourcesForPage(string culture, string page)
        {
            var records = new List<ResourceRecord>();
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                using (var cmd = new NpgsqlCommand("dblocalizer_resources_by_culture") {CommandType = CommandType.StoredProcedure})
                {
                    cmd.Connection = conn;
                    conn.Open();

                    cmd.AddParameters(new Dictionary<string, object>
                    {
                        {"_resource_page", page},
                        {"_culture_code", culture},
                    });

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            records.Add(new ResourceRecord(reader));
                }
            }
            return records;
        }

        public Dictionary<string, string> GetDictionaryAllResourcesForPage(string page, string culture)
        {
            var resObjects = GetResourcesForPage(culture, page);

            return resObjects.ToDictionary(record => record.Key, record => record.Value);
        }

        public List<ResourceRecord> GetResourceForPageNoCulture(string page)
        {
            var records = new List<ResourceRecord>();

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                using (var cmd = new NpgsqlCommand("dblocalizer_resources_by_page_all_cultures") { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Connection = conn;
                    conn.Open();

                    cmd.AddParameters(new Dictionary<string, object> {{"_resource_page", page}});

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            records.Add(new ResourceRecord(reader));
                }
            }

            return records;
        }

        public List<string> GetPageList()
        {
            var records = new List<string>();

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                using (var cmd = new NpgsqlCommand("dblocalizer_get_all_unique_pages") { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Connection = conn;
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            records.Add(reader[0].ToString());
                }
            }

            return records;
        } 

        public List<ResourceRecord> GetAllResources(string page = "", int limit = 0, int offset = 0)
        {
            var records = new List<ResourceRecord>();

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                using (var cmd = new NpgsqlCommand("dblocalizer_get_all_resources") { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Connection = conn;
                    conn.Open();

                    cmd.AddParameters(new Dictionary<string, object>
                    {
                        {"_page", page},
                        {"_limit", limit},
                        {"_offset", offset}
                    });

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            records.Add(new ResourceRecord(reader));
                }
            }

            return records;
        }
    }

    public static class NpgsqlExtensions
    {
        public static void AddParameters(this NpgsqlCommand command, Dictionary<string, object> parameters)
        {
            // if a connection hasn't been setup already lets do that thing
            if (command.Connection == null)
            {
                command.Connection = new NpgsqlConnection(DbFunctions.ConnectionString);
                command.Connection.Open();
            }
            // if the connection is close we need to opens it
            if (command.Connection.State == ConnectionState.Closed)
                command.Connection.Open();

            NpgsqlCommandBuilder.DeriveParameters(command);

            foreach (KeyValuePair<string, object> pair in parameters)
                command.Parameters[pair.Key].Value = pair.Value;
        }

        public static void AddParameters(this NpgsqlCommand command, List<NpgsqlParameter> parameters)
        {
            // if a connection hasn't been setup already lets do that thing
            if (command.Connection == null)
            {
                command.Connection = new NpgsqlConnection(DbFunctions.ConnectionString);
                command.Connection.Open();
            }
            // if the connection is close we need to opens it
            if (command.Connection.State == ConnectionState.Closed)
                command.Connection.Open();

            NpgsqlCommandBuilder.DeriveParameters(command);

            foreach (NpgsqlParameter parameter in parameters)
            {
                var commandParam = command.Parameters[parameter.ParameterName];
                commandParam.Value = parameter.Value;
                // 0 is not a valid type for a function, most likely this happens when there is an array involved.
                if (commandParam.NpgsqlDbType == 0 || commandParam.DbType == 0)
                    commandParam.NpgsqlDbType = parameter.NpgsqlDbType;
            }
        }
    }
}

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

        public List<ResourceRecord> GetResourcesForPage(string culture, string page)
        {
            var records = new List<ResourceRecord>();
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                using (var cmd = new NpgsqlCommand("localize_resources_by_culture") {CommandType = CommandType.StoredProcedure})
                {
                    cmd.Connection = conn;
                    conn.Open();

                    cmd.Parameters.AddWithValue("_resource_type", page);
                    cmd.Parameters.AddWithValue("_culture_code", culture);

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

            return resObjects.ToDictionary(record => record.ResourceKey, record => record.ResourceValue);
        }

        public List<ResourceRecord> GetResourceForPageNoCulture(string page)
        {
            var records = new List<ResourceRecord>();

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                using (var cmd = new NpgsqlCommand("localize_resources_by_page_all_cultures") { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Connection = conn;
                    conn.Open();

                    cmd.Parameters.AddWithValue("_resource_page", NpgsqlDbType.Text, page);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            records.Add(new ResourceRecord(reader));
                }
            }

            return records;
        }

        public List<ResourceRecord> GetAllResources()
        {
            var records = new List<ResourceRecord>();

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                using (var cmd = new NpgsqlCommand("localize_get_all_resources") { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Connection = conn;
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            records.Add(new ResourceRecord(reader));
                }
            }

            return records;
        }
    }
}

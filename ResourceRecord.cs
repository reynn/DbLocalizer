using System;
using System.Data;
using System.Data.Common;
using Npgsql;
using NpgsqlTypes;

namespace DbLocalizer
{
    public class ResourceRecord
    {
        public string ResourcePage { get; set; }
        public string CultureCode { get; set; }
        public string ResourceKey { get; set; }
        public string ResourceValue { get; set; }

        public ResourceRecord()
        {
        }

        public ResourceRecord(DbDataReader reader)
        {
            ResourcePage = reader.GetString(reader.GetOrdinal("resourcepage"));
            CultureCode = reader.GetString(reader.GetOrdinal("culturecode"));
            ResourceKey = reader.GetString(reader.GetOrdinal("resourcekey"));
            ResourceValue = reader.GetString(reader.GetOrdinal("resourcevalue"));
        }

        public void Save()
        {
            using (var conn = new NpgsqlConnection(DbFunctions.ConnectionString))
            {
                using (var cmd = new NpgsqlCommand("localize_save_resource") {CommandType = CommandType.StoredProcedure})
                {
                    cmd.Connection = conn;
                    conn.Open();

                    cmd.Parameters.AddWithValue("_resource_page", NpgsqlDbType.Text, ResourcePage);
                    cmd.Parameters.AddWithValue("_culture_code", NpgsqlDbType.Text, CultureCode);
                    cmd.Parameters.AddWithValue("_resource_key", NpgsqlDbType.Text, ResourceKey);
                    cmd.Parameters.AddWithValue("_resource_value", NpgsqlDbType.Text, ResourceValue);
                    
                    bool success = bool.Parse(cmd.ExecuteScalar().ToString());
                    if (!success)
                        throw new Exception("Failed to save the localized resource");
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Npgsql;
using NpgsqlTypes;

namespace DbLocalizer
{
    public class ResourceRecord
    {
        private readonly string _connectionString;
        public string Page { get; set; }
        public string CultureCode { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public ResourceRecord()
        {
        }

        public ResourceRecord(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ResourceRecord(DbDataReader reader)
        {
            Page = reader.GetString(reader.GetOrdinal("resourcepage"));
            CultureCode = reader.GetString(reader.GetOrdinal("culturecode"));
            Key = reader.GetString(reader.GetOrdinal("resourcekey"));
            Value = reader.GetString(reader.GetOrdinal("resourcevalue"));
        }

        public void Save()
        {
            using (var conn = new NpgsqlConnection(string.IsNullOrEmpty(_connectionString) ? DbFunctions.ConnectionString : _connectionString))
            {
                using (var cmd = new NpgsqlCommand("dblocalizer_save_resource") {CommandType = CommandType.StoredProcedure})
                {
                    cmd.Connection = conn;
                    conn.Open();

                    cmd.AddParameters(new Dictionary<string, object>
                    {
                        {"_resource_page", Page.TrimStart('\\')},
                        {"_culture_code", CultureCode},
                        {"_resource_key", Key},
                        {"_resource_value", Value}
                    });
                    
                    bool success = bool.Parse(cmd.ExecuteScalar().ToString());
                    if (!success)
                        throw new Exception("Failed to save the localized resource");
                }
            }
        }
    }
}
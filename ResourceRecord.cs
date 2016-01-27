using System;
using System.Data;
using System.Data.Common;
using Npgsql;
using NpgsqlTypes;

namespace DbLocalizer
{
    public class ResourceRecord
    {
        private string _connectionString;
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
                using (var cmd = new NpgsqlCommand("localize_save_resource") {CommandType = CommandType.StoredProcedure})
                {
                    cmd.Connection = conn;
                    conn.Open();

                    cmd.Parameters.AddWithValue("_resource_page", NpgsqlDbType.Text, Page.TrimStart('\\'));
                    cmd.Parameters.AddWithValue("_culture_code", NpgsqlDbType.Text, CultureCode);
                    cmd.Parameters.AddWithValue("_resource_key", NpgsqlDbType.Text, Key);
                    cmd.Parameters.AddWithValue("_resource_value", NpgsqlDbType.Text, Value);
                    
                    bool success = bool.Parse(cmd.ExecuteScalar().ToString());
                    if (!success)
                        throw new Exception("Failed to save the localized resource");
                }
            }
        }
    }
}
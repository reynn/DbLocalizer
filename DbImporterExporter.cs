using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Resources;
using System.Threading.Tasks;
using Npgsql;

namespace DbLocalizer
{
    public class DbExporter
    {
        private readonly string _connectionString;

        public DbExporter() {  }

        public DbExporter(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void ExportFromDatabase(string exportPath)
        {
            var allRecords = new DbFunctions(_connectionString).GetAllResources();

            foreach (ResourceRecord record in allRecords)
            {
                using (ResXResourceWriter writer = new ResXResourceWriter(GetExportPath(exportPath, record)))
                {
                    writer.AddResource(record.Key, record.Value);
                }
            }
        }

        private string GetExportPath(string basePath, ResourceRecord record)
        {
            string fileName = record.Page + (record.CultureCode == "en"
                ? string.Empty
                : "." + record.CultureCode) + ".resx";

            string fullExportPath = Path.Combine(basePath, fileName);
            if (!Directory.Exists(fullExportPath.Substring(0, fullExportPath.LastIndexOf(@"\"))))
                Directory.CreateDirectory(fullExportPath.Substring(0, fullExportPath.LastIndexOf(@"\")));
            return fullExportPath;
        }
    }

    public class DbImporter
    {
        private readonly string _connectionString;

        public DbImporter()
        {
            _connectionString = DbFunctions.ConnectionString;
            CheckForDatabase(_connectionString);
        }

        public DbImporter(string connectionString)
        {
            _connectionString = connectionString;

            CheckForDatabase(connectionString);
        }

        private void CheckForDatabase(string connectionString)
        {
            var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            var desiredDatabaseName = connectionBuilder.Database;
            connectionBuilder.Database = "postgres";
            var psqlCommand = string.Format("select 1 from pg_database where datname = '{0}'",
                desiredDatabaseName);
            using (var conn = new NpgsqlConnection(connectionBuilder))
            {
                using (var cmd = new NpgsqlCommand(psqlCommand) {CommandType = CommandType.Text})
                {
                    cmd.Connection = conn;
                    conn.Open();

                    var result = cmd.ExecuteScalar();
                    if (result == null || result.ToString() != "1")
                        TryCreateDatabase(desiredDatabaseName, conn);
                }
            }
        }

        private void TryCreateDatabase(string database, NpgsqlConnection conn)
        {
            if (conn.State == ConnectionState.Closed)
                conn.Open();
            try
            {
                using (var cmd = new NpgsqlCommand(string.Format("create database {0};", database)))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = conn;
                    cmd.ExecuteNonQuery();

                    conn.Close();
                    var builder = new NpgsqlConnectionStringBuilder(conn.ConnectionString) {Database = database};
                    conn.ConnectionString = builder.ToString();
                    conn.Open();

                    var sql = File.ReadAllText("../../SQL/DbLocalizer Schema.sql");

                    cmd.CommandText = sql;

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                if (ex.Data["Code"].ToString() == "55006")
                {
                    DropConnectionToTemplateDb(conn);
                    TryCreateDatabase(database, conn);
                }
                else
                {
                    Console.WriteLine(string.Format("Failed to create database, message: {0}", ex.Message));
                }
            }
            finally
            {
                conn.Close();
            }
        }

        /// <summary>
        /// This will attempt to drop connections to the template database so we can try creating the db again
        /// </summary>
        /// <param name="conn">Connection to use for command.</param>
        private void DropConnectionToTemplateDb(NpgsqlConnection conn)
        {
            using (var cmd = new NpgsqlCommand("SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = 'template1' AND pid <> pg_backend_pid();"))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// This will go through all files in your basePath and look for resx files in app_localresources folders, 
        /// </summary>
        /// <param name="basePath">Start location for the search</param>
        /// <param name="defaultCulture">Default Culture for your site, usually en or en-US, for files where the culture isn't specified</param>
        public void ImportProject(string basePath, string defaultCulture = "en-US")
        {
            var files = Directory.EnumerateFiles(basePath, "*.resx", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (file.Contains(@"\obj\") || file.Contains(@"\bin\")) continue;
                ImportResxFile(file, basePath, defaultCulture, file.Contains("App_GlobalResources"));
            }
        }

        public void ImportResxFile(string resxPath, string rootPath, string defaultCulture = "en-US", bool globalResource = false)
        {
            using (var resxFile = new ResXResourceReader(resxPath))
            {
                var dictEnumerator = resxFile.GetEnumerator();
                while (dictEnumerator.MoveNext())
                {
                    var record = new ResourceRecord(_connectionString)
                    {
                        Page = GetPagePath(resxPath, rootPath, globalResource),
                        CultureCode = GetCulture(resxPath, defaultCulture),
                        Key = dictEnumerator.Key.ToString(),
                        Value = dictEnumerator.Value.ToString()
                    };
                    record.Save();
                    Console.WriteLine(string.Format("Saved record for page {0}, key: {1} value: {2}", record.Page, record.Key, record.Value));
                }
            }
        }

        private string GetPagePath(string path, string rootPath, bool globalResource = false)
        {
            var tempPath = path.Replace(rootPath, string.Empty);
            var splits = tempPath.Split('.');
            return string.Format("{0}.{1}",
                splits[0].Replace("App_LocalResources\\", string.Empty),
                globalResource ? string.Empty : splits[1]).Trim('.');
        }

        private string GetCulture(string path, string defaultCulture)
        {
            string temp = path.Substring(path.LastIndexOf("\\") + 1);

            var splitTemp = temp.Split('.');
            string culture = splitTemp[splitTemp.Length - 2];
            // check to see if we got the culture
            CultureInfo info;
            try
            {
                info = new CultureInfo(culture);
            }
            catch (CultureNotFoundException cnfe)
            {
                return defaultCulture;
            }

            return info.Name;
        }
    }
}

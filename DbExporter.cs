using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Resources;
using System.Threading.Tasks;

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
                    writer.AddResource(record.ResourceKey, record.ResourceValue);
                }
            }
        }

        private string GetExportPath(string basePath, ResourceRecord record)
        {
            string fileName = record.ResourcePage + (record.CultureCode == "en"
                ? string.Empty
                : "." + record.CultureCode) + ".resx";

            string fullExportPath = Path.Combine(basePath, fileName);
            if (!Directory.Exists(fullExportPath.Substring(0, fullExportPath.LastIndexOf(@"\"))))
                Directory.CreateDirectory(fullExportPath.Substring(0, fullExportPath.LastIndexOf(@"\")));
            return fullExportPath;
        }
    }
}

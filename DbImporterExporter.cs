﻿using System;
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
        }

        public DbImporter(string connectionString)
        {
            _connectionString = connectionString;
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
                ImportResxFile(file, basePath, defaultCulture);
            }
        }

        public void ImportResxFile(string resxPath, string rootPath, string defaultCulture = "en-US")
        {
            using (var resxFile = new ResXResourceReader(resxPath))
            {
                var dictEnumerator = resxFile.GetEnumerator();
                while (dictEnumerator.MoveNext())
                {
                    var record = new ResourceRecord
                    {
                        Page = GetPagePath(resxPath, rootPath),
                        CultureCode = GetCulture(resxPath, defaultCulture),
                        Key = dictEnumerator.Key.ToString(),
                        Value = dictEnumerator.Value.ToString()
                    };
                    record.Save();
                }
            }
        }

        private string GetPagePath(string path, string rootPath)
        {
            var tempPath = path.Replace(rootPath, string.Empty);
            var splits = tempPath.Split('.');
            return string.Format("{0}.{1}", splits[0].Replace("App_LocalResources\\", string.Empty), splits[1]);
        }

        private string GetCulture(string path, string defaultCulture)
        {
            string temp = path.Substring(path.LastIndexOf("\\")+1);

            if (temp.EndsWith(".aspx.resx", StringComparison.InvariantCultureIgnoreCase))
                return defaultCulture;

            var splitTemp = temp.Split('.');

            if (splitTemp.Length <= 2)
                return defaultCulture;

            return splitTemp[2];
        }
    }
}

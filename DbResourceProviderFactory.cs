using System;
using System.Diagnostics;
using System.Globalization;
using System.Web.Compilation;

namespace DbLocalizer
{
    public class DbResourceProviderFactory : ResourceProviderFactory
    {
        public override IResourceProvider CreateGlobalResourceProvider(string classKey)
        {
            Debug.WriteLine(String.Format(CultureInfo.InvariantCulture, "DBResourceProviderFactory.CreateGlobalResourceProvider({0})", classKey));
            return new DbResourceProvider(classKey);
        }

        public override IResourceProvider CreateLocalResourceProvider(string virtualPath)
        {
            Debug.WriteLine(String.Format(CultureInfo.InvariantCulture, "DBResourceProviderFactory.CreateLocalResourceProvider({0}", virtualPath));

            // we should always get a path from the runtime
            string classKey = virtualPath;
            if (!string.IsNullOrEmpty(virtualPath))
            {
                virtualPath = virtualPath.Remove(0, 1);
                classKey = virtualPath.Remove(0, virtualPath.IndexOf('/') + 1);
            }

            return new DbResourceProvider(classKey);
        }
    }
}
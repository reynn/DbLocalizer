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
            return new DbResourceProvider(classKey, true);
        }

        public override IResourceProvider CreateLocalResourceProvider(string virtualPath)
        {
            return new DbResourceProvider(virtualPath.TrimStart('/').Replace("/", @"\"));
        }
    }
}
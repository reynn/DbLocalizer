using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Web.Compilation;

namespace DbLocalizer
{
    public class DbResourceProvider : DisposableBaseType, IResourceProvider
    {
        private string _mClassKey;
        private StringResourcesDalc _mDalc;

        // resource cache
        private readonly Dictionary<string, Dictionary<string, string>> _mResourceCache = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Constructs this instance of the provider 
        /// supplying a resource type for the instance. 
        /// </summary>
        /// <param name="resourceType">The resource type.</param>
        public DbResourceProvider(string classKey)
        {
            this._mClassKey = classKey;
            _mDalc = new StringResourcesDalc(classKey);

        }

        #region IResourceProvider Members

        /// <summary>
        /// Retrieves a resource entry based on the specified culture and 
        /// resource key. The resource type is based on this instance of the
        /// DBResourceProvider as passed to the constructor.
        /// To optimize performance, this function caches values in a dictionary
        /// per culture.
        /// </summary>
        /// <param name="resourceKey">The resource key to find.</param>
        /// <param name="culture">The culture to search with.</param>
        /// <returns>If found, the resource string is returned. 
        /// Otherwise an empty string is returned.</returns>
        public object GetObject(string resourceKey, System.Globalization.CultureInfo culture)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("DBResourceProvider object is already disposed.");
            }

            if (string.IsNullOrEmpty(resourceKey))
            {
                throw new ArgumentNullException("resourceKey");
            }

            if (culture == null)
            {
                culture = CultureInfo.CurrentUICulture;
            }

            string resourceValue = null;
            Dictionary<string, string> resCacheByCulture = null;
            // check the cache first
            // find the dictionary for this culture
            // check for the inner dictionary entry for this key
            if (_mResourceCache.ContainsKey(culture.Name))
            {
                resCacheByCulture = _mResourceCache[culture.Name];
                if (resCacheByCulture.ContainsKey(resourceKey))
                {
                    resourceValue = resCacheByCulture[resourceKey];
                }
            }

            // if not in the cache, go to the database
            if (resourceValue == null)
            {
                resourceValue = _mDalc.GetResourceByCultureAndKey(culture, resourceKey);

                // add this result to the cache
                // find the dictionary for this culture
                // add this key/value pair to the inner dictionary
                lock (this)
                {
                    if (resCacheByCulture == null)
                    {
                        resCacheByCulture = new Dictionary<string, string>();
                        _mResourceCache.Add(culture.Name, resCacheByCulture);
                    }
                    resCacheByCulture.Add(resourceKey, resourceValue);
                }
            }
            return resourceValue;
        }

        /// <summary>
        /// Returns a resource reader.
        /// </summary>
        public System.Resources.IResourceReader ResourceReader
        {
            get
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException("DBResourceProvider object is already disposed.");
                }

                // this is required for implicit resources 
                // this is also used for the expression editor sheet 

                ListDictionary resourceDictionary = this._mDalc.GetResourcesByCulture(CultureInfo.InvariantCulture);

                return new DbResourceReader(resourceDictionary);
            }
        }

        #endregion

        protected override void Cleanup()
        {
            try
            {
                this._mDalc.Dispose();
                this._mResourceCache.Clear();
            }
            finally
            {
                base.Cleanup();
            }
        }

    }
}

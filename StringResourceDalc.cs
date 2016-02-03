using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Npgsql;

namespace DbLocalizer
{
    public class StringResourcesDalc : IDisposable
    {
        private readonly string _defaultResourceCulture = "en-US";
        private readonly string _resourcePage;

        /// <summary>
        /// Constructs this instance of the data access 
        /// component supplying a resource type for the instance. 
        /// </summary>
        /// <param name="resourcePage">The resource type.</param>
        public StringResourcesDalc(string resourcePage)
        {
            // save the resource type for this instance
            this._resourcePage = resourcePage;
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["defaultCulture"]))
                _defaultResourceCulture = ConfigurationManager.AppSettings["defaultCulture"];
        }

        /// <summary>
        /// Uses an open database connection to recurse 
        /// looking for the resource.
        /// Retrieves a resource entry based on the 
        /// specified culture and resource 
        /// key. The resource type is based on this instance of the
        /// StringResourceDALC as passed to the constructor.
        /// Resource fallback follows the same mechanism 
        /// of the .NET 
        /// ResourceManager. Ultimately falling back to the 
        /// default resource
        /// specified in this class.
        /// </summary>
        /// <param name="culture">The culture to search with.</param>
        /// <param name="resourceKey">The resource key to find.</param>
        /// <returns>If found, the resource string is returned. 
        /// Otherwise an empty string is returned.</returns>
        private string GetResourceByCultureAndKeyInternal(CultureInfo culture, string resourceKey)
        {
            if (string.IsNullOrEmpty(culture.Name))
                culture = CultureInfo.CurrentUICulture;

            // we should only get one back, but just in case, we'll iterate reader results
            var resources = new DbFunctions().GetResourceValue(_resourcePage, culture.Name, resourceKey);

            // we should only get 1 back, this is just to verify the tables aren't incorrect
            if (resources == null || string.IsNullOrEmpty(resources.Value))
            {
                // is this already fallback location?
                if (culture.Name == this._defaultResourceCulture)
                {
                    throw new InvalidOperationException(String.Format(Thread.CurrentThread.CurrentUICulture,
                        Properties.Resource.RM_DefaultResourceNotFound, resourceKey));
                }

                // try to get parent culture
                culture = culture.Parent;
                if (string.IsNullOrEmpty(culture.Name))
                {
                    // there isn't a parent culture, change to neutral
                    culture = new CultureInfo(this._defaultResourceCulture);
                } 
                return this.GetResourceByCultureAndKeyInternal(culture, resourceKey);
            }

            return resources.Value;
        }

        /// <summary>
        /// Returns a dictionary type containing all resources for a 
        /// particular resource type and culture.
        /// The resource type is based on this instance of the
        /// StringResourceDALC as passed to the constructor.
        /// </summary>
        /// <param name="culture">The culture to search for.</param>
        /// <param name="resourceKey">The resource key to 
        /// search for.</param>
        /// <returns>If found, the dictionary contains key/value 
        /// pairs for each 
        /// resource for the specified culture.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public ListDictionary GetResourcesByCulture(CultureInfo culture)
        {
            // make sure we have a default culture at least
            if (culture == null || culture.Name.Length == 0)
            {
                culture = new CultureInfo(this._defaultResourceCulture);
            }

            // create the dictionary
            ListDictionary resourceDictionary = new ListDictionary();

            // gather resource and create the dictionary
            var resources = new DbFunctions().GetResourcesForPage(culture.Name, _resourcePage);
            resources.ForEach(r => resourceDictionary.Add(r.Key, r.Value));

            return resourceDictionary;
        }

        /// <summary>
        /// Retrieves a resource entry based on the specified culture and 
        /// resource key. The resource type is based on this instance of the
        /// StringResourceDALC as passed to the constructor.
        /// To optimize performance, this function opens the database connection 
        /// before calling the internal recursive function. 
        /// </summary>
        /// <param name="culture">The culture to search with.</param>
        /// <param name="resourceKey">The resource key to find.</param>
        /// <returns>If found, the resource string is returned. Otherwise an empty string is returned.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public string GetResourceByCultureAndKey(CultureInfo culture, string resourceKey)
        {
            return GetResourceByCultureAndKeyInternal(culture, resourceKey);
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }
}

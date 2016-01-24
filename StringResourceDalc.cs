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
        private readonly string _mDefaultResourceCulture = "en";
        private readonly string _mResourceType;

        private readonly NpgsqlConnection _mConnection;
        private readonly NpgsqlCommand _mCmdGetResourceByCultureAndKey;
        private readonly NpgsqlCommand _mCmdGetResourcesByCulture;

        /// <summary>
        /// Constructs this instance of the data access 
        /// component supplying a resource type for the instance. 
        /// </summary>
        /// <param name="resourceType">The resource type.</param>
        public StringResourcesDalc(string resourceType)
        {
            // save the resource type for this instance
            this._mResourceType = resourceType;

            // grab the connection string
            _mConnection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["localizationConnectionString"].ConnectionString);

            // command to retrieve the resource the matches 
            // a specific type, culture and key
            _mCmdGetResourceByCultureAndKey = new NpgsqlCommand("localize_get_by_type_and_culture")
            {
                Connection = _mConnection,
                CommandType = CommandType.StoredProcedure
            };
            _mCmdGetResourceByCultureAndKey.Parameters.AddWithValue("_resource_type", resourceType);
            _mCmdGetResourceByCultureAndKey.Parameters.AddWithValue("_culture_code", string.Empty);
            _mCmdGetResourceByCultureAndKey.Parameters.AddWithValue("_resource_key", string.Empty);

            // command to retrieve all resources for a particular culture
            _mCmdGetResourcesByCulture =
                new NpgsqlCommand("localize_resources_by_culture")
                {
                    Connection = _mConnection,
                    CommandType = CommandType.StoredProcedure
                };
            _mCmdGetResourcesByCulture.Parameters.AddWithValue("_resource_type", _mResourceType);
            _mCmdGetResourcesByCulture.Parameters.AddWithValue("_culture_code", string.Empty);
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

            // we should only get one back, but just in case, we'll iterate reader results
            StringCollection resources = new StringCollection();
            string resourceValue = null;

            // set up the dynamic query params
            _mCmdGetResourceByCultureAndKey.Parameters["_culture_code"].Value = culture.Name;
            _mCmdGetResourceByCultureAndKey.Parameters["_resource_key"].Value = resourceKey;

            // get resources from the database
            if (_mConnection.FullState == ConnectionState.Closed)
                _mConnection.Open();
            using (NpgsqlDataReader reader = _mCmdGetResourceByCultureAndKey.ExecuteReader())
            {
                while (reader.Read())
                {
                    resources.Add(reader.GetString(reader.GetOrdinal("resourceValue")));
                }
            }

            // we should only get 1 back, this is just to verify the tables aren't incorrect
            if (resources.Count == 0)
            {
                // is this already fallback location?
                if (culture.Name == this._mDefaultResourceCulture)
                {
                    throw new InvalidOperationException(String.Format(Thread.CurrentThread.CurrentUICulture, Properties.Resource.RM_DefaultResourceNotFound, resourceKey));
                }

                // try to get parent culture
                culture = culture.Parent;
                if (culture.Name.Length == 0)
                {
                    // there isn't a parent culture, change to neutral
                    culture = new CultureInfo(this._mDefaultResourceCulture);
                }
                resourceValue = this.GetResourceByCultureAndKeyInternal(culture, resourceKey);
            }
            else if (resources.Count == 1)
            {
                resourceValue = resources[0];
            }
            else
            {
                // if > 1 row returned, log an error, we shouldn't have > 1 value for a resourceKey!
                throw new DataException(String.Format(Thread.CurrentThread.CurrentUICulture, Properties.Resource.RM_DuplicateResourceFound, resourceKey));
            }

            return resourceValue;
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
                culture = new CultureInfo(this._mDefaultResourceCulture);
            }

            // set up dynamic query string parameters
            _mCmdGetResourcesByCulture.Parameters["_culture_code"].Value = culture.Name;
            // create the dictionary
            ListDictionary resourceDictionary = new ListDictionary();

            // open a connection to gather resource and create the dictionary
            try
            {
                if (_mConnection.FullState == ConnectionState.Closed)
                    _mConnection.Open();

                // get resources from the database
                using (NpgsqlDataReader reader = this._mCmdGetResourcesByCulture.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string k = reader.GetString(reader.GetOrdinal("resourceKey"));
                        string v = reader.GetString(reader.GetOrdinal("resourceValue"));

                        resourceDictionary.Add(k, v);
                    }
                }
            }
            finally
            {
                _mConnection.Close();
            }
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
            string resourceValue = string.Empty;

            try
            {

                // make sure we have a default culture at least
                if (culture == null || culture.Name.Length == 0)
                {
                    culture = new CultureInfo(this._mDefaultResourceCulture);
                }

                // open the connection before we call the recursive reading function
                this._mConnection.Open();

                // recurse to find resource, includes fallback behavior
                resourceValue = this.GetResourceByCultureAndKeyInternal(culture, resourceKey);
            }
            finally
            {
                // cleanup the connection, reader won't do that if it was open prior to calling in, and that's what we wanted
                this._mConnection.Close();
            }
            return resourceValue;
        }

        #region IDisposable Members

        public void Dispose()
        {
            try
            {
                // TODO: add in idisposable pattern, check what we're cleaning up here
                this._mCmdGetResourceByCultureAndKey.Dispose();
                this._mCmdGetResourcesByCulture.Dispose();
                this._mConnection.Dispose();
            }
            catch { }
        }

        #endregion
    }
}

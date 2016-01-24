using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Resources;

namespace DbLocalizer
{
    public class DbResourceReader : DisposableBaseType, IResourceReader, IEnumerable<KeyValuePair<string, object>>
    {
        private ListDictionary _resourceDictionary;

        public DbResourceReader(ListDictionary resourceDictionary)
        {
            Debug.WriteLine("DBResourceReader()");

            _resourceDictionary = resourceDictionary;
        }

        #region IEnumerable<KeyValuePair<string,object>> Members

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("DBResourceReader object is already disposed.");
            }

            return _resourceDictionary.GetEnumerator() as IEnumerator<KeyValuePair<string, object>>;
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("DBResourceReader object is already disposed.");
            }

            return _resourceDictionary.GetEnumerator();
        }

        #endregion

        protected override void Cleanup()
        {
            try
            {
                _resourceDictionary = null;
            }
            finally
            {
                base.Cleanup();
            }
        }

        #region IResourceReader Members

        public void Close()
        {
            Dispose();
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            Debug.WriteLine("DBResourceReader.GetEnumerator()");

            // NOTE: this is the only enumerator called by the runtime for 
            // implicit expressions

            if (Disposed)
            {
                throw new ObjectDisposedException("DBResourceReader object is already disposed.");
            }

            return _resourceDictionary.GetEnumerator();
        }

        #endregion
    }
}
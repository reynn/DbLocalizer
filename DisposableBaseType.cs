using System;

namespace DbLocalizer
{
    public class DisposableBaseType : IDisposable
    {
        private bool _disposed;
        protected bool Disposed
        {
            get
            {
                lock (this)
                {
                    return _disposed;
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            lock (this)
            {
                if (_disposed) return;
                Cleanup();
                _disposed = true;

                GC.SuppressFinalize(this);
            }
        }

        #endregion

        protected virtual void Cleanup()
        {
            // override to provide cleanup
        }

        ~DisposableBaseType()
        {
            Cleanup();
        }

    }
}

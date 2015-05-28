using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Support
{
    public class DsDisposableAction : IDisposable
    {
        private Action _action;

        public DsDisposableAction(Action action)
        {
            _action = action;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _action();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

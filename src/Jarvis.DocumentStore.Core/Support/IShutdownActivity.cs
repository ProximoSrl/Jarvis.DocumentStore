using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Support
{
    public interface IShutdownActivity
    {
        void Shutdown();
    }
}

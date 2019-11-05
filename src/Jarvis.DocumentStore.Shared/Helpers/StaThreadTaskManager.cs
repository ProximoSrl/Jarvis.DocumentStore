using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Shared.Helpers
{
    /// <summary>
    /// http://www.stevencwilliams.com/2015/05/26/running-net-tasks-in-sta-threads
    /// 
    /// Code is quite simple, it simply create a new tread with STA apartment state, then 
    /// start the thread, to simulate a task the classic TaskCompletionSource is used so 
    /// you can use with standard tast management.
    /// </summary>
    public static class StaThreadTaskManager
    {
        public static Task StartSTATask(Action action)
        {
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    action();
                    source.SetResult(null);
                }
                catch (Exception ex)
                {
                    source.SetException(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return source.Task;
        }

        public static Task<TResult> StartSTATask<TResult>(Func<TResult> function)
        {
            TaskCompletionSource<TResult> source = new TaskCompletionSource<TResult>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    source.SetResult(function());
                }
                catch (Exception ex)
                {
                    source.SetException(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return source.Task;
        }
    }
}

using System;
using System.Threading;

namespace ObjectStream.Threading
{
    internal class Worker
    {
        public event WorkerExceptionEventHandler Error;

        public Worker()
        {
        }

        public void DoWork(Action action)
        {
            new Thread(DoWorkImpl) { IsBackground = true }.Start(action);
            //new Task(DoWorkImpl, action, CancellationToken.None, TaskCreationOptions.LongRunning).Start();
        }

        private void DoWorkImpl(object oAction)
        {
            var action = (Action)oAction;
            try
            {
                action();
            }
            catch (Exception e)
            {
                Callback(() => Fail(e));
            }
        }

        private void Fail(Exception exception)
        {
            if (Error != null)
                Error(exception);
        }

        private void Callback(Action action)
        {
            new Thread(new ThreadStart(action)) { IsBackground = true }.Start();
            //Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, _callbackThread);
        }
    }

    internal delegate void WorkerSucceededEventHandler();

    internal delegate void WorkerExceptionEventHandler(Exception exception);
}

using System.ComponentModel;
using System.Threading;
using ME3TweaksCore.Diagnostics;

namespace ME3TweaksCore.Helpers
{
    /// <summary>
    /// Background worker that sets the name of the thread. It also logs any exceptions that occurred in the thread to disk when the
    /// RunWorkerCompleted event is handled.
    /// </summary>
    public class NamedBackgroundWorker : BackgroundWorker
    {
        public NamedBackgroundWorker(string name)
        {
            Name = name;
            RunWorkerCompleted += InternalOnRunWorkerCompleted;
        }

        private void InternalOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Print the error out on completion
            if (e.Error != null)
            {
                MLog.Error($@"Exception occurred in {Name} thread: {e.Error.Message}");
                MLog.Error(e.Error.StackTrace);
            }

            RunWorkerCompleted -= InternalOnRunWorkerCompleted;
        }

        public string Name { get; private set; }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            if (Thread.CurrentThread.Name == null) // Can only set it once
                Thread.CurrentThread.Name = Name;

            base.OnDoWork(e);
        }
    }
}

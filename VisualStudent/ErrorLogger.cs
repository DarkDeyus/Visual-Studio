using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace VisualStudent
{
    public class ErrorLogger : ILogger
    {
        public ObservableCollection<BuildErrorEventArgs> errors = new ObservableCollection<BuildErrorEventArgs>();

        public LoggerVerbosity Verbosity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Parameters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.ErrorRaised += ErrorRaised;
        }

        public void Shutdown() { }

        private void ErrorRaised(object sender, BuildErrorEventArgs e) => errors.Add(e);
        
    }
}

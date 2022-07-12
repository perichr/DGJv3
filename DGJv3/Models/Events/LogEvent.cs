using System;

namespace DGJv3
{
    public delegate void LogEvent(object sender, LogEventArgs e);

    public class LogEventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public override string ToString()
        {
            return this.Message + (this.Exception == null ? string.Empty : $"(ex:{this.Exception.Message})");
        }
    }
}

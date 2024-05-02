using System;

namespace Electra.Common.Logging
{
    public interface IAppXLog
    {
        void Error(string message);
        void Error(Exception ex, string message);
        void Verbose(string message);
        void Verbose(Exception ex, string message);
        void Warn(string message);
        void Warn(Exception ex, string message);
        void Critical(string message);
        void Critical(Exception ex, string message);
        void Fatal(string message);
        void Fatal(Exception ex, string message);
        void Information(string message);
        void Information(Exception ex, string message);
        void Debug(string message);
        void Debug(Exception ex, string message);
    }
}
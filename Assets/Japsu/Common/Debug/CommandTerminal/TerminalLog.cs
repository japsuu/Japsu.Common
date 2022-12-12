using System.Collections.Generic;
using UnityEngine;

namespace Japsu.Common.CommandTerminal
{
    public enum TerminalLogType
    {
        Error = LogType.Error,
        Assert = LogType.Assert,
        Warning = LogType.Warning,
        Message = LogType.Log,
        Exception = LogType.Exception,
        Input,
        ShellMessage
    }

    public struct LogItem
    {
        public TerminalLogType Type;
        public string Message;
        public string StackTrace;
    }

    public class TerminalLog
    {
        private List<LogItem> logs = new();
        private int max_items;

        public List<LogItem> Logs => logs;

        public TerminalLog(int maxItems)
        {
            this.max_items = maxItems;
        }

        public void HandleLog(string message, TerminalLogType type)
        {
            HandleLog(message, "", type);
        }

        public void HandleLog(string message, string stackTrace, TerminalLogType type)
        {
            LogItem log = new()
            {
                Message = message,
                StackTrace = stackTrace,
                Type = type
            };

            logs.Add(log);

            if (logs.Count > max_items) logs.RemoveAt(0);
        }

        public void Clear()
        {
            logs.Clear();
        }
    }
}
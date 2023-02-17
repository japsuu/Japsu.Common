using System.Collections.Generic;

namespace Japsu.Common.Tools.DebugTools.CommandTerminal
{
    public class TerminalHistory
    {
        private List<string> history = new();
        private int position;

        public void Push(string commandString)
        {
            if (commandString == "") return;

            history.Add(commandString);
            position = history.Count;
        }

        public string Next()
        {
            position++;

            if (position >= history.Count)
            {
                position = history.Count;
                return "";
            }

            return history[position];
        }

        public string Previous()
        {
            if (history.Count == 0) return "";

            position--;

            if (position < 0) position = 0;

            return history[position];
        }

        public void Clear()
        {
            history.Clear();
            position = 0;
        }
    }
}
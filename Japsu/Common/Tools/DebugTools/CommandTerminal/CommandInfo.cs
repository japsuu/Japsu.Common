using System;

namespace Japsu.Common.Tools.DebugTools.CommandTerminal
{
    public struct CommandInfo
    {
        public Action<CommandArg[]> Proc;
        public int MaxArgCount;
        public int MinArgCount;
        public string Help;
        public string Usage;
        public bool Secret;
    }
}
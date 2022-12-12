using System;

namespace Japsu.Common.CommandTerminal
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
using System;

namespace Japsu.Common.Tools.DebugTools.CommandTerminal
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RegisterCommandAttribute : Attribute
    {
        private int min_arg_count = 0;
        private int max_arg_count = -1;

        public int MinArgCount
        {
            get => min_arg_count;
            set => min_arg_count = value;
        }

        public int MaxArgCount
        {
            get => max_arg_count;
            set => max_arg_count = value;
        }

        public string Name { get; set; }
        public string Help { get; set; }
        public string Usage { get; set; }

        /// <summary>
        /// if this is true, the command will not show up in help.
        /// </summary>
        public bool Secret { get; set; }

        public RegisterCommandAttribute(string commandName = null)
        {
            Name = commandName;
        }
    }
}
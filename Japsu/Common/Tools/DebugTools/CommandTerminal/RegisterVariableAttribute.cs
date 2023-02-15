using System;

namespace Japsu.Common.DebugTools.CommandTerminal
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RegisterVariableAttribute : Attribute
    {
        public string Name { get; set; }

        public RegisterVariableAttribute(string commandName = null)
        {
            Name = commandName;
        }
    }
}
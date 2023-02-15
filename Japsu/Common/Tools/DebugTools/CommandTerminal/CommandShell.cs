using System;
using System.Collections.Generic;
using System.Reflection;

namespace Japsu.Common.DebugTools.CommandTerminal
{
    public class CommandShell
    {
        private Dictionary<string, CommandInfo> commands = new();
        private Dictionary<string, PropertyInfo> variables = new();
        private List<CommandArg> arguments = new(); // Cache for performance

        public string IssuedErrorMessage { get; private set; }

        public Dictionary<string, CommandInfo> Commands => commands;

        public List<string> Variables => new List<string>(variables.Keys);

        /// <summary>
        /// Uses reflection to find all RegisterCommand and RegisterVariable attributes
        /// and adds them to the commands dictionary.
        /// </summary>
        public void RegisterCommandsAndVariables()
        {
            Dictionary<string, CommandInfo> rejectedCommands = new Dictionary<string, CommandInfo>();
            BindingFlags methodFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            BindingFlags propertyFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    foreach (MethodInfo method in type.GetMethods(methodFlags))
                    {
                        RegisterCommandAttribute attribute = Attribute.GetCustomAttribute(
                            method, typeof(RegisterCommandAttribute)) as RegisterCommandAttribute;

                        if (attribute == null)
                        {
                            if (method.Name.StartsWith("FRONTCOMMAND", StringComparison.CurrentCultureIgnoreCase))
                                // Front-end Command methods don't implement RegisterCommand, use default attribute
                                attribute = new RegisterCommandAttribute();
                            else
                                continue;
                        }

                        ParameterInfo[] methodsParams = method.GetParameters();

                        string commandName = InferFrontCommandName(method.Name);
                        Action<CommandArg[]> proc;

                        if (attribute.Name == null)
                            // Use the method's name as the command's name
                            commandName = InferCommandName(commandName == null ? method.Name : commandName);
                        else
                            commandName = attribute.Name;

                        if (methodsParams.Length != 1 || methodsParams[0].ParameterType != typeof(CommandArg[]))
                        {
                            // Method does not match expected Action signature,
                            // this could be a command that has a FrontCommand method to handle its arguments.
                            rejectedCommands.Add(commandName.ToUpper(),
                                CommandFromParamInfo(methodsParams, attribute.Help));
                            continue;
                        }

                        // Convert MethodInfo to Action.
                        // This is essentially allows us to store a reference to the method,
                        // which makes calling the method significantly more performant than using MethodInfo.Invoke().
                        proc = (Action<CommandArg[]>)Delegate.CreateDelegate(typeof(Action<CommandArg[]>), method);
                        AddCommand(commandName, proc, attribute.MinArgCount, attribute.MaxArgCount, attribute.Help,
                            attribute.Usage, attribute.Secret);
                    }

                    foreach (PropertyInfo property in type.GetProperties(propertyFlags))
                    {
                        RegisterVariableAttribute attribute = Attribute.GetCustomAttribute(
                            property, typeof(RegisterVariableAttribute)) as RegisterVariableAttribute;

                        if (attribute == null) continue;

                        string variableName = attribute.Name ?? property.Name;

                        AddVariable(variableName, property);
                    }
                }

                HandleRejectedCommands(rejectedCommands);
            }
        }

        /// <summary>
        /// Parses an input line into a command and runs that command.
        /// </summary>
        public void RunCommand(string line)
        {
            Terminal.Log(line, TerminalLogType.Input);

            string remaining = line;
            IssuedErrorMessage = null;
            arguments.Clear();

            while (remaining != "")
            {
                CommandArg argument = EatArgument(ref remaining);

                if (argument.String != "")
                {
                    string variableName = argument.String.Substring(1).ToUpper();

                    arguments.Add(argument);
                }
            }

            if (arguments.Count == 0)
                // Nothing to run
                return;

            string commandName = arguments[0].String.ToUpper();
            arguments.RemoveAt(0); // Remove command name from arguments

            if (!commands.ContainsKey(commandName))
            {
                IssueErrorMessage("Command {0} could not be found", commandName);
                return;
            }

            RunCommand(commandName, arguments.ToArray());
        }

        private void RunCommand(string commandName, CommandArg[] arguments)
        {
            CommandInfo command = commands[commandName];
            int argCount = arguments.Length;
            string errorMessage = null;
            int requiredArg = 0;

            if (argCount < command.MinArgCount)
            {
                if (command.MinArgCount == command.MaxArgCount)
                    errorMessage = "exactly";
                else
                    errorMessage = "at least";
                requiredArg = command.MinArgCount;
            }
            else if (command.MaxArgCount > -1 && argCount > command.MaxArgCount)
            {
                // Do not check max allowed number of arguments if it is -1
                if (command.MinArgCount == command.MaxArgCount)
                    errorMessage = "exactly";
                else
                    errorMessage = "at most";
                requiredArg = command.MaxArgCount;
            }

            if (errorMessage != null)
            {
                string pluralFix = requiredArg == 1 ? "" : "s";

                IssueErrorMessage(
                    "{0} requires {1} {2} argument{3}",
                    commandName,
                    errorMessage,
                    requiredArg,
                    pluralFix
                );

                ShowUsage();
                return;
            }

            try
            {
                command.Proc(arguments);
            }
            catch (Exception e)
            {
                IssueErrorMessage(e.Message);
            }

            if (IssuedErrorMessage != null)
                ShowUsage();

            void ShowUsage()
            {
                if (command.Usage != null)
                    IssuedErrorMessage += string.Format("\n    -> Usage: {0}", command.Usage);
            }
        }

        public void AddCommand(string name, CommandInfo info)
        {
            name = name.ToUpper();

            if (commands.ContainsKey(name))
            {
                IssueErrorMessage("Command {0} is already defined.", name);
                return;
            }

            commands.Add(name, info);
        }

        public void AddCommand(string name, Action<CommandArg[]> proc, int minArgs = 0, int maxArgs = -1,
            string help = "", string usage = null, bool secret = false)
        {
            CommandInfo info = new CommandInfo()
            {
                Proc = proc,
                MinArgCount = minArgs,
                MaxArgCount = maxArgs,
                Help = help,
                Usage = usage,
                Secret = secret
            };

            AddCommand(name, info);
        }

        public void AddVariable(string name, PropertyInfo info)
        {
            if (!IsAllowedPropertyType(info.PropertyType))
                throw new Exception(
                    $"can't register property {info.Name} - registered variables must be string, int, float, bool or enum");

            name = name.ToUpper();

            if (variables.ContainsKey(name))
                throw new Exception($"there is already a variable called {name}");

            variables.Add(name, info);
        }

        private bool IsAllowedPropertyType(Type type)
        {
            return type == typeof(string)
                   || type == typeof(int)
                   || type == typeof(float)
                   || type == typeof(bool)
                   || type.IsEnum;
        }

        public void SetVariable(string name, string value)
        {
            SetVariable(name, new CommandArg() { String = value });
        }

        public void SetVariable(string name, CommandArg arg)
        {
            name = name.ToUpper();

            if (!variables.ContainsKey(name))
                throw new Exception($"no variable registered with name {name}");

            object value = null;

            Type propertyType = variables[name].PropertyType;

            if (propertyType == typeof(string))
                value = arg.String;
            else if (propertyType == typeof(int))
                value = arg.Int;
            else if (propertyType == typeof(float))
                value = arg.Float;
            else if (propertyType == typeof(bool))
                value = arg.Bool;
            else if (propertyType.IsEnum)
                value = Enum.Parse(propertyType, arg.String);

            variables[name].SetMethod.Invoke(null, new object[] { value });
        }

        public object GetVariable(string name)
        {
            name = name.ToUpper();

            if (!variables.ContainsKey(name))
                throw new Exception($"no variable registered with name {name}");

            return variables[name].GetMethod.Invoke(null, null);
        }

        public void IssueErrorMessage(string format, params object[] message)
        {
            IssuedErrorMessage = string.Format(format, message);
        }

        private string InferCommandName(string methodName)
        {
            string commandName;
            int index = methodName.IndexOf("COMMAND", StringComparison.CurrentCultureIgnoreCase);

            if (index >= 0)
                // Method is prefixed, suffixed with, or contains "COMMAND".
                commandName = methodName.Remove(index, 7);
            else
                commandName = methodName;

            return commandName;
        }

        private string InferFrontCommandName(string methodName)
        {
            int index = methodName.IndexOf("FRONT", StringComparison.CurrentCultureIgnoreCase);
            return index >= 0 ? methodName.Remove(index, 5) : null;
        }

        private void HandleRejectedCommands(Dictionary<string, CommandInfo> rejectedCommands)
        {
            foreach (KeyValuePair<string, CommandInfo> command in rejectedCommands)
                if (commands.ContainsKey(command.Key))
                    commands[command.Key] = new CommandInfo()
                    {
                        Proc = commands[command.Key].Proc,
                        MinArgCount = command.Value.MinArgCount,
                        MaxArgCount = command.Value.MaxArgCount,
                        Help = command.Value.Help
                    };
                else
                    IssueErrorMessage("{0} is missing a front command.", command);
        }

        private CommandInfo CommandFromParamInfo(ParameterInfo[] parameters, string help)
        {
            int optionalArgs = 0;

            foreach (ParameterInfo param in parameters)
                if (param.IsOptional)
                    optionalArgs += 1;

            return new CommandInfo()
            {
                Proc = null,
                MinArgCount = parameters.Length - optionalArgs,
                MaxArgCount = parameters.Length,
                Help = help
            };
        }

        private CommandArg EatArgument(ref string s)
        {
            CommandArg arg = new CommandArg();
            int spaceIndex = s.IndexOf(' ');

            if (spaceIndex >= 0)
            {
                arg.String = s.Substring(0, spaceIndex);
                s = s.Substring(spaceIndex + 1); // Remaining
            }
            else
            {
                arg.String = s;
                s = "";
            }

            return arg;
        }
    }
}
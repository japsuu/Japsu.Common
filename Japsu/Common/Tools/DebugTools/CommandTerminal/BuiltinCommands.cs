using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace Japsu.Common.DebugTools.CommandTerminal
{
    public static class BuiltinCommands
    {
        [RegisterCommand(Name = "Clear", Help = "Clear the command console", MaxArgCount = 0)]
        private static void CommandClear(CommandArg[] args)
        {
            Terminal.Buffer.Clear();
        }

        [RegisterCommand(Name = "Help", Help = "Display help information about a command", MaxArgCount = 1)]
        private static void CommandHelp(CommandArg[] args)
        {
            if (args.Length == 0)
            {
                foreach (KeyValuePair<string, CommandInfo> command in Terminal.Shell.Commands)
                    if (!command.Value.Secret)
                        Terminal.Log("{0}: {1}", command.Key.PadRight(16), command.Value.Help);
                return;
            }

            string commandName = args[0].String.ToUpper();

            if (!Terminal.Shell.Commands.ContainsKey(commandName))
            {
                Terminal.Shell.IssueErrorMessage("Command {0} could not be found.", commandName);
                return;
            }

            CommandInfo info = Terminal.Shell.Commands[commandName];

            if (info.Help == null)
                Terminal.Log("{0} does not provide any help documentation.", commandName);
            else if (info.Usage == null)
                Terminal.Log(info.Help);
            else
                Terminal.Log("{0}\nUsage: {1}", info.Help, info.Usage);
        }

        [RegisterCommand(Name = "Time", Help = "Measure the execution time of a command", MinArgCount = 1)]
        private static void CommandTime(CommandArg[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Terminal.Shell.RunCommand(JoinArguments(args));

            sw.Stop();
            Terminal.Log("Time: {0}ms", (double)sw.ElapsedTicks / 10000);
        }

        [RegisterCommand(Name = "Schedule", Help = "Schedule a command to be executed some time in the future",
            MinArgCount = 2,
            Usage = "schedule [delay] [command] - delay is in seconds")]
        private static void CommandSchedule(CommandArg[] args)
        {
            Terminal.RunCommandAfterDelay(args[0].Float, JoinArguments(args, 1), true);
        }

        [RegisterCommand(Name = "ScheduleUnscaled", Help = "Schedule a command using the time scale", MinArgCount = 2,
            Usage = "schedule [delay] [command] - delay is in seconds")]
        private static void CommandScheduleUnScaled(CommandArg[] args)
        {
            Terminal.RunCommandAfterDelay(args[0].Float, JoinArguments(args, 1), false);
        }

        [RegisterCommand(Name = "Print", Help = "Output message")]
        private static void CommandPrint(CommandArg[] args)
        {
            Terminal.Log(JoinArguments(args));
        }

#if DEBUG
        [RegisterCommand(Name = "Trace", Help = "Output the stack trace of the previous message", MaxArgCount = 0)]
        private static void CommandTrace(CommandArg[] args)
        {
            int logCount = Terminal.Buffer.Logs.Count;

            if (logCount - 2 < 0)
            {
                Terminal.Log("Nothing to trace.");
                return;
            }

            LogItem logItem = Terminal.Buffer.Logs[logCount - 2];

            if (logItem.StackTrace == "")
                Terminal.Log("{0} (no trace)", logItem.Message);
            else
                Terminal.Log(logItem.StackTrace);
        }
#endif

        [RegisterCommand(Name = "Set", Help = "List all variables or set a variable value")]
        private static void CommandSet(CommandArg[] args)
        {
            if (args.Length == 0)
            {
                foreach (string v in Terminal.Shell.Variables)
                    Terminal.Log("{0}: {1}", v.PadRight(16), Terminal.Shell.GetVariable(v));
                return;
            }

            string variableName = args[0].String;

            try
            {
                Terminal.Shell.SetVariable(variableName, JoinArguments(args, 1));
            }
            catch (Exception e)
            {
                throw e?.InnerException ?? e;
            }
        }

        [RegisterCommand(Name = "Bind", Help = "Bind a key to a command", MinArgCount = 2,
            Usage =
                "bind [keycode] [command] - see https://docs.unity3d.com/ScriptReference/KeyCode.html for a list of valid keycodes")]
        private static void CommandBind(CommandArg[] args)
        {
            string fullCommand = JoinArguments(args, 1);
            Terminal.AddBinding(args[0].AsEnum<KeyCode>(), fullCommand);
        }

        [RegisterCommand(Name = "Unbind", Help = "Remove all bindings from a key", MinArgCount = 1, MaxArgCount = 1,
            Usage =
                "unbind [keycode] - see https://docs.unity3d.com/ScriptReference/KeyCode.html for a list of valid keycodes")]
        private static void CommandUnbind(CommandArg[] args)
        {
            Terminal.ResetBinding(args[0].AsEnum<KeyCode>());
        }

        [RegisterCommand(Name = "Screenshot",
            Help = "Save a screenshot of the game. You probably want to bind this to a key", MaxArgCount = 2,
            Usage = "screenshot [supersize] [file name or path]")]
        private static void CommandScreenshot(CommandArg[] args)
        {
            string filePath = Path.Combine(Application.persistentDataPath, "screenshots",
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"));
            int superSize = 1;

            if (args.Length > 0)
                superSize = args[0].Int;

            if (args.Length > 1)
            {
                string name = args[1].String;
                if (Path.IsPathRooted(name))
                    filePath = name;
                else
                    filePath = Path.Combine(Application.persistentDataPath, "screenshots", name);
            }

            filePath = filePath.Replace('\\',
                '/'); // this is mostly so that the Terminal.Log message looks consistent on Windows
            filePath = Path.ChangeExtension(filePath, ".png");
            DirectoryInfo directoryInfo = new DirectoryInfo(filePath).Parent;
            if (directoryInfo != null)
            {
                string folderPath = directoryInfo.FullName;
                Directory.CreateDirectory(folderPath);
            }

            ScreenCapture.CaptureScreenshot(filePath, superSize);
            Terminal.Log($"saved screenshot as {filePath} (supersize {superSize})");
        }

        [RegisterCommand(Name = "Noop", Help = "No operation")]
        private static void CommandNoop(CommandArg[] args)
        {
        }

        [RegisterCommand(Name = "Quit", Secret = true)]
        private static void CommandQuit(CommandArg[] args)
        {
            CommandExit(args);
        }

        [RegisterCommand(Name = "Exit", Help = "Quit running application. 'exit' also works.", MaxArgCount = 0)]
        private static void CommandExit(CommandArg[] args)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static string JoinArguments(CommandArg[] args, int start = 0)
        {
            StringBuilder sb = new StringBuilder();
            int argLength = args.Length;

            for (int i = start; i < argLength; i++)
            {
                sb.Append(args[i].String);

                if (i < argLength - 1) sb.Append(" ");
            }

            return sb.ToString();
        }
    }
}
using System;
using System.Linq;

namespace Japsu.Common.Tools.DebugTools.CommandTerminal
{
    public struct CommandArg
    {
        public string String { get; set; }

        public int Int
        {
            get
            {
                int intValue;

                if (int.TryParse(String, out intValue)) return intValue;

                TypeError("int");
                return 0;
            }
        }

        public float Float
        {
            get
            {
                float floatValue;

                if (float.TryParse(String, out floatValue)) return floatValue;

                TypeError("float");
                return 0;
            }
        }

        private static readonly string[] TrueStrings = { "true", "yes", "y", "on" };
        private static readonly string[] FalseStrings = { "false", "no", "n", "off" };

        public bool Bool
        {
            get
            {
                if (TrueStrings.Contains(String.ToLower()))
                    return true;

                if (FalseStrings.Contains(String.ToLower()))
                    return false;

                TypeError("bool");
                return false;
            }
        }

        public T AsEnum<T>() where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new Exception(
                    $"type {typeof(T).FullName} is not an enum - you can't read the CommandArg this way");

            if (Enum.TryParse(String, true, out T value))
            {
                return value;
            }
            else
            {
                TypeError(typeof(T).FullName);
                throw new Exception($"value {String} not found in enumerated type {typeof(T).FullName}");
            }
        }

        public override string ToString()
        {
            return String;
        }

        private void TypeError(string expectedType)
        {
            Terminal.Shell.IssueErrorMessage(
                "Incorrect type for {0}, expected <{1}>",
                String, expectedType
            );
        }
    }
}
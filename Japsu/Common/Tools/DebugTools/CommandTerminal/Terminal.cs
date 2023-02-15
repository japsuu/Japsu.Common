using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace Japsu.Common.DebugTools.CommandTerminal
{
    public enum TerminalState
    {
        Close,
        OpenSmall,
        OpenFull
    }

    public class Terminal : MonoBehaviour
    {
        [Header("Window")]
        [Range(0, 1)]
        [SerializeField]
        private float MaxHeight = 0.7f;

        [SerializeField] [Range(0, 1)] private float SmallTerminalRatio = 0.33f;

        [Range(100, 1000)] [SerializeField] private float ToggleSpeed = 360;

        [SerializeField] private string ToggleHotkey = "`";
        [SerializeField] private string ToggleFullHotkey = "#`";
        [SerializeField] private int BufferSize = 512;

        [Header("Input")] [SerializeField] private Font ConsoleFont;
        [SerializeField] private int ConsoleFontSize;
        [SerializeField] private string InputCaret = ">";
        [SerializeField] private bool ShowGUIButtons;
        [SerializeField] private bool RightAlignButtons;

        [Header("Theme")]
        [Range(0, 1)]
        [SerializeField]
        private float InputContrast;

        [Range(0, 1)] [SerializeField] private float InputAlpha = 0.5f;

        [SerializeField] private Color BackgroundColor = Color.black;
        [SerializeField] private Color ForegroundColor = Color.white;
        [SerializeField] private Color ShellColor = Color.white;
        [SerializeField] private Color InputColor = Color.cyan;
        [SerializeField] private Color WarningColor = Color.yellow;
        [SerializeField] private Color ErrorColor = Color.red;

        private TerminalState state;
        private TextEditor editor_state;
        private bool input_fix;
        private bool move_cursor;
        private bool initial_open; // Used to focus on TextField when console opens
        private Rect window;
        private float current_open_t;
        private float open_target;
        private float real_window_size;
        private string command_text;
        private string cached_command_text;
        private Vector2 scroll_position;
        private GUIStyle window_style;
        private GUIStyle label_style;
        private GUIStyle input_style;
        private Texture2D background_texture;
        private Texture2D input_background_texture;

        public static void BottomOutScrollbar()
        {
            instance.scroll_position.y = int.MaxValue;
        }

        public static TerminalLog Buffer { get; private set; }
        public static CommandShell Shell { get; private set; }
        public static TerminalHistory History { get; private set; }
        public static TerminalAutocomplete Autocomplete { get; private set; }

        public static bool IssuedError => Shell.IssuedErrorMessage != null;

        public bool IsClosed => state == TerminalState.Close && Mathf.Approximately(current_open_t, open_target);

        public static void Log(string message, TerminalLogType type = TerminalLogType.Message)
        {
            Log(type, "{0}", message);
        }

        public static void Log(string format, params object[] message)
        {
            Log(TerminalLogType.ShellMessage, format, message);
        }

        public static void Log(TerminalLogType type, string format, params object[] message)
        {
            string text = string.Format(format, message);
            Buffer.HandleLog(text, type);
            BottomOutScrollbar();
            History.Push(text);
        }

        private CursorLockMode previousCursorLockState;
        private bool previousCursorVisible;

        private void OnTerminalOpen()
        {
            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            WhenTerminalOpens?.Invoke();
        }

        private void OnTerminalClose()
        {
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
            WhenTerminalCloses?.Invoke();
        }

        public static event System.Action WhenTerminalOpens;
        public static event System.Action WhenTerminalCloses;


        public void SetState(TerminalState newState)
        {
            input_fix = true;
            cached_command_text = command_text;
            command_text = "";

            switch (newState)
            {
                case TerminalState.Close:
                {
                    open_target = 0;
                    OnTerminalClose();
                    break;
                }
                case TerminalState.OpenSmall:
                {
                    open_target = Screen.height * MaxHeight * SmallTerminalRatio;
                    if (current_open_t > open_target)
                    {
                        // Prevent resizing from OpenFull to OpenSmall if window y position
                        // is greater than OpenSmall's target
                        open_target = 0;
                        state = TerminalState.Close;
                        OnTerminalClose();
                        return;
                    }

                    real_window_size = open_target;
                    BottomOutScrollbar();
                    OnTerminalOpen();
                    break;
                }
                case TerminalState.OpenFull:
                default:
                {
                    real_window_size = Screen.height * MaxHeight;
                    open_target = real_window_size;
                    OnTerminalOpen();
                    break;
                }
            }

            state = newState;
        }

        public void ToggleState(TerminalState newState)
        {
            if (state == newState)
                SetState(TerminalState.Close);
            else
                SetState(newState);
        }

        private void OnEnable()
        {
            Buffer = new TerminalLog(BufferSize);
            Shell = new CommandShell();
            History = new TerminalHistory();
            Autocomplete = new TerminalAutocomplete();

            // Hook Unity log events
            Application.logMessageReceived += HandleUnityLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleUnityLog;
        }

        private void Start()
        {
            if (ConsoleFont == null)
            {
                ConsoleFont = Font.CreateDynamicFontFromOSFont("Courier New", 16);
                UnityEngine.Debug.LogWarning(
                    "Please assign a font to the command terminal - using OS font Courier New for now.");
            }

            if (ConsoleFontSize == 0)
                ConsoleFontSize = ConsoleFont.fontSize;

            command_text = "";
            cached_command_text = command_text;
            Assert.AreNotEqual(ToggleHotkey.ToLower(), "return", "Return is not a valid ToggleHotkey");

            SetupWindow();
            SetupInput();
            SetupLabels();

            Shell.RegisterCommandsAndVariables();

            if (IssuedError) Log(TerminalLogType.Error, "Error: {0}", Shell.IssuedErrorMessage);

            foreach (KeyValuePair<string, CommandInfo> command in Shell.Commands)
                if (!command.Value.Secret)
                    Autocomplete.Register(command.Key);

            RunStartupCommands();
        }

        private void OnGUI()
        {
            if (Event.current.Equals(Event.KeyboardEvent(ToggleHotkey)))
            {
                SetState(TerminalState.OpenSmall);
                initial_open = true;
            }
            else if (Event.current.Equals(Event.KeyboardEvent(ToggleFullHotkey)))
            {
                SetState(TerminalState.OpenFull);
                initial_open = true;
            }

            if (ShowGUIButtons) DrawGUIButtons();

            if (IsClosed) return;

            HandleOpenness();
            window = GUILayout.Window(88, window, DrawConsole, "", window_style);
        }

        private void SetupWindow()
        {
            real_window_size = Screen.height * MaxHeight / 3;
            window = new Rect(0, current_open_t - real_window_size, Screen.width, real_window_size);

            // Set background color
            background_texture = new Texture2D(1, 1);
            background_texture.SetPixel(0, 0, BackgroundColor);
            background_texture.Apply();

            window_style = new GUIStyle();
            window_style.normal.background = background_texture;
            window_style.padding = new RectOffset(4, 4, 4, 4);
            window_style.normal.textColor = ForegroundColor;
            window_style.font = ConsoleFont;
            window_style.fontSize = ConsoleFontSize;
        }

        private void SetupLabels()
        {
            label_style = new GUIStyle();
            label_style.font = ConsoleFont;
            label_style.fontSize = ConsoleFontSize;
            label_style.normal.textColor = ForegroundColor;
            label_style.wordWrap = true;
        }

        private void SetupInput()
        {
            input_style = new GUIStyle();
            input_style.padding = new RectOffset(4, 4, 4, 4);
            input_style.font = ConsoleFont;
            input_style.fontSize = ConsoleFontSize;
            input_style.fixedHeight = ConsoleFontSize * 1.6f;
            input_style.normal.textColor = InputColor;

            Color darkBackground = new Color();
            darkBackground.r = BackgroundColor.r - InputContrast;
            darkBackground.g = BackgroundColor.g - InputContrast;
            darkBackground.b = BackgroundColor.b - InputContrast;
            darkBackground.a = InputAlpha;

            input_background_texture = new Texture2D(1, 1);
            input_background_texture.SetPixel(0, 0, darkBackground);
            input_background_texture.Apply();
            input_style.normal.background = input_background_texture;
        }

        private void DrawConsole(int window2D)
        {
            GUILayout.BeginVertical();

            scroll_position = GUILayout.BeginScrollView(scroll_position, false, false, GUIStyle.none, GUIStyle.none);
            GUILayout.FlexibleSpace();
            DrawLogs();
            GUILayout.EndScrollView();

            if (move_cursor)
            {
                CursorToEnd();
                move_cursor = false;
            }

            if (Event.current.Equals(Event.KeyboardEvent("escape")))
            {
                SetState(TerminalState.Close);
            }
            else if (Event.current.Equals(Event.KeyboardEvent("return")) // keboard enter
                     || Event.current.Equals(Event.KeyboardEvent("[enter]")))
            {
                // numpad enter
                EnterCommand();
            }
            else if (Event.current.Equals(Event.KeyboardEvent("up")))
            {
                command_text = History.Previous();
                move_cursor = true;
            }
            else if (Event.current.Equals(Event.KeyboardEvent("down")))
            {
                command_text = History.Next();
            }
            else if (Event.current.Equals(Event.KeyboardEvent(ToggleHotkey)))
            {
                ToggleState(TerminalState.OpenSmall);
            }
            else if (Event.current.Equals(Event.KeyboardEvent(ToggleFullHotkey)))
            {
                ToggleState(TerminalState.OpenFull);
            }
            else if (Event.current.Equals(Event.KeyboardEvent("tab")))
            {
                CompleteCommand();
                move_cursor = true; // Wait till next draw call
            }

            GUILayout.BeginHorizontal();

            if (InputCaret != "") GUILayout.Label(InputCaret, input_style, GUILayout.Width(ConsoleFont.fontSize));

            GUI.SetNextControlName("command_text_field");
            command_text = GUILayout.TextField(command_text, input_style);

            if (input_fix && command_text.Length > 0)
            {
                command_text = cached_command_text; // Otherwise the TextField picks up the ToggleHotkey character event
                input_fix = false; // Prevents checking string Length every draw call
            }

            if (initial_open)
            {
                GUI.FocusControl("command_text_field");
                initial_open = false;
            }

            if (ShowGUIButtons && GUILayout.Button("| run", input_style, GUILayout.Width(Screen.width / 10)))
                EnterCommand();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawLogs()
        {
            foreach (LogItem log in Buffer.Logs)
            {
                label_style.normal.textColor = GetLogColor(log.Type);
                GUILayout.Label(log.Message, label_style);
            }
        }

        private void DrawGUIButtons()
        {
            int size = ConsoleFont.fontSize;
            float xPosition = RightAlignButtons ? Screen.width - 7 * size : 0;

            // 7 is the number of chars in the button plus some padding, 2 is the line height.
            // The layout will resize according to the font size.
            GUILayout.BeginArea(new Rect(xPosition, current_open_t, 7 * size, size * 2));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Small", window_style))
                ToggleState(TerminalState.OpenSmall);
            else if (GUILayout.Button("Full", window_style)) ToggleState(TerminalState.OpenFull);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void HandleOpenness()
        {
            float dt = ToggleSpeed * Time.unscaledDeltaTime;

            if (current_open_t < open_target)
            {
                current_open_t += dt;
                if (current_open_t > open_target) current_open_t = open_target;
            }
            else if (current_open_t > open_target)
            {
                current_open_t -= dt;
                if (current_open_t < open_target) current_open_t = open_target;
            }
            else
            {
                if (input_fix) input_fix = false;
                return; // Already at target
            }

            window = new Rect(0, current_open_t - real_window_size, Screen.width, real_window_size);
        }

        private void EnterCommand()
        {
            Shell.RunCommand(command_text);

            if (IssuedError) Log(TerminalLogType.Error, "Error: {0}", Shell.IssuedErrorMessage);

            command_text = "";
        }

        private void CompleteCommand()
        {
            string headText = command_text;
            int formatWidth = 0;

            string[] completionBuffer = Autocomplete.Complete(ref headText, ref formatWidth);
            int completionLength = completionBuffer.Length;

            if (completionLength != 0) command_text = headText;

            if (completionLength > 1)
            {
                // Print possible completions
                StringBuilder logBuffer = new StringBuilder();

                foreach (string completion in completionBuffer)
                    logBuffer.Append(completion.PadRight(formatWidth + 4));

                Log("{0}", logBuffer);
                BottomOutScrollbar();
            }
        }

        private void CursorToEnd()
        {
            if (editor_state == null)
                editor_state = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);

            editor_state.MoveCursorToPosition(new Vector2(999, 999));
        }

        public static bool LogUnityMessages = true;

        private void HandleUnityLog(string message, string stackTrace, LogType type)
        {
            if (!LogUnityMessages) return;

            Buffer.HandleLog(message, stackTrace, (TerminalLogType)type);
            BottomOutScrollbar();
        }

        private Color GetLogColor(TerminalLogType type)
        {
            switch (type)
            {
                case TerminalLogType.Message: return ForegroundColor;
                case TerminalLogType.Warning: return WarningColor;
                case TerminalLogType.Input: return InputColor;
                case TerminalLogType.ShellMessage: return ShellColor;
                default: return ErrorColor;
            }
        }


        private static Dictionary<KeyCode, List<string>> boundCommands = new();

        public static void AddBinding(KeyCode key, string command)
        {
            if (!boundCommands.ContainsKey(key))
                boundCommands.Add(key, new List<string>());

            boundCommands[key].Add(command);
        }

        public static void ResetBinding(KeyCode key)
        {
            boundCommands.Remove(key);
        }

        private void Update()
        {
            foreach (KeyValuePair<KeyCode, List<string>> v in boundCommands)
                if (Input.GetKeyDown(v.Key))
                    foreach (string c in v.Value)
                        Shell.RunCommand(c);
        }

        private static void RunStartupCommands()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return;

            const string fileName = "StartupCommands.txt";

            // In editor, this refers to the directory which contains the assets folder.
            // In stanalone, this refers to the directory which contains the executable.
            string directory = Directory.GetParent(Application.dataPath).FullName;
            string filepath = Path.Combine(directory, fileName);
            if (File.Exists(filepath))
            {
                string[] lines = File.ReadAllLines(filepath);
                for (int i = 0; i < lines.Length; i++)
                    if (!lines[i].StartsWith("#") && !string.IsNullOrWhiteSpace(lines[i]))
                        Shell.RunCommand(lines[i]);
            }
            else
            {
                File.WriteAllText(filepath,
                    @"# each line of this file that doesn't begin with # will be run as a command when the game starts.
# If you are the developer and you include important stuff here, make sure to include it with your builds. It is not automatically added.");
            }
        }

        private static Terminal instance;

        private void Awake()
        {
            instance = this;
        }

        private static IEnumerator RunCommandAfterDelayRoutine(float seconds, string command, bool scaledTime)
        {
            if (scaledTime)
                yield return new WaitForSeconds(seconds);
            else
                yield return new WaitForSecondsRealtime(seconds);

            Shell.RunCommand(command);
        }

        public static void RunCommandAfterDelay(float seconds, string command, bool scaledTime)
        {
            instance.StartCoroutine(RunCommandAfterDelayRoutine(seconds, command, scaledTime));
        }
    }
}
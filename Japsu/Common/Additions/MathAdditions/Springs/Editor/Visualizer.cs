using System;
using System.Linq;
using System.Reflection;
using Japsu.Common.MathAdditions.Springs.Runtime;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUI;

namespace Japsu.Common.Math.Springs.Editor
{
    public class Visualizer : EditorWindow
    {
        // Graph
        private static readonly Rect Box = new(100, 100, 1020, 500);
        private readonly int graphOffsetY = 65;
        private readonly int cellSize = 20;
        private readonly int offset = 10;
        private readonly Color gridColor = new(0.5f, 0.5f, 0.5f, 0.1f);
        private readonly Color axisColor = Color.gray;
        private float GridSizeX => Box.width - offset * 2;
        private float GridSizeY => Box.height - offset * 2 - graphOffsetY;
        private int GridCountX => Mathf.CeilToInt(GridSizeX / cellSize);
        private int GridCountY => Mathf.CeilToInt(GridSizeY / cellSize);
        private int AxisY => GridCountY / 2;

        // Spring Types
        private readonly Type[] springTypes =
        {
            typeof(ClosedFormSpring),
            typeof(SemiImplicitEulerSpring),
            typeof(ExplicitRk4Spring),
            typeof(VerletIntegrationSpring)
        };

        private string[] SpringTypeOptions => springTypes
            .Select(t => t.FullName)
            .Select(s => s.Split('.')[1])
            .ToArray();

        private Type currentType = typeof(ClosedFormSpring);

        private readonly (float damping, float startValue, float endValue, float initialVelocity)[] dataset =
        {
            (26, 10, 0, 0), // critically damped
            (5, 10, 0, 0), // under damped
            (100, 10, 0, 0), // over damped
            (5, 0, 0, 100) // under damped with initial velocity
        };

        private readonly Color[] colors =
        {
            Color.red, // critically damped
            Color.cyan, // under damped
            Color.yellow, // over damped
            Color.magenta // under damped with initial velocity
        };

        // caches
        private SpringBase[] springs;
        private FieldInfo stepSizeField;
        private int stepSizeFps;
        private int fps = 60;
        private float graphTime = 2f;
        private float damping;
        private float mass;
        private float stiffness;
        private float startValue;
        private float endValue;
        private float initialVelocity;

        private readonly string[] graphModes = { "Presets", "Custom" };
        private int graphModeIndex = 0;

        [MenuItem("Tools/UnitySpring/Visualizer")]
        private static void ShowWindow()
        {
            GetWindowWithRect<Visualizer>(Box, true, "Unity Spring Visualizer", true);
        }

        private void OnEnable()
        {
            SetupPresetSprings();
        }

        private void SetupPresetSprings()
        {
            springs = dataset.Select(d =>
            {
                SpringBase spring = Activator.CreateInstance(currentType) as SpringBase;
                spring.damping = d.damping;
                spring.startValue = d.startValue;
                spring.endValue = d.endValue;
                spring.initialVelocity = d.initialVelocity;
                return spring;
            }).ToArray();

            SetupStepSize(springs[0]);
        }

        private void SetupCustomSpring()
        {
            SpringBase spring = Activator.CreateInstance(currentType) as SpringBase;
            damping = spring.damping;
            mass = spring.mass;
            stiffness = spring.stiffness;
            startValue = spring.startValue = 10;
            endValue = spring.endValue = 0;
            initialVelocity = spring.initialVelocity = 0;
            springs = new SpringBase[] { spring };

            SetupStepSize(spring);
        }

        private void SetupStepSize(SpringBase spring)
        {
            stepSizeField = currentType.GetField("stepSize", BindingFlags.NonPublic | BindingFlags.Instance);
            if (stepSizeField != null) stepSizeFps = Mathf.CeilToInt(1f / (float)stepSizeField.GetValue(spring));
        }

        private void UpdateCustomSpring()
        {
            SpringBase spring = springs[0];
            spring.damping = damping;
            spring.mass = mass;
            spring.stiffness = stiffness;
            spring.startValue = startValue;
            spring.endValue = endValue;
            spring.initialVelocity = initialVelocity;
        }

        private void OnGUI()
        {
            DrawController();
            DrawGrid();
            PlotGraph();
        }

        private void DrawGrid()
        {
            for (int x = 0; x < GridCountX + 1; x++)
            {
                Color color = x == 0 ? axisColor : gridColor;
                DrawLine(x, 0, 1, GridCountY * cellSize, color);
            }

            for (int y = 0; y < GridCountY + 1; y++)
            {
                Color color = y == AxisY ? axisColor : gridColor;
                DrawLine(0, y, GridCountX * cellSize, 1, color);
            }

            for (float t = 0f; t < graphTime; t++)
            {
                float x = t / (graphTime / GridSizeX) / cellSize;
                DrawHatchMark(x, AxisY, t);
            }

            void DrawLine(float x, float y, float w, float h, Color c)
            {
                x = x * cellSize + offset - 0.5f;
                y = y * cellSize + offset - 0.5f + graphOffsetY;
                DrawRect(new Rect(x, y, w, h), c);
            }

            void DrawHatchMark(float x, float y, float unit)
            {
                x = x * cellSize + offset - 0.5f;
                y = y * cellSize + offset + -0.5f + graphOffsetY;
                DrawRect(new Rect(x, y - 10, 1, 21), axisColor);
                GUI.Label(new Rect(x + 2, y + 6, 10, 10), unit.ToString());
            }
        }

        private void PlotGraph()
        {
            float step = 1f / fps;
            float dt = step / (graphTime / GridSizeX);

            foreach (SpringBase s in springs) s.Reset();

            // start values
            for (int i = 0; i < springs.Length; i++) DrawPoint(0, springs[i].startValue, colors[i]);

            // draw until end of axis x
            float t = dt;
            while (t < GridSizeX)
            {
                for (int i = 0; i < springs.Length; i++) DrawPoint(t, springs[i].Evaluate(step), colors[i]);
                t += dt;
            }

            void DrawPoint(float x, float y, Color c)
            {
                int n = Mathf.FloorToInt(dt / 2);
                n = Mathf.Clamp(n, 1, 5);
                DrawRect(
                    new Rect(
                        x + offset - 0.5f * n,
                        (AxisY - y) * cellSize + offset - 0.5f * n + graphOffsetY,
                        n,
                        n
                    ),
                    c
                );
            }
        }

        private void DrawController()
        {
            EditorGUIUtility.labelWidth = 70;
            GUILayoutOption sliderWidth = GUILayout.Width(230);

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);

                // spring types
                BeginChangeCheck();
                {
                    int index = Array.IndexOf(springTypes, currentType);
                    index = EditorGUILayout.Popup("Spring Type:", index, SpringTypeOptions);
                    currentType = springTypes[index];
                }
                if (EndChangeCheck())
                {
                    if (graphModeIndex == 0)
                        SetupPresetSprings();
                    else
                        SetupCustomSpring();
                }

                GUILayout.Space(10);

                // fps
                fps = EditorGUILayout.IntSlider("FPS:", fps, 10, 120, sliderWidth);

                GUILayout.Space(10);

                // step size
                if (stepSizeField != null)
                {
                    BeginChangeCheck();
                    {
                        stepSizeFps = EditorGUILayout.IntSlider("Step FPS:", stepSizeFps, fps, 120, sliderWidth);
                    }
                    if (EndChangeCheck())
                        foreach (SpringBase s in springs)
                            stepSizeField.SetValue(s, 1f / stepSizeFps);
                }

                GUILayout.Space(10);

                // graph time
                graphTime = EditorGUILayout.Slider("Graph Time:", graphTime, 0.1f, 5f, sliderWidth);

                GUILayout.Space(10);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);

                // graph mode
                BeginChangeCheck();
                {
                    graphModeIndex = EditorGUILayout.Popup("Modes:", graphModeIndex, graphModes);
                }
                if (EndChangeCheck())
                {
                    if (graphModeIndex == 0)
                        SetupPresetSprings();
                    else
                        SetupCustomSpring();
                }

                // custom spring parameters
                BeginDisabledGroup(graphModeIndex == 0);
                BeginChangeCheck();
                {
                    GUILayout.Space(10);
                    EditorGUIUtility.labelWidth = 100;
                    startValue = EditorGUILayout.FloatField("Start Value:", startValue);
                    GUILayout.Space(10);
                    endValue = EditorGUILayout.FloatField("End Value:", endValue);
                    GUILayout.Space(10);
                    initialVelocity = EditorGUILayout.FloatField("Initial Velocity:", initialVelocity);
                    GUILayout.Space(10);
                }
                if (EndChangeCheck()) UpdateCustomSpring();
                EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                // custom spring parameters
                BeginDisabledGroup(graphModeIndex == 0);
                BeginChangeCheck();
                {
                    EditorGUIUtility.labelWidth = 70;
                    GUILayout.Space(10);
                    damping = EditorGUILayout.Slider("Damping:", damping, 0f, 100f);
                    GUILayout.Space(10);
                    mass = EditorGUILayout.Slider("Mass:", mass, 0f, 100f);
                    GUILayout.Space(10);
                    stiffness = EditorGUILayout.Slider("Stiffness:", stiffness, 0f, 200f);
                    GUILayout.Space(10);
                }
                if (EndChangeCheck()) UpdateCustomSpring();
                EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 0;
        }
    }
}
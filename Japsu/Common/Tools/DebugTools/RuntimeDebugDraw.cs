using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Japsu.Common.DebugTools
{
    /*
 *	Runtime Debug Draw
 *	Single file debuging DrawLine/DrawText/etc that works in both Scene/Game view, also works in built PC/mobile builds.
 *	
 *	Very Important Notes:
 *	1.	You are expected to make some changes in this file before intergrating this into you projects.
 *			a.	`_DEBUG` symbol, you should this to your project's debugging symbol so these draw calls will be compiled away in final release builds.
 *				If you forget to do this, DrawXXX calls won't be shown.
 *			b.	`RuntimeDebugDraw` namespace and `Draw` class name, you can change this into your project's namespace to make it more accessable.
 *			c.	`Draw.DrawLineLayer` is the layer the lines will be drawn on. If you have camera postprocessing turned on, set this to a layer that is ignored
 *				by the post processor.
 *			d.	`GetDebugDrawCamera()` will be called to get the camera for line drawings and text coordinate calcuation.
 *				It defaults to `Camera.main`, returning null will mute drawings.
 *			e.	`DrawTextDefaultSize`/`DrawDefaultColor` styling variables, defaults as Unity Debug.Draw.
 *	2.	Performance should be relatively ok for debugging,  but it's never intended for release use. You should use conditional to
 *		compile away these calls anyway. Additionally DrawText is implemented with OnGUI, which costs a lot on mobile devices.
 *	3.	Don't rename this file of 'RuntimeDebugDraw' or this won't work. This file contains a MonoBehavior also named 'RuntimeDebugDraw' and Unity needs this file
 *		to have the same name. If you really want to rename this file, remember to rename the 'RuntimeDebugDraw' class below too.
 *	
 *	License: Public Domain
 */

    public static class Draw
    {
        #region Main Functions

        /// <summary>
        /// Which layer the lines will be drawn on.
        /// </summary>
        public const int DRAW_LINE_LAYER = 4;

        /// <summary>
        /// Default font size for DrawText.
        /// </summary>
        private const int DRAW_TEXT_DEFAULT_SIZE = 12;

        /// <summary>
        /// Default color for Draws.
        /// </summary>
        private static readonly Color DrawDefaultColor = Color.white;

        /// <summary>
        ///	Which camera to use for line drawing and texts coordinate calculation.
        /// </summary>
        /// <returns>Camera to debug draw on, returns null will mute debug drawing.</returns>
        public static Camera GetDebugDrawCamera()
        {
            return Camera.main;
        }

        /// <summary>
        ///	Draw a line from <paramref name="start"/> to <paramref name="end"/> with <paramref name="color"/>.
        /// </summary>
        /// <param name="start">Point in world space where the line should start.</param>
        /// <param name="end">Point in world space where the line should end.</param>
        /// <param name="color">Color of the line.</param>
        /// <param name="duration">How long the line should be visible for.</param>
        /// <param name="depthTest">Should the line be obscured by objects closer to the camera?</param>
        [Conditional("_DEBUG")]
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest)
        {
            CheckAndBuildHiddenRTDrawObject();
            rtDraw.RegisterLine(start, end, color, duration, !depthTest);
        }

        /// <summary>
        /// Draws a line from start to start + dir in world coordinates.
        /// </summary>
        /// <param name="start">Point in world space where the ray should start.</param>
        /// <param name="dir">Direction and length of the ray.</param>
        /// <param name="color">Color of the drawn line.</param>
        /// <param name="duration">How long the line will be visible for (in seconds).</param>
        /// <param name="depthTest">Should the line be obscured by other objects closer to the camera?</param>
        [Conditional("_DEBUG")]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration, bool depthTest)
        {
            CheckAndBuildHiddenRTDrawObject();
            rtDraw.RegisterLine(start, start + dir, color, duration, !depthTest);
        }

        /// <summary>
        /// Draw a text at given position.
        /// </summary>
        /// <param name="pos">Position</param>
        /// <param name="text">String of the text.</param>
        /// <param name="color">Color for the text.</param>
        /// <param name="size">Font size for the text.</param>
        /// <param name="duration">How long the text should be visible for.</param>
        /// <param name="popUp">Set to true to let the text moving up, so multiple texts at the same position can be visible.</param>
        [Conditional("_DEBUG")]
        public static void DrawText(Vector3 pos, string text, Color color, int size, float duration, bool popUp)
        {
            CheckAndBuildHiddenRTDrawObject();
            rtDraw.RegisterDrawText(pos, text, color, size, duration, popUp);
        }

        /// <summary>
        /// Attach text to a transform.
        /// </summary>
        /// <param name="transform">Target transform to attach text to.</param>
        /// <param name="strFunc">Function will be called on every frame to get a string as attached text. </param>
        /// <param name="offset">Text attach offset to transform position.</param>
        /// <param name="color">Color for the text.</param>
        /// <param name="size">Font size for the text.</param>
        [Conditional("_DEBUG")]
        public static void AttachText(Transform transform, Func<string> strFunc, Vector3 offset, Color color, int size)
        {
            CheckAndBuildHiddenRTDrawObject();
            rtDraw.RegisterAttachText(transform, strFunc, offset, color, size);
        }

        #endregion

        #region Overloads

        /*
         *	These are tons of overloads following how 'Debug.DrawXXX' are overloaded.
         */

        ///  <summary>
        /// 	Draw a line from <paramref name="start"/> to <paramref name="end"/> with <paramref>
        ///         <name>color</name>
        ///     </paramref>
        ///     .
        ///  </summary>
        ///  <param name="start">Point in world space where the line should start.</param>
        ///  <param name="end">Point in world space where the line should end.</param>
        [Conditional("_DEBUG")]
        public static void DrawLine(Vector3 start, Vector3 end)
        {
            DrawLine(start, end, DrawDefaultColor, 0f, true);
        }

        /// <summary>
        ///	Draw a line from <paramref name="start"/> to <paramref name="end"/> with <paramref name="color"/>.
        /// </summary>
        /// <param name="start">Point in world space where the line should start.</param>
        /// <param name="end">Point in world space where the line should end.</param>
        /// <param name="color">Color of the line.</param>
        [Conditional("_DEBUG")]
        public static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            DrawLine(start, end, color, 0f, true);
        }

        /// <summary>
        ///	Draw a line from <paramref name="start"/> to <paramref name="end"/> with <paramref name="color"/>.
        /// </summary>
        /// <param name="start">Point in world space where the line should start.</param>
        /// <param name="end">Point in world space where the line should end.</param>
        /// <param name="color">Color of the line.</param>
        /// <param name="duration">How long the line should be visible for.</param>
        [Conditional("_DEBUG")]
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration)
        {
            DrawLine(start, end, color, duration, true);
        }

        /// <summary>
        /// Draws a line from start to start + dir in world coordinates.
        /// </summary>
        /// <param name="start">Point in world space where the ray should start.</param>
        /// <param name="dir">Direction and length of the ray.</param>
        [Conditional("_DEBUG")]
        public static void DrawRay(Vector3 start, Vector3 dir)
        {
            DrawRay(start, dir, DrawDefaultColor, 0f, true);
        }

        /// <summary>
        /// Draws a line from start to start + dir in world coordinates.
        /// </summary>
        /// <param name="start">Point in world space where the ray should start.</param>
        /// <param name="dir">Direction and length of the ray.</param>
        /// <param name="color">Color of the drawn line.</param>
        [Conditional("_DEBUG")]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color)
        {
            DrawRay(start, dir, color, 0f, true);
        }

        /// <summary>
        /// Draws a line from start to start + dir in world coordinates.
        /// </summary>
        /// <param name="start">Point in world space where the ray should start.</param>
        /// <param name="dir">Direction and length of the ray.</param>
        /// <param name="color">Color of the drawn line.</param>
        /// <param name="duration">How long the line will be visible for (in seconds).</param>
        [Conditional("_DEBUG")]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration)
        {
            DrawRay(start, dir, color, duration, true);
        }

        /// <summary>
        /// Draw a text at given position.
        /// </summary>
        /// <param name="pos">Position</param>
        /// <param name="text">String of the text.</param>
        [Conditional("_DEBUG")]
        public static void DrawText(Vector3 pos, string text)
        {
            DrawText(pos, text, DrawDefaultColor, DRAW_TEXT_DEFAULT_SIZE, 0f, false);
        }

        /// <summary>
        /// Draw a text at given position.
        /// </summary>
        /// <param name="pos">Position</param>
        /// <param name="text">String of the text.</param>
        /// <param name="color">Color for the text.</param>
        [Conditional("_DEBUG")]
        public static void DrawText(Vector3 pos, string text, Color color)
        {
            DrawText(pos, text, color, DRAW_TEXT_DEFAULT_SIZE, 0f, false);
        }

        /// <summary>
        /// Draw a text at given position.
        /// </summary>
        /// <param name="pos">Position</param>
        /// <param name="text">String of the text.</param>
        /// <param name="color">Color for the text.</param>
        /// <param name="size">Font size for the text.</param>
        [Conditional("_DEBUG")]
        public static void DrawText(Vector3 pos, string text, Color color, int size)
        {
            DrawText(pos, text, color, size, 0f, false);
        }

        /// <summary>
        /// Draw a text at given position.
        /// </summary>
        /// <param name="pos">Position</param>
        /// <param name="text">String of the text.</param>
        /// <param name="color">Color for the text.</param>
        /// <param name="size">Font size for the text.</param>
        /// <param name="duration">How long the text should be visible for.</param>
        [Conditional("_DEBUG")]
        public static void DrawText(Vector3 pos, string text, Color color, int size, float duration)
        {
            DrawText(pos, text, color, size, duration, false);
        }

        /// <summary>
        /// Attach text to a transform.
        /// </summary>
        /// <param name="transform">Target transform to attach text to.</param>
        /// <param name="strFunc">Function will be called on every frame to get a string as attached text. </param>
        [Conditional("_DEBUG")]
        public static void AttachText(Transform transform, Func<string> strFunc)
        {
            AttachText(transform, strFunc, Vector3.zero, DrawDefaultColor, DRAW_TEXT_DEFAULT_SIZE);
        }

        /// <summary>
        /// Attach text to a transform.
        /// </summary>
        /// <param name="transform">Target transform to attach text to.</param>
        /// <param name="strFunc">Function will be called on every frame to get a string as attached text. </param>
        /// <param name="offset">Text attach offset to transform position.</param>
        [Conditional("_DEBUG")]
        public static void AttachText(Transform transform, Func<string> strFunc, Vector3 offset)
        {
            AttachText(transform, strFunc, offset, DrawDefaultColor, DRAW_TEXT_DEFAULT_SIZE);
        }

        /// <summary>
        /// Attach text to a transform.
        /// </summary>
        /// <param name="transform">Target transform to attach text to.</param>
        /// <param name="strFunc">Function will be called on every frame to get a string as attached text. </param>
        /// <param name="offset">Text attach offset to transform position.</param>
        /// <param name="color">Color for the text.</param>
        [Conditional("_DEBUG")]
        public static void AttachText(Transform transform, Func<string> strFunc, Vector3 offset, Color color)
        {
            AttachText(transform, strFunc, offset, color, DRAW_TEXT_DEFAULT_SIZE);
        }

        #endregion

        #region Internal

        /// <summary>
        /// Singleton RuntimeDebugDraw component that is needed to call Unity APIs.
        /// </summary>
        private static RuntimeDebugDraw rtDraw;

        /// <summary>
        /// Check and build 
        /// </summary>
        private const string HIDDEN_GO_NAME = "________HIDDEN_C4F6A87F298241078E21C0D7C1D87A76_";

        private static void CheckAndBuildHiddenRTDrawObject()
        {
            if (rtDraw != null)
                return;

            //	try reuse existing one first
            rtDraw = Object.FindObjectOfType<RuntimeDebugDraw>();
            if (rtDraw != null)
                return;

            //	instantiate an hidden gameobject w/ RuntimeDebugDraw attached.
            //	hardcode an GUID in the name so one won't accidentally get this by name.
            GameObject go = new(HIDDEN_GO_NAME);
            GameObject childGo = new(HIDDEN_GO_NAME);
            childGo.transform.parent = go.transform;
            rtDraw = childGo.AddComponent<RuntimeDebugDraw>();
            //	hack to only hide outer go, so that RuntimeDebugDraw's OnGizmos will work properly.
            go.hideFlags = HideFlags.HideAndDontSave;
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(go);
        }

        #endregion
    }

    #region Editor

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
    public static class DrawEditor
    {
        static DrawEditor()
        {
            //	set a low execution order
            string name = typeof(RuntimeDebugDraw).Name;
            foreach (UnityEditor.MonoScript monoScript in UnityEditor.MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (name != monoScript.name)
                    continue;

                if (UnityEditor.MonoImporter.GetExecutionOrder(monoScript) != 9990)
                {
                    UnityEditor.MonoImporter.SetExecutionOrder(monoScript, 9990);
                    return;
                }
            }
        }
    }
#endif

    #endregion

    internal class RuntimeDebugDraw : MonoBehaviour
    {
        #region Basics

        private void CheckInitialized()
        {
            //	as RuntimeDebugDraw component has a very low execution order, other script might Awake()
            //	earlier than this and at that moment it's not initialized. check and init on every public
            //	member
            if (drawTextEntries == null)
            {
                zTestBatch = new BatchedLineDraw(true);
                alwaysBatch = new BatchedLineDraw(false);
                lineEntries = new List<DrawLineEntry>(16);

                textStyle = new GUIStyle();
                textStyle.alignment = TextAnchor.UpperLeft;
                drawTextEntries = new List<DrawTextEntry>(16);
                attachTextEntries = new List<AttachTextEntry>(16);
            }
        }

        private void Awake()
        {
            CheckInitialized();
        }

        private void OnGUI()
        {
            DrawTextOnGUI();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawTextOnDrawGizmos();
        }
#endif

        private void LateUpdate()
        {
            TickAndDrawLines();
            TickTexts();
        }

        private void OnDestroy()
        {
            alwaysBatch.Dispose();
            zTestBatch.Dispose();
        }

        #endregion

        #region Draw Lines

        private class DrawLineEntry
        {
            public bool Occupied;
            public Vector3 Start;
            public Vector3 End;
            public Color Color;
            public float Timer;
            public bool NoZTest;
        }

        private List<DrawLineEntry> lineEntries;

        //	helper class for batching
        private class BatchedLineDraw : IDisposable
        {
            public readonly Mesh Mesh;
            public readonly Material Mat;

            private readonly List<Vector3> vertices;
            private readonly List<Color> colors;
            private readonly List<int> indices;

            public BatchedLineDraw(bool depthTest)
            {
                Mesh = new Mesh();
                Mesh.MarkDynamic();

                //	relying on a builtin shader, but it shouldn't change that much.
                Mat = new Material(Shader.Find("Hidden/Internal-Colored"));
                Mat.SetInt(ZTest, depthTest
                        ? 4 // LEqual
                        : 0 // Always
                );

                vertices = new List<Vector3>();
                colors = new List<Color>();
                indices = new List<int>();
            }

            public void AddLine(Vector3 from, Vector3 to, Color color)
            {
                vertices.Add(from);
                vertices.Add(to);
                colors.Add(color);
                colors.Add(color);
                int verticeCount = vertices.Count;
                indices.Add(verticeCount - 2);
                indices.Add(verticeCount - 1);
            }

            public void Clear()
            {
                Mesh.Clear();
                vertices.Clear();
                colors.Clear();
                indices.Clear();
            }

            public void BuildBatch()
            {
                Mesh.SetVertices(vertices);
                Mesh.SetColors(colors);
                Mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0); // cant get rid of this alloc for now
            }

            public void Dispose()
            {
                DestroyImmediate(Mesh);
                DestroyImmediate(Mat);
            }
        }

        private BatchedLineDraw zTestBatch;
        private BatchedLineDraw alwaysBatch;
        private bool linesNeedRebuild;

        public void RegisterLine(Vector3 start, Vector3 end, Color color, float timer, bool noZTest)
        {
            CheckInitialized();

            DrawLineEntry entry = null;
            for (int ix = 0; ix < lineEntries.Count; ix++)
                if (!lineEntries[ix].Occupied)
                {
                    entry = lineEntries[ix];
                    break;
                }

            if (entry == null)
            {
                entry = new DrawLineEntry();
                lineEntries.Add(entry);
            }

            entry.Occupied = true;
            entry.Start = start;
            entry.End = end;
            entry.Color = color;
            entry.Timer = timer;
            entry.NoZTest = noZTest;
            linesNeedRebuild = true;
        }

        private void RebuildDrawLineBatchMesh()
        {
            zTestBatch.Clear();
            alwaysBatch.Clear();

            for (int ix = 0; ix < lineEntries.Count; ix++)
            {
                DrawLineEntry entry = lineEntries[ix];
                if (!entry.Occupied)
                    continue;

                if (entry.NoZTest)
                    alwaysBatch.AddLine(entry.Start, entry.End, entry.Color);
                else
                    zTestBatch.AddLine(entry.Start, entry.End, entry.Color);
            }

            zTestBatch.BuildBatch();
            alwaysBatch.BuildBatch();
        }

        private void TickAndDrawLines()
        {
            if (linesNeedRebuild)
            {
                RebuildDrawLineBatchMesh();
                linesNeedRebuild = false;
            }

            //	draw on UI layer which should bypass most postFX setups
            Graphics.DrawMesh(alwaysBatch.Mesh, Vector3.zero, Quaternion.identity, alwaysBatch.Mat,
                Draw.DRAW_LINE_LAYER, null, 0, null, false, false);
            Graphics.DrawMesh(zTestBatch.Mesh, Vector3.zero, Quaternion.identity, zTestBatch.Mat, Draw.DRAW_LINE_LAYER,
                null, 0, null, false, false);

            //	update timer late so every added entry can be drawed for at least one frame
            for (int ix = 0; ix < lineEntries.Count; ix++)
            {
                DrawLineEntry entry = lineEntries[ix];
                if (!entry.Occupied)
                    continue;
                entry.Timer -= Time.deltaTime;
                if (entry.Timer < 0)
                {
                    entry.Occupied = false;
                    linesNeedRebuild = true;
                }
            }
        }

        #endregion

        #region Draw Text

        [Flags]
        public enum DrawFlag : byte
        {
            None = 0,
            DrawnGizmo = 1 << 0,
            DrawnGUI = 1 << 1,
            DrawnAll = DrawnGizmo | DrawnGUI
        }

        private class DrawTextEntry
        {
            public bool Occupied;
            public readonly GUIContent Content;
            public Vector3 Anchor;
            public int Size;
            public Color Color;
            public float Timer;
            public bool PopUp;
            public float Duration;

            //	Text entries needs to be draw in both OnGUI/OnDrawGizmos, need flags for mark
            //	has been visited by both
            public DrawFlag Flag = DrawFlag.None;

            public DrawTextEntry()
            {
                Content = new GUIContent();
            }
        }

        private class AttachTextEntry
        {
            public bool Occupied;
            public readonly GUIContent Content;
            public Vector3 Offset;
            public int Size;
            public Color Color;


            public Transform Transform;
            public Func<string> StrFunc;

            public DrawFlag Flag = DrawFlag.None;

            public AttachTextEntry()
            {
                Content = new GUIContent();
            }
        }

        private List<DrawTextEntry> drawTextEntries;
        private List<AttachTextEntry> attachTextEntries;
        private GUIStyle textStyle;
        private static readonly int ZTest = Shader.PropertyToID("_ZTest");

        public void RegisterDrawText(Vector3 anchor, string text, Color color, int size, float timer, bool popUp)
        {
            CheckInitialized();

            DrawTextEntry entry = null;
            for (int ix = 0; ix < drawTextEntries.Count; ix++)
                if (!drawTextEntries[ix].Occupied)
                {
                    entry = drawTextEntries[ix];
                    break;
                }

            if (entry == null)
            {
                entry = new DrawTextEntry();
                drawTextEntries.Add(entry);
            }

            entry.Occupied = true;
            entry.Anchor = anchor;
            entry.Content.text = text;
            entry.Size = size;
            entry.Color = color;
            entry.Duration = entry.Timer = timer;
            entry.PopUp = popUp;
#if UNITY_EDITOR
            entry.Flag = DrawFlag.None;
#else
			//	in builds consider gizmo is already drawn
			entry.flag = DrawFlag.DrawnGizmo;
#endif
        }

        public void RegisterAttachText(Transform target, Func<string> strFunc, Vector3 offset, Color color, int size)
        {
            CheckInitialized();

            AttachTextEntry entry = null;
            for (int ix = 0; ix < attachTextEntries.Count; ix++)
                if (!attachTextEntries[ix].Occupied)
                {
                    entry = attachTextEntries[ix];
                    break;
                }

            if (entry == null)
            {
                entry = new AttachTextEntry();
                attachTextEntries.Add(entry);
            }

            entry.Occupied = true;
            entry.Offset = offset;
            entry.Transform = target;
            entry.StrFunc = strFunc;
            entry.Color = color;
            entry.Size = size;
            //	get first text
            entry.Content.text = strFunc();
#if UNITY_EDITOR
            entry.Flag = DrawFlag.None;
#else
			//	in builds consider gizmo is already drawn
			entry.flag = DrawFlag.DrawnGizmo;
#endif
        }

        private void TickTexts()
        {
            for (int ix = 0; ix < drawTextEntries.Count; ix++)
            {
                DrawTextEntry entry = drawTextEntries[ix];
                if (!entry.Occupied)
                    continue;
                entry.Timer -= Time.deltaTime;
                if (entry.Flag == DrawFlag.DrawnAll)
                    if (entry.Timer < 0)
                        entry.Occupied = false;
                //	actually no need to tick DrawFlag as it won't move
            }

            for (int ix = 0; ix < attachTextEntries.Count; ix++)
            {
                AttachTextEntry entry = attachTextEntries[ix];
                if (!entry.Occupied)
                    continue;
                if (entry.Transform == null)
                {
                    entry.Occupied = false;
                    entry.StrFunc = null; // needs to release ref to callback
                }
                else if (entry.Flag == DrawFlag.DrawnAll)
                {
                    // tick content
                    entry.Content.text = entry.StrFunc();
                    // tick flag
#if UNITY_EDITOR
                    entry.Flag = DrawFlag.None;
#else
					//	in builds consider gizmo is already drawn
					entry.flag = DrawFlag.DrawnGizmo;
#endif
                }
            }
        }

        private void DrawTextOnGUI()
        {
            Camera drawCamera = Draw.GetDebugDrawCamera();
            if (drawCamera == null)
                return;

            foreach (DrawTextEntry entry in drawTextEntries)
            {
                if (!entry.Occupied)
                    continue;

                GUIDrawTextEntry(drawCamera, entry);
                entry.Flag |= DrawFlag.DrawnGUI;
            }

            for (int ix = 0; ix < attachTextEntries.Count; ix++)
            {
                AttachTextEntry entry = attachTextEntries[ix];
                if (!entry.Occupied)
                    continue;

                GUIAttachTextEntry(drawCamera, entry);
                entry.Flag |= DrawFlag.DrawnGUI;
            }
        }

        private void GUIDrawTextEntry(Camera drawCamera, DrawTextEntry entry)
        {
            Vector3 worldPos = entry.Anchor;
            Vector3 screenPos = drawCamera.WorldToScreenPoint(worldPos);
            screenPos.y = Screen.height - screenPos.y;

            if (entry.PopUp)
            {
                float ratio = entry.Timer / entry.Duration;
                screenPos.y -= (1 - ratio * ratio) * entry.Size * 1.5f;
            }

            textStyle.normal.textColor = entry.Color;
            textStyle.fontSize = entry.Size;
            Rect rect = new(screenPos, textStyle.CalcSize(entry.Content));
            GUI.Label(rect, entry.Content, textStyle);
        }

        private void GUIAttachTextEntry(Camera drawCamera, AttachTextEntry entry)
        {
            if (entry.Transform == null)
                return;

            Vector3 worldPos = entry.Transform.position + entry.Offset;
            Vector3 screenPos = drawCamera.WorldToScreenPoint(worldPos);
            screenPos.y = Screen.height - screenPos.y;

            textStyle.normal.textColor = entry.Color;
            textStyle.fontSize = entry.Size;
            Rect rect = new(screenPos, textStyle.CalcSize(entry.Content));
            GUI.Label(rect, entry.Content, textStyle);
        }


#if UNITY_EDITOR
        private void DrawTextOnDrawGizmos()
        {
            if (!(Camera.current == Draw.GetDebugDrawCamera()
                  || Camera.current == UnityEditor.SceneView.lastActiveSceneView.camera))
                return;

            Camera drawCamera = Camera.current;
            if (drawCamera == null)
                return;

            UnityEditor.Handles.BeginGUI();
            for (int ix = 0; ix < drawTextEntries.Count; ix++)
            {
                DrawTextEntry entry = drawTextEntries[ix];
                if (!entry.Occupied)
                    continue;

                GUIDrawTextEntry(drawCamera, entry);
                entry.Flag |= DrawFlag.DrawnGizmo;
            }

            for (int ix = 0; ix < attachTextEntries.Count; ix++)
            {
                AttachTextEntry entry = attachTextEntries[ix];
                if (!entry.Occupied)
                    continue;

                GUIAttachTextEntry(drawCamera, entry);
                entry.Flag |= DrawFlag.DrawnGizmo;
            }

            UnityEditor.Handles.EndGUI();
        }
#endif

        #endregion
    }
}
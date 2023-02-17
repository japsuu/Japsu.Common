using System;
using UnityEditor;
using UnityEngine;

namespace Japsu.Common.Editor
{
    public static class GameObjectCreatorHelper
    {
        // [MenuItem("GameObject/Systems/xxx")]
        // private static void CreateVFXSystem()
        // {
        //     CreateSystemGameObject(typeof(xxx));
        // }
        
        [MenuItem("GameObject/--- Spacer ---", priority = -100)]
        private static void CreateSpacer()
        {
            GameObject go = new GameObject("------- XXX ------");
            
            go.SetActive(false);

            go.tag = "EditorOnly";

            if (Selection.activeGameObject != null)
            {
                int selectionIndex = Selection.activeGameObject.transform.GetSiblingIndex();
                go.transform.SetSiblingIndex(selectionIndex + 1);
            }
        
            Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");
        }

        private static void CreateSystemGameObject(Type system)
        {
            GameObject go = new GameObject(system.Name);

            go.AddComponent(system);
        
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        
            Selection.activeObject = go;
        }
    }
}
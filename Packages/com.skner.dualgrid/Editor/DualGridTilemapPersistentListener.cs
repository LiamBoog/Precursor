using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace skner.DualGrid.Editor
{
    [InitializeOnLoad]
    public static class DualGridTilemapPersistentListener
    {
        private static bool dragging;
        
        private static DualGridTilemapModule[] DualGridModules => Object.FindObjectsByType<DualGridTilemapModule>(FindObjectsSortMode.None);
        
        static DualGridTilemapPersistentListener()
        {
            Tilemap.tilemapTileChanged += HandleTilemapChange;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void HandleTilemapChange(Tilemap tilemap, Tilemap.SyncTile[] tiles)
        {
            foreach (var module in DualGridModules)
            {
                module.HandleTilemapChange(tilemap, tiles);
            }
        }

        private static void OnSceneGUI(SceneView _)
        {
            if (dragging)
            {
                foreach (var module in DualGridModules)
                {
                    module.SetEditorPreviewTiles();
                }
                return;
            }
            
            Type activeToolType = ToolManager.activeToolType;
            if (activeToolType != typeof(PaintTool) && activeToolType != typeof(EraseTool))
                return;
            
            Event currentEvent = Event.current;
            if (!(currentEvent.type == EventType.MouseDown && currentEvent.button == 0))
                return;

            SceneView.beforeSceneGui += OnUpdate;
            dragging = true;
            
            void OnUpdate(SceneView _)
            {
                if (Event.current.type != EventType.MouseUp)
                    return;
                
                SceneView.beforeSceneGui -= OnUpdate;
                dragging = false;
            }
        }
    }
}

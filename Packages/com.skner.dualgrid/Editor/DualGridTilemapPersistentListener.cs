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
            Type activeToolType = ToolManager.activeToolType;
            if (activeToolType != typeof(PaintTool) && activeToolType != typeof(EraseTool))
                return;
            
            EventType currentEventType = Event.current.type;
            if (currentEventType != EventType.MouseDrag && currentEventType != EventType.Layout)
                return;
            
            foreach (var module in DualGridModules)
            {
                module.SetEditorPreviewTiles();
            }
        }
    }
}

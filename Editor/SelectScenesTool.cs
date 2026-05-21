using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EditorTool.SceneSelectTool
{
    /// <summary>
    /// A simple little Unity Editor tool for quickly switching between scenes.
    /// </summary>
    public class SceneSelectTool : EditorWindow
    {
        private Vector2 mScrollPos;
        private bool mShowAllScenes;

        [MenuItem("Tools/Scene Select Tool")]
        internal static void Init()
        {
            var window = (SceneSelectTool)GetWindow(typeof(SceneSelectTool), false, "Scene Select Tool");
            window.position = new Rect(window.position.xMin + 100f, window.position.yMin + 100f, 200f, 400f);
        }

        internal void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            mScrollPos = EditorGUILayout.BeginScrollView(mScrollPos, false, false);
            GUILayout.Space(10);
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            mShowAllScenes = GUILayout.Toggle(mShowAllScenes, "Show All Scenes in the Project");
            GUILayout.Space(10);
            
            var scenesGUIDs = AssetDatabase.FindAssets("t:Scene");
            var scenesPaths = mShowAllScenes
                ? scenesGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToArray()
                : scenesGUIDs.Select(AssetDatabase.GUIDToAssetPath).Where(s => s.StartsWith("Assets/Scenes/"))
                    .ToArray();
            GUILayout.Label("Master Loading", EditorStyles.boldLabel);

            if(IsActiveMasterScene())
                SelectMasterScene(scenesPaths);

            GUILayout.Space(10);

            GUILayout.Label("Play Tests", EditorStyles.boldLabel);
            if (GUILayout.Button("Run PlayMode Tests (Ignore Master Scene)"))
                PlayModeTestRunner.RunAllPlayModeTests();

            GUILayout.Space(10);
            
            GUILayout.Label("Scenes", EditorStyles.boldLabel);
            DrawScenesList(scenesPaths);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawScenesList(IEnumerable<string> scenesPaths)
        {
            var scenesPathsArray = scenesPaths.ToArray();
            foreach (var path in scenesPathsArray)
            {
                var sceneName = Path.GetFileNameWithoutExtension(path);
                var pressed = GUILayout.Button(sceneName,
                    new GUIStyle(GUI.skin.GetStyle("Button")) { alignment = TextAnchor.MiddleLeft });
                if (pressed)
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(path);
            }
        }

        private static bool IsActiveMasterScene()
        {
            var previousLoadMasterOnPlay = SceneAutoLoader.LoadMasterOnPlay;

            var newLoadMasterOnPlay = EditorGUILayout.Toggle("Play From Master Scene", previousLoadMasterOnPlay);

            if (newLoadMasterOnPlay != previousLoadMasterOnPlay)
                SceneAutoLoader.LoadMasterOnPlay = newLoadMasterOnPlay;

            return newLoadMasterOnPlay;
        }
        
        private void SelectMasterScene(string[] scenePaths)
        {
            var previousMasterScene = SceneAutoLoader.MasterScene;

            scenePaths = new[] { string.Empty }.Concat(scenePaths).ToArray();
            var displayNames = new[] { "<No scene chosen>" }
                .Concat(scenePaths.Where(s => !string.IsNullOrEmpty(s)).Select(Path.GetFileNameWithoutExtension))
                .ToArray();
            
            var previousSelectedIndex = Math.Max(Array.IndexOf(scenePaths, previousMasterScene), 0);
            var newSelectedIndex = EditorGUILayout.Popup("Master Scene", previousSelectedIndex, displayNames);
            
            var newMasterScene = newSelectedIndex > 0 ? scenePaths[newSelectedIndex] : "";

            if (newMasterScene != previousMasterScene)
                SceneAutoLoader.MasterScene = newMasterScene;
        }
    }
}
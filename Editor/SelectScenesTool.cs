using System;
using System.Collections.Generic;
using System.IO;
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
        private const string ProjectScenesFolder = "Assets/Scenes/";

        private static SceneCache sProjectScenesCache;
        private static SceneCache sAllScenesCache;
        private static GUIStyle sSceneButtonStyle;
        private static bool sIsSubscribedToProjectChanges;

        private Vector2 mScrollPos;
        private bool mShowAllScenes;

        [MenuItem("Tools/Scene Select Tool")]
        internal static void Init()
        {
            var window = (SceneSelectTool)GetWindow(typeof(SceneSelectTool), false, "Scene Select Tool");
            window.position = new Rect(window.position.xMin + 100f, window.position.yMin + 100f, 200f, 400f);
        }

        internal void OnEnable()
        {
            EnsureProjectChangedSubscription();
        }

        internal void OnGUI()
        {
            EnsureProjectChangedSubscription();

            EditorGUILayout.BeginVertical();
            mScrollPos = EditorGUILayout.BeginScrollView(mScrollPos, false, false);
            GUILayout.Space(10);
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            mShowAllScenes = GUILayout.Toggle(mShowAllScenes, "Show All Scenes in the Project");
            GUILayout.Space(10);

            var scenesCache = GetSceneCache(mShowAllScenes);

            GUILayout.Label("Master Loading", EditorStyles.boldLabel);

            if (IsActiveMasterScene())
                SelectMasterScene(scenesCache);

            GUILayout.Space(10);

            GUILayout.Label("Play Tests", EditorStyles.boldLabel);
            if (GUILayout.Button("Run PlayMode Tests (Ignore Master Scene)"))
                PlayModeTestRunner.RunAllPlayModeTests();

            GUILayout.Space(10);
            
            GUILayout.Label("Scenes", EditorStyles.boldLabel);
            DrawScenesList(scenesCache);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static SceneCache GetSceneCache(bool showAllScenes)
        {
            EnsureSceneCache();
            return showAllScenes ? sAllScenesCache : sProjectScenesCache;
        }

        private static void EnsureSceneCache()
        {
            if (sProjectScenesCache != null && sAllScenesCache != null)
                return;

            var scenesGUIDs = AssetDatabase.FindAssets("t:Scene");
            var allScenePaths = new List<string>(scenesGUIDs.Length);

            foreach (var sceneGuid in scenesGUIDs)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);

                if (!string.IsNullOrEmpty(scenePath))
                    allScenePaths.Add(scenePath);
            }

            allScenePaths.Sort(StringComparer.OrdinalIgnoreCase);

            var projectScenePaths = new List<string>(allScenePaths.Count);

            foreach (var scenePath in allScenePaths)
            {
                if (scenePath.StartsWith(ProjectScenesFolder, StringComparison.Ordinal))
                    projectScenePaths.Add(scenePath);
            }

            sAllScenesCache = new SceneCache(allScenePaths.ToArray());
            sProjectScenesCache = new SceneCache(projectScenePaths.ToArray());
        }

        private static void EnsureProjectChangedSubscription()
        {
            if (sIsSubscribedToProjectChanges)
                return;

            EditorApplication.projectChanged += ClearSceneCache;
            sIsSubscribedToProjectChanges = true;
        }

        private static void ClearSceneCache()
        {
            sProjectScenesCache = null;
            sAllScenesCache = null;
        }

        private static GUIStyle SceneButtonStyle
        {
            get
            {
                if (sSceneButtonStyle == null)
                    sSceneButtonStyle = new GUIStyle(GUI.skin.GetStyle("Button")) { alignment = TextAnchor.MiddleLeft };

                return sSceneButtonStyle;
            }
        }

        private void DrawScenesList(SceneCache scenesCache)
        {
            for (var i = 0; i < scenesCache.Paths.Length; i++)
            {
                var pressed = GUILayout.Button(scenesCache.DisplayNames[i], SceneButtonStyle);

                if (pressed && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(scenesCache.Paths[i]);
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
        
        private void SelectMasterScene(SceneCache scenesCache)
        {
            var previousMasterScene = SceneAutoLoader.MasterScene;

            var previousSelectedIndex = Math.Max(Array.IndexOf(scenesCache.PopupPaths, previousMasterScene), 0);
            var newSelectedIndex = EditorGUILayout.Popup("Master Scene", previousSelectedIndex, scenesCache.PopupDisplayNames);
            
            var newMasterScene = newSelectedIndex > 0 ? scenesCache.PopupPaths[newSelectedIndex] : "";

            if (newMasterScene != previousMasterScene)
                SceneAutoLoader.MasterScene = newMasterScene;
        }

        private sealed class SceneCache
        {
            public readonly string[] Paths;
            public readonly string[] DisplayNames;
            public readonly string[] PopupPaths;
            public readonly string[] PopupDisplayNames;

            public SceneCache(string[] paths)
            {
                Paths = paths;
                DisplayNames = new string[paths.Length];

                for (var i = 0; i < paths.Length; i++)
                    DisplayNames[i] = Path.GetFileNameWithoutExtension(paths[i]);

                PopupPaths = new string[paths.Length + 1];
                PopupDisplayNames = new string[paths.Length + 1];

                PopupPaths[0] = string.Empty;
                PopupDisplayNames[0] = "<No scene chosen>";

                Array.Copy(paths, 0, PopupPaths, 1, paths.Length);
                Array.Copy(DisplayNames, 0, PopupDisplayNames, 1, DisplayNames.Length);
            }
        }
    }
}
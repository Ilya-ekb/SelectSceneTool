using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace EditorTool.SceneSelectTool
{
    [InitializeOnLoad]
    public static class SceneAutoLoader
    {
        private const string TestRunnerApiTypeName = "UnityEditor.TestTools.TestRunner.Api.TestRunnerApi, UnityEditor.TestRunner";

        private static readonly bool sIsCommandLineTestRun =
            Environment.GetCommandLineArgs()
                .Any(arg => string.Equals(arg, "-runTests", StringComparison.OrdinalIgnoreCase));

        private static MethodInfo sIsRunActiveMethod;
        private static bool sDidResolveIsRunActiveMethod;

        static SceneAutoLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                case PlayModeStateChange.ExitingEditMode:
                {
                    ScenesInHierarchyView = new SceneSetup[] { };

                    if (ShouldSkipMasterSceneLoading())
                        break;

                    var masterSceneExistsInProject = AssetDatabase.AssetPathToGUID(MasterScene) != "";

                    if (LoadMasterOnPlay && masterSceneExistsInProject)
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            ScenesInHierarchyView = EditorSceneManager.GetSceneManagerSetup();

                            var scene = EditorSceneManager.OpenScene(MasterScene, OpenSceneMode.Single);

                            if (!scene.IsValid())
                            {
                                EditorApplication.isPlaying = false;
                                EditorApplication.update += ReloadLastScene;
                            }
                        }
                        else
                        {
                            EditorApplication.isPlaying = false;
                        }
                    }

                    break;
                }
                case PlayModeStateChange.ExitingPlayMode:
                {
                    if (ScenesInHierarchyView.Length != 0)
                        EditorApplication.update += ReloadLastScene;
                    break;
                }
            }
        }

        private static void ReloadLastScene()
        {
            if (EditorApplication.isPlaying)
                return;
            
            var scenes = ScenesInHierarchyView;
            var activeSceneIndex = Array.FindIndex(scenes, scene => scene.isActive);
            var firstLoadedSceneIndex = Array.FindIndex(scenes, scene => scene.isLoaded);

            if (activeSceneIndex != firstLoadedSceneIndex)
            {
                scenes[firstLoadedSceneIndex].isActive = true;
                scenes[activeSceneIndex].isActive = false;
            }
            
            EditorSceneManager.RestoreSceneManagerSetup(scenes);
            
            if (activeSceneIndex != firstLoadedSceneIndex)
                SceneManager.SetActiveScene(SceneManager.GetSceneAt(activeSceneIndex));


            ScenesInHierarchyView = new SceneSetup[] { };

            EditorApplication.update -= ReloadLastScene;
        }

        private static string CEditorPrefLoadMasterOnPlay =>
            "SceneAutoLoader." + PlayerSettings.productName + ".LoadMasterOnPlay";

        private static string CEditorPrefMasterScene =>
            "SceneAutoLoader." + PlayerSettings.productName + ".MasterScene";

        private static string CEditorPrefLoadedScenes =>
            "SceneAutoLoader." + PlayerSettings.productName + ".LoadedScenes";

        private static string CSessionStateSuppressMasterSceneLoading =>
            "SceneAutoLoader." + PlayerSettings.productName + ".SuppressMasterSceneLoading";

        public static bool LoadMasterOnPlay
        {
            get => EditorPrefs.GetBool(CEditorPrefLoadMasterOnPlay, false);
            set => EditorPrefs.SetBool(CEditorPrefLoadMasterOnPlay, value);
        }

        public static string MasterScene
        {
            get => EditorPrefs.GetString(CEditorPrefMasterScene, "Master.unity");
            set => EditorPrefs.SetString(CEditorPrefMasterScene, value);
        }

        public static bool SuppressMasterSceneLoading
        {
            get => SessionState.GetBool(CSessionStateSuppressMasterSceneLoading, false);
            set => SessionState.SetBool(CSessionStateSuppressMasterSceneLoading, value);
        }

        private static bool ShouldSkipMasterSceneLoading()
        {
            return SuppressMasterSceneLoading || sIsCommandLineTestRun || IsUnityTestRunnerRunActive();
        }

        private static bool IsUnityTestRunnerRunActive()
        {
            var isRunActiveMethod = GetIsRunActiveMethod();

            if (isRunActiveMethod == null)
                return false;

            try
            {
                return isRunActiveMethod.Invoke(null, null) is bool isActive && isActive;
            }
            catch
            {
                return false;
            }
        }

        private static MethodInfo GetIsRunActiveMethod()
        {
            if (sDidResolveIsRunActiveMethod)
                return sIsRunActiveMethod;

            sDidResolveIsRunActiveMethod = true;

            var testRunnerApiType = Type.GetType(TestRunnerApiTypeName);
            sIsRunActiveMethod = testRunnerApiType?.GetMethod(
                "IsRunActive",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            return sIsRunActiveMethod;
        }

        private static SceneSetup[] ScenesInHierarchyView
        {
            get
            {
                string prefValue = EditorPrefs.GetString(CEditorPrefLoadedScenes, "");

                string[] tokens = prefValue.Split('|');

                int numScenes = tokens.Length / 3;

                SceneSetup[] scenes = new SceneSetup[numScenes];

                for (int i = 0; i < tokens.Length / 3; i++)
                {
                    scenes[i] = new SceneSetup();
                    scenes[i].isActive = (tokens[i * 3 + 0] != "false");
                    scenes[i].isLoaded = (tokens[i * 3 + 1] != "false");
                    scenes[i].path = tokens[i * 3 + 2];
                }

                return scenes;
            }

            set
            {
                string prefValue = string.Join("|",
                    value.Select(scene =>
                        (scene.isActive ? "true" : "false") + "|" + (scene.isLoaded ? "true" : "false") + "|" +
                        scene.path).ToArray());

                EditorPrefs.SetString(CEditorPrefLoadedScenes, prefValue);
            }
        }
    }
}
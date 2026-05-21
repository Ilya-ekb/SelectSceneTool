using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace EditorTool.SceneSelectTool
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        static PlayModeTestRunner()
        {
            RegisterCallbacks();
        }

        [MenuItem("Tools/Scene Select Tool/Run PlayMode Tests")]
        internal static void RunAllPlayModeTests()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("PlayMode tests cannot be started while Unity is already entering or leaving Play Mode.");
                return;
            }

            SceneAutoLoader.SuppressMasterSceneLoading = true;

            var filter = new Filter
            {
                testMode = TestMode.PlayMode
            };

            var executionSettings = new ExecutionSettings(filter);

            try
            {
#pragma warning disable CS0618
                var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                testRunnerApi.Execute(executionSettings);
#pragma warning restore CS0618
            }
            catch
            {
                SceneAutoLoader.SuppressMasterSceneLoading = false;
                throw;
            }
        }

        private static void RegisterCallbacks()
        {
#pragma warning disable CS0618
            var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            testRunnerApi.RegisterCallbacks(new MasterScenePlayModeTestCallbacks(), int.MinValue);
#pragma warning restore CS0618
        }

        private sealed class MasterScenePlayModeTestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                SceneAutoLoader.SuppressMasterSceneLoading = true;
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                SceneAutoLoader.SuppressMasterSceneLoading = false;
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}

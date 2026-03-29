#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace HelloMyWorld.EditorAutomation
{
    /// <summary>
    /// CI commandlet entry points invoked from Unity batch mode via -executeMethod.
    /// </summary>
    public static class UnityCiCommandlet
    {
        private const int SuccessExitCode = 0;
        private const int FailureExitCode = 1;
        private const double MaxRunMinutes = 30.0d;
        private const string ResultsDirectoryRelativePath = "reports/unity-tests";

        private static readonly Queue<TestMode> PendingModes = new Queue<TestMode>();

        private static bool _isRunning;
        private static DateTime _deadlineUtc;
        private static TestRunnerApi _testRunnerApi;
        private static TestRunCallbacks _currentCallbacks;
        private static int _totalPassed;
        private static int _totalFailed;
        private static int _totalSkipped;
        private static string _resultsDirectory = string.Empty;

        public static void RunCompileAndTests()
        {
            Begin(runEditModeTests: true, runPlayModeTests: true);
        }

        public static void RunCompileOnly()
        {
            Begin(runEditModeTests: false, runPlayModeTests: false);
        }

        public static void RunEditModeTests()
        {
            Begin(runEditModeTests: true, runPlayModeTests: false);
        }

        public static void RunPlayModeTests()
        {
            Begin(runEditModeTests: false, runPlayModeTests: true);
        }

        [MenuItem("Tools/CI/Run Compile+Tests (Commandlet)")]
        private static void RunFromMenu()
        {
            RunCompileAndTests();
        }

        private static void Begin(bool runEditModeTests, bool runPlayModeTests)
        {
            if (_isRunning)
            {
                FailAndExit("Unity CI commandlet is already running.");
                return;
            }

            _isRunning = true;
            _deadlineUtc = DateTime.UtcNow.AddMinutes(MaxRunMinutes);
            _totalPassed = 0;
            _totalFailed = 0;
            _totalSkipped = 0;
            _resultsDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ResultsDirectoryRelativePath));
            Directory.CreateDirectory(_resultsDirectory);

            PendingModes.Clear();
            if (runEditModeTests)
            {
                PendingModes.Enqueue(TestMode.EditMode);
            }

            if (runPlayModeTests)
            {
                PendingModes.Enqueue(TestMode.PlayMode);
            }

            LogInfo($"Start. runEditModeTests={runEditModeTests}, runPlayModeTests={runPlayModeTests}");

            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            if (DateTime.UtcNow > _deadlineUtc)
            {
                FailAndExit($"Timed out after {MaxRunMinutes} minutes.");
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (EditorUtility.scriptCompilationFailed)
            {
                FailAndExit("C# compilation failed.");
                return;
            }

            if (_currentCallbacks != null)
            {
                if (!_currentCallbacks.IsFinished)
                {
                    return;
                }

                OnCurrentTestRunFinished();
                return;
            }

            if (PendingModes.Count == 0)
            {
                SucceedAndExit("Compilation and requested tests completed.");
                return;
            }

            StartNextTestRun();
        }

        private static void StartNextTestRun()
        {
            TestMode mode = PendingModes.Dequeue();
            _testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            _currentCallbacks = new TestRunCallbacks(mode);
            _testRunnerApi.RegisterCallbacks(_currentCallbacks);

            var filter = new Filter
            {
                testMode = mode
            };

            var settings = new ExecutionSettings(new[] { filter });
            _testRunnerApi.Execute(settings);

            LogInfo($"Started {mode} tests.");
        }

        private static void OnCurrentTestRunFinished()
        {
            if (_currentCallbacks == null)
            {
                return;
            }

            _totalPassed += _currentCallbacks.PassCount;
            _totalFailed += _currentCallbacks.FailCount + _currentCallbacks.InconclusiveCount;
            _totalSkipped += _currentCallbacks.SkipCount;

            WriteSummaryFile(_currentCallbacks);

            string modeName = _currentCallbacks.Mode.ToString();
            string resultText =
                $"Finished {modeName} tests. Passed={_currentCallbacks.PassCount}, Failed={_currentCallbacks.FailCount}, Inconclusive={_currentCallbacks.InconclusiveCount}, Skipped={_currentCallbacks.SkipCount}";

            if (_currentCallbacks.FailCount > 0 || _currentCallbacks.InconclusiveCount > 0)
            {
                CleanupRunnerState();
                FailAndExit(resultText);
                return;
            }

            LogInfo(resultText);
            CleanupRunnerState();
        }

        private static void WriteSummaryFile(TestRunCallbacks callbacks)
        {
            var summary = new TestRunSummary
            {
                mode = callbacks.Mode.ToString(),
                startedUtc = callbacks.StartedUtc.ToString("O"),
                finishedUtc = callbacks.FinishedUtc.ToString("O"),
                passed = callbacks.PassCount,
                failed = callbacks.FailCount,
                inconclusive = callbacks.InconclusiveCount,
                skipped = callbacks.SkipCount
            };

            string fileName = $"summary-{summary.mode.ToLowerInvariant()}.json";
            string outputPath = Path.Combine(_resultsDirectory, fileName);
            string json = JsonUtility.ToJson(summary, prettyPrint: true);
            File.WriteAllText(outputPath, json + Environment.NewLine);
            LogInfo($"Wrote {outputPath}");
        }

        private static void CleanupRunnerState()
        {
            if (_testRunnerApi != null && _currentCallbacks != null)
            {
                _testRunnerApi.UnregisterCallbacks(_currentCallbacks);
            }

            if (_testRunnerApi != null)
            {
                ScriptableObject.DestroyImmediate(_testRunnerApi);
            }

            _testRunnerApi = null;
            _currentCallbacks = null;
        }

        private static void SucceedAndExit(string message)
        {
            CleanupAndExit(SuccessExitCode, message);
        }

        private static void FailAndExit(string message)
        {
            CleanupAndExit(FailureExitCode, message);
        }

        private static void CleanupAndExit(int exitCode, string message)
        {
            EditorApplication.update -= Update;
            CleanupRunnerState();

            string totalSummary = $"Totals => Passed={_totalPassed}, Failed={_totalFailed}, Skipped={_totalSkipped}";
            if (exitCode == SuccessExitCode)
            {
                LogInfo(message);
                LogInfo(totalSummary);
            }
            else
            {
                LogError(message);
                LogError(totalSummary);
            }

            _isRunning = false;
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }

        private static void LogInfo(string message)
        {
            Debug.Log($"[UnityCiCommandlet] {message}");
        }

        private static void LogError(string message)
        {
            Debug.LogError($"[UnityCiCommandlet] {message}");
        }

        private sealed class TestRunCallbacks : ICallbacks
        {
            public TestRunCallbacks(TestMode mode)
            {
                Mode = mode;
            }

            public TestMode Mode { get; }
            public bool IsFinished { get; private set; }
            public DateTime StartedUtc { get; private set; }
            public DateTime FinishedUtc { get; private set; }
            public int PassCount { get; private set; }
            public int FailCount { get; private set; }
            public int InconclusiveCount { get; private set; }
            public int SkipCount { get; private set; }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                StartedUtc = DateTime.UtcNow;
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                FinishedUtc = DateTime.UtcNow;
                PassCount = result.PassCount;
                FailCount = result.FailCount;
                InconclusiveCount = result.InconclusiveCount;
                SkipCount = result.SkipCount;
                IsFinished = true;
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }

        [Serializable]
        private sealed class TestRunSummary
        {
            public string mode = string.Empty;
            public string startedUtc = string.Empty;
            public string finishedUtc = string.Empty;
            public int passed;
            public int failed;
            public int inconclusive;
            public int skipped;
        }
    }
}
#endif

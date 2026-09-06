#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fishing.V2.EditorTools
{
    /// <summary>
    /// Records one representative gameplay flow. The helper controls recording and the
    /// first cast only; fish movement, bite selection, catch flight, and tallying all use
    /// the normal runtime path.
    /// </summary>
    [InitializeOnLoad]
    public static class FishingV2DemoVideoCapture
    {
        private const string ScenePath = "Assets/FishingV2/Scenes/FishingV2Prototype.unity";
        private const string RequestKey = "FishingV2.DemoVideoCaptureRequested";
        private const string OutputKey = "FishingV2.DemoVideoCaptureOutput";
        private const string ExitKey = "FishingV2.DemoVideoCaptureExitWhenDone";
        private const string ExitPendingKey = "FishingV2.DemoVideoCaptureExitPending";
        private const string ExitCodeKey = "FishingV2.DemoVideoCaptureExitCode";
        private const double PreCastSeconds = 1.25;
        private const double FinalHoldSeconds = 2.5;
        private const double TimeoutSeconds = 45.0;
        private const int Width = 1920;
        private const int Height = 1080;
        private const float FrameRate = 60f;

        private enum Phase
        {
            WaitForSession,
            PreCast,
            WaitForArrival,
            FinalHold
        }

        private static RecorderController _controller;
        private static RecorderControllerSettings _controllerSettings;
        private static MovieRecorderSettings _movieSettings;
        private static FishingV2Session _session;
        private static Phase _phase;
        private static double _startedAt;
        private static double _phaseStartedAt;
        private static string _outputWithoutExtension;
        private static string _reportPath;
        private static bool _active;
        private static bool _sawSessionLive;
        private static bool _sawCatchFlight;
        private static int _initialArrivals;
        private static int _errors;
        private static readonly List<string> Records = new List<string>();

        static FishingV2DemoVideoCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += BootstrapAfterDomainReload;
            EditorApplication.delayCall += ExitIfPending;
        }

        [MenuItem("Fishing V2/Capture portfolio demo video", priority = 46)]
        public static void CaptureFromMenu()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".validation", "demo-video"));
            string output = Path.Combine(folder, "FishingIsGood_Unity_Demo_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            RequestCapture(output, false);
        }

        // Usage:
        // Unity.exe -projectPath <project> -executeMethod Fishing.V2.EditorTools.FishingV2DemoVideoCapture.CaptureFromCommandLine
        //           --fishing-demo-output <absolute path without extension>
        public static void CaptureFromCommandLine()
        {
            string output = null;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == "--fishing-demo-output")
                {
                    output = args[i + 1];
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".validation", "demo-video"));
                output = Path.Combine(folder, "FishingIsGood_Unity_Demo");
            }

            RequestCapture(output, true);
        }

        private static void RequestCapture(string outputWithoutExtension, bool exitWhenDone)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Demo video capture must be started from Edit Mode.");
                if (exitWhenDone) EditorApplication.Exit(2);
                return;
            }

            _outputWithoutExtension = Path.GetFullPath(outputWithoutExtension);
            Directory.CreateDirectory(Path.GetDirectoryName(_outputWithoutExtension));
            string existingVideo = _outputWithoutExtension + ".mp4";
            if (File.Exists(existingVideo)) File.Delete(existingVideo);

            EditorSceneManager.OpenScene(ScenePath);
            FishingV2Session sceneSession = UnityEngine.Object.FindFirstObjectByType<FishingV2Session>();
            if (sceneSession == null)
            {
                Debug.LogError("Demo video capture could not find FishingV2Session in " + ScenePath);
                if (exitWhenDone) EditorApplication.Exit(3);
                return;
            }

            // Keep the complete opening presentation in the recorded flow. This assignment
            // is runtime setup for the opened scene and is never saved to the scene asset.
            sceneSession.PlaySessionDivePresentation = true;
            SessionState.SetString(OutputKey, _outputWithoutExtension);
            SessionState.SetBool(ExitKey, exitWhenDone);
            SessionState.SetBool(RequestKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                BootstrapAfterDomainReload();
            }
            else if (change == PlayModeStateChange.EnteredEditMode && _active)
            {
                Finish(false, "Play Mode ended before capture completed.");
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += ExitIfPending;
            }
        }

        private static void BootstrapAfterDomainReload()
        {
            if (SessionState.GetBool(ExitPendingKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ExitIfPending();
                return;
            }

            if (_active || !SessionState.GetBool(RequestKey, false) || !EditorApplication.isPlaying)
            {
                return;
            }

            SessionState.EraseBool(RequestKey);
            _outputWithoutExtension = SessionState.GetString(OutputKey, string.Empty);
            _reportPath = _outputWithoutExtension + ".txt";
            _session = null;
            _phase = Phase.WaitForSession;
            _startedAt = EditorApplication.timeSinceStartup;
            _phaseStartedAt = _startedAt;
            _sawSessionLive = false;
            _sawCatchFlight = false;
            _initialArrivals = 0;
            _errors = 0;
            Records.Clear();
            Records.Add("FishingIsGood Unity portfolio demo capture");
            Records.Add("capturedAt=" + DateTime.Now.ToString("O"));
            Records.Add("scene=" + ScenePath);
            Records.Add("resolution=" + Width + "x" + Height + ";fps=" + FrameRate);
            Application.logMessageReceived += CountErrors;

            try
            {
                StartRecorder();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(false, "Recorder initialization failed: " + exception.Message);
                return;
            }

            _active = true;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("Fishing V2 portfolio demo recording started: " + _outputWithoutExtension + ".mp4");
        }

        private static void StartRecorder()
        {
            _controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            _movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            _movieSettings.name = "Fishing V2 Portfolio Demo";
            _movieSettings.Enabled = true;
            _movieSettings.CaptureAudio = false;
            _movieSettings.CaptureAlpha = false;
            _movieSettings.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
            };
            _movieSettings.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = Width,
                OutputHeight = Height
            };
            _movieSettings.OutputFile = _outputWithoutExtension.Replace('\\', '/');

            _controllerSettings.AddRecorderSettings(_movieSettings);
            _controllerSettings.SetRecordModeToManual();
            _controllerSettings.FrameRate = FrameRate;
            _controllerSettings.CapFrameRate = true;
            RecorderOptions.VerboseMode = false;
            _controller = new RecorderController(_controllerSettings);
            _controller.PrepareRecording();
            if (!_controller.StartRecording())
            {
                throw new InvalidOperationException("Unity Recorder did not start.");
            }
        }

        private static void Tick()
        {
            if (!_active) return;
            if (!EditorApplication.isPlaying)
            {
                Finish(false, "Play Mode stopped.");
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now - _startedAt > TimeoutSeconds)
            {
                Finish(false, "Timed out before a catch reached the bag.");
                return;
            }

            if (_session == null)
            {
                _session = UnityEngine.Object.FindFirstObjectByType<FishingV2Session>();
                if (_session == null) return;
                Application.runInBackground = true;
                _initialArrivals = _session.CatchFlightArrivals;
                _phase = Phase.PreCast;
                _phaseStartedAt = now;
                Records.Add("sessionFoundAt=" + Elapsed(now));
                return;
            }

            if (_session.SessionLive) _sawSessionLive = true;
            if (_session.ActiveCatchFlightCount > 0) _sawCatchFlight = true;

            switch (_phase)
            {
                case Phase.PreCast:
                    if (now - _phaseStartedAt < PreCastSeconds) return;
                    _session.CastAtPondPoint(new Vector2(0f, -1.2f));
                    Records.Add("firstCastAt=" + Elapsed(now) + ";position=0,-1.2");
                    _phase = Phase.WaitForArrival;
                    _phaseStartedAt = now;
                    break;

                case Phase.WaitForArrival:
                    if (_session.CatchFlightArrivals <= _initialArrivals) return;
                    Records.Add(
                        "arrivalAt=" + Elapsed(now) +
                        ";activeCatchFlightSeen=" + _sawCatchFlight +
                        ";sessionLiveSeen=" + _sawSessionLive +
                        ";arrivals=" + _session.CatchFlightArrivals +
                        ";tally=" + GetTally(_session));
                    _phase = Phase.FinalHold;
                    _phaseStartedAt = now;
                    break;

                case Phase.FinalHold:
                    if (now - _phaseStartedAt < FinalHoldSeconds) return;
                    Finish(true, "Capture completed after the first catch reached the bag.");
                    break;
            }
        }

        private static void Finish(bool success, string message)
        {
            bool wasActive = _active;
            _active = false;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= CountErrors;

            if (_controller != null && _controller.IsRecording())
            {
                _controller.StopRecording();
            }

            string videoPath = string.IsNullOrWhiteSpace(_outputWithoutExtension)
                ? string.Empty
                : _outputWithoutExtension + ".mp4";
            bool videoExists = !string.IsNullOrWhiteSpace(videoPath) && File.Exists(videoPath);
            long bytes = videoExists ? new FileInfo(videoPath).Length : 0L;
            int tally = _session != null ? GetTally(_session) : 0;
            int arrivals = _session != null ? _session.CatchFlightArrivals : 0;
            bool passed = success && videoExists && bytes > 0 && _errors == 0 &&
                _sawSessionLive && _sawCatchFlight && arrivals > _initialArrivals && arrivals == tally;

            Records.Add("message=" + message);
            Records.Add(
                "video=" + videoPath.Replace('\\', '/') +
                ";exists=" + videoExists +
                ";bytes=" + bytes);
            Records.Add(
                "sessionLiveSeen=" + _sawSessionLive +
                ";catchFlightSeen=" + _sawCatchFlight +
                ";arrivals=" + arrivals +
                ";tally=" + tally +
                ";errors=" + _errors +
                ";passed=" + passed);

            if (!string.IsNullOrWhiteSpace(_reportPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_reportPath));
                File.WriteAllLines(_reportPath, Records);
            }

            if (_movieSettings != null) UnityEngine.Object.DestroyImmediate(_movieSettings);
            if (_controllerSettings != null) UnityEngine.Object.DestroyImmediate(_controllerSettings);
            _controller = null;
            _movieSettings = null;
            _controllerSettings = null;

            if (passed) Debug.Log("Fishing V2 portfolio demo recording passed: " + videoPath);
            else Debug.LogError("Fishing V2 portfolio demo recording failed: " + message + " report=" + _reportPath);

            bool exitWhenDone = SessionState.GetBool(ExitKey, false);
            SessionState.EraseBool(OutputKey);
            SessionState.EraseBool(ExitKey);
            SessionState.EraseBool(RequestKey);

            if (exitWhenDone)
            {
                SessionState.SetBool(ExitPendingKey, true);
                SessionState.SetInt(ExitCodeKey, passed ? 0 : 1);
            }

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            else if (exitWhenDone) EditorApplication.delayCall += ExitIfPending;
            else if (!wasActive && !success)
            {
                Debug.LogWarning("Demo video capture cleanup completed after an interrupted run.");
            }
        }

        private static void CountErrors(string message, string stack, LogType kind)
        {
            if (kind != LogType.Error && kind != LogType.Exception && kind != LogType.Assert) return;

            // Unity Search can throw once while its editor index is being created on a fresh
            // checkout. It is outside Play Mode content and does not affect recorded frames.
            if (!string.IsNullOrEmpty(stack) && stack.Contains("UnityEditor.Search.SearchDatabase"))
            {
                Records.Add("ignoredEditorError=" + message.Replace('\n', ' '));
                return;
            }

            _errors++;
            Records.Add("runtimeError=" + message.Replace('\n', ' '));
        }

        private static void ExitIfPending()
        {
            if (!SessionState.GetBool(ExitPendingKey, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            int exitCode = SessionState.GetInt(ExitCodeKey, 1);
            SessionState.EraseBool(ExitPendingKey);
            SessionState.EraseInt(ExitCodeKey);
            EditorApplication.Exit(exitCode);
        }

        private static int GetTally(FishingV2Session session)
        {
            int tally = 0;
            foreach (int count in session.Caught.Values) tally += count;
            return tally;
        }

        private static string Elapsed(double now)
        {
            return (now - _startedAt).ToString("F3") + "s";
        }
    }
}
#endif

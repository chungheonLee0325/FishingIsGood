using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fishing.V2
{
    // Opt-in standalone evidence run. Normal launches never create this component.
    // It injects a world-space cast, not physical pointer input, and skips the opening dive.
    public sealed class FishingBuildEvidence : MonoBehaviour
    {
        private string _output;
        private int _errors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] != "--fishing-evidence") continue;
                var host = new GameObject("FishingBuildEvidence");
                host.AddComponent<FishingBuildEvidence>()._output = Path.GetFullPath(args[i + 1]);
                break;
            }
        }

        private void CountErrors(string message, string stack, LogType kind)
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) _errors++;
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += CountErrors;
            Application.runInBackground = true;
            Directory.CreateDirectory(_output);
            var lines = new List<string> { "method=standalone main-camera RenderTexture; world only, no OnGUI HUD; injected world-space cast; opening dive skipped", "capturedAtUtc=" + DateTime.UtcNow.ToString("O") };
            foreach (string name in new[] { "FishingV2/FishSurface", "FishingV2/FishShadow", "FishingV2/WaterSurface", "FishingV2/WaterRipple", "Universal Render Pipeline/Unlit" })
            {
                Shader shader = Shader.Find(name);
                bool supported = shader != null && shader.isSupported;
                lines.Add("shader=" + name + ";supported=" + supported);
                if (!supported) _errors++;
            }
            yield return null;
            FishingV2Session session = FindFirstObjectByType<FishingV2Session>();
            if (session == null) { Application.Quit(1); yield break; }
            session.PlaySessionDivePresentation = false;
            session.BeginSession();
            yield return new WaitForSeconds(0.5f);
            session.CastAtPondPoint(new Vector2(0f, -1.2f));
            float started = Time.realtimeSinceStartup;
            bool overview = false, flight = false, arrival = false;
            while (Time.realtimeSinceStartup - started < 30f)
            {
                string label = null;
                if (!overview && Time.realtimeSinceStartup - started > 2f) { overview = true; label = "gameplay"; }
                else if (!flight && session.ActiveCatchFlightCount > 0) { flight = true; label = "catch-flight"; }
                else if (!arrival && session.CatchFlightArrivals > 0) { arrival = true; label = "arrival"; }
                if (label != null)
                {
                    if (!CaptureCamera(Path.Combine(_output, label + ".png"))) _errors++;
                    lines.Add("capture=" + label + ";arrivals=" + session.CatchFlightArrivals + ";respawns=" + session.RespawnsSpawned + ";fish=" + session.Fish.Count);
                }
                if (overview && flight && arrival) break;
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            int tally = 0;
            foreach (int count in session.Caught.Values) tally += count;
            bool files = File.Exists(Path.Combine(_output, "gameplay.png")) && File.Exists(Path.Combine(_output, "catch-flight.png")) && File.Exists(Path.Combine(_output, "arrival.png"));
            bool passed = _errors == 0 && flight && arrival && files && session.CatchFlightArrivals == tally;
            lines.Add("errors=" + _errors + ";arrivals=" + session.CatchFlightArrivals + ";tally=" + tally + ";captureFiles=" + files + ";passed=" + passed);
            File.WriteAllLines(Path.Combine(_output, "report.txt"), lines);
            Debug.Log("PUBLIC_PLAYER_EVIDENCE passed=" + passed + " errors=" + _errors);
            Application.logMessageReceived -= CountErrors;
            Application.Quit(passed ? 0 : 1);
        }

        // Render offscreen explicitly: a hidden Windows swapchain can produce black
        // ScreenCapture images even while simulation and the GPU cameras are running.
        // This proves the world render only; OnGUI/HUD and physical input need a manual pass.
        private static bool CaptureCamera(string path)
        {
            Camera camera = Camera.main;
            if (camera == null) return false;
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                Color32[] colors = pixels.GetPixels32();
                byte minimum = 255, maximum = 0;
                for (int i = 0; i < colors.Length; i += 17)
                {
                    minimum = (byte)Mathf.Min(minimum, colors[i].r, colors[i].g, colors[i].b);
                    maximum = (byte)Mathf.Max(maximum, colors[i].r, colors[i].g, colors[i].b);
                }
                if (maximum - minimum < 8) return false;
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                return true;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                Destroy(pixels);
            }
        }
    }
}

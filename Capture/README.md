# Demo capture

The HTML comparison recording uses Playwright and writes a 1600×900 WebM file to
`.validation/demo-video/` by default.

```powershell
cd Capture
npm install
npx playwright install chromium
npm run html
```

The Unity recording uses Unity Recorder 5.1.7. Open the project and run
`Fishing V2 > Capture portfolio demo video`, or call the same capture from the command line:

```powershell
& 'D:/Unity/6000.3.23f1/Editor/Unity.exe' `
  -projectPath '../UnityProject' `
  -executeMethod Fishing.V2.EditorTools.FishingV2DemoVideoCapture.CaptureFromCommandLine
```

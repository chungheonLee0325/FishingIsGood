# Fishing Is Good — Top-down Fishing Prototype (Unity / Three.js)

**물고기의 움직임을 지켜보고, 찌를 놓을 위치를 고르는 탑다운 낚시 프로토타입입니다.**

반복해서 누르는 조작보다 **물고기를 관찰하고 다음 위치를 선택하는 재미**에 집중했습니다.  
처음부터 Unity에서 크게 만들기보다 HTML 프로토타입으로 행동 규칙을 빠르게 바꿔가며 직접 플레이했고, 남길 가치가 있다고 판단한 규칙만 Unity Runtime으로 옮겼습니다.

> **AI-assisted Game R&D**  
> AI는 구현 대안과 프로토타입을 빠르게 만드는 데 사용했고, 재미의 기준과 채택·폐기 판단은 직접 플레이하며 결정했습니다.

---

## 🎬 플레이 / 다운로드

| 항목 | 링크 |
| :--- | :--- |
| 브라우저에서 HTML Prototype 실행 | [Play HTML Prototype](https://chungheonlee0325.github.io/FishingIsGood/HTML/) |
| HTML 다운로드 | [HTML ZIP](https://github.com/chungheonLee0325/FishingIsGood/releases/download/v0.1.0/FishingIsGood-HTML.zip) |
| Windows x64 빌드 | [Download Windows Build](https://github.com/chungheonLee0325/FishingIsGood/releases/download/v0.1.0/FishingIsGood-Windows-x64.zip) |
| Release | [v0.1.0](https://github.com/chungheonLee0325/FishingIsGood/releases/tag/v0.1.0) |
| HTML → Unity 비교 영상 | 촬영 후 추가 예정 |

Windows 빌드는 ZIP 전체를 압축 해제한 뒤 `FishingIsGood.exe`를 실행하면 됩니다.

**기본 조작**

- 수면 클릭: 찌 배치
- 물고기가 입질하면 자동 회수
- 입질 후 직접 클릭하면 즉시 회수

Unity Editor 또는 Development Build에서는 `R`로 세션을 재시작하고 `T`로 시작 연출을 건너뛸 수 있습니다.

![Unity Gameplay](Media/unity-gameplay.png)

*공개 Windows 빌드의 월드 카메라 렌더. 검증 도구로 찌를 배치한 장면이며 HUD는 제외되어 있습니다.*

---

## ✨ 프로젝트 개요

처음 정한 방향은 단순했습니다.

> **가만히 보고 있는 시간도 재미있는 낚시 게임을 만들 수 있을까?**

물고기의 움직임, 찌를 던졌을 때의 반응, 먹이를 두고 경쟁하는 모습처럼 **플레이어가 개입하지 않는 순간에도 볼거리가 생기는지**를 먼저 확인했습니다.

문제는 이런 감각적인 요소를 코드 구조만 보고 판단하기 어렵다는 점이었습니다. 그래서 HTML을 실험 공간으로 사용했습니다.

```text
재미에 대한 질문
    ↓
HTML에서 빠르게 구현
    ↓
버전별 직접 플레이 / 관찰
    ↓
유지 / 폐기 / 다시 설계
    ↓
Unity Runtime으로 이관
```

HTML의 역할은 완성 게임을 만드는 것이 아니라, **아이디어를 빠르게 움직여 보고 다음 판단을 내리는 것**이었습니다.

---

## 🧪 HTML Prototype에서 검증한 것

### 1. 반복 터치보다 물고기를 보는 재미

초기에는 플레이어가 자주 탭할수록 이득을 얻는 방향도 검토했습니다.

방치형의 재미를 반복 터치보다 물고기의 움직임을 보는 데 두고 싶었습니다. 그래서 **자동 회수를 기본으로 두고, 플레이어의 주요 선택은 찌를 어디에 놓을지로 좁혔습니다.**

이 결정은 Unity 버전에도 그대로 남아 있습니다.

---

### 2. 찌를 던졌을 때 전부 도망가야 할까?

초기 버전에서는 찌를 옮길 때 주변 물고기가 크게 흩어졌습니다. 직접 개입에 작은 보상이 돌아오도록, 이 도피 반응을 줄이고 찌 배치를 주요 동작으로 다듬었습니다.

그래서 가까운 개체의 도피와 주변 개체의 관심을 분리하고, **찌를 옮기는 행동에 작은 위험과 작은 보상이 함께 돌아오도록** 조정했습니다.

Unity에서는 도피와 유인을 별도 규칙으로 두고, 어종별 반응 확률과 반복 착수에 대한 피로도를 데이터로 관리합니다.

---

### 3. 회피를 추가했더니 움직임이 더 어색해졌다

물고기끼리 자연스럽게 비키게 하려고 회피 행동을 추가했지만, 위치 변화가 방향 계산에 계속 영향을 주면서 선회가 떨리는 문제가 생겼습니다.

단순히 감쇠 값을 더 키우는 대신 **회피 목표와 실제 오프셋을 분리하고, 목표를 단계적으로 따라가도록 다시 구성했습니다.**

이 과정에서 "기능이 추가됐는가"보다 **실제로 화면에서 자연스럽게 읽히는가**를 기준으로 구현을 버리거나 다시 만들었습니다.

---

### 4. 한 번에 비교할 수 있도록 기능을 모듈화

HTML 버전을 반복해서 개선한 과정은 현재 파일의 Toggle과 비교 프리셋으로 살펴볼 수 있습니다. 당시 개발은 버전별 반복 작업이었고, 아래 도구는 지금 차이를 확인하는 방법입니다.

예를 들어 다음 요소를 따로 켜고 끄며 차이를 볼 수 있습니다.

- 좌우 재중심
- 물고기끼리 회피
- 정점 휨
- 어종별 Motion / 먹이 반응
- 먹이 경쟁 행동
- Bite 순간 피드백

먹이 경쟁은 Bite Commitment를 켠 상태에서 비교합니다. After-Bite 후속 행동은 일반 어획과 별도의 방생·검증 경로로 다룹니다.

또한 **머리 감쇠 → 슬립 상한 → 사행 주기 변경**처럼 한 문제에 대한 여러 개선 단계를 같은 화면에서 비교할 수 있게 했습니다.

[▶ HTML Prototype 바로 실행](https://chungheonlee0325.github.io/FishingIsGood/HTML/)

---

## 🚀 HTML에서 Unity Runtime으로

HTML 구현을 그대로 포팅하지는 않았습니다.

HTML에서 플레이해 보고 남기기로 한 **행동의 의도와 데이터**를 Unity의 상태와 Runtime 시스템으로 다시 구성했습니다.

| 검증한 행동 | Unity Runtime |
| :--- | :--- |
| 찌 배치 · 입질 · 자동/즉시 회수 | `FishingV2Session`, `BobberV2` |
| 상태별 행동 · 선회 · 무리 · 회피 | `FishAgentV2`, `FishState` |
| 어종별 움직임 · 먹이 · 경쟁 | `FishSpeciesConfig`, `MotionSignature`, `FeedSignature`, `ContestSignature` |
| 접근 방식 · 입질 후 행동 | `ApproachStyle`, `AfterBiteProfile` |
| 회수 비행 · 도착 집계 · 리스폰 | `CatchFlightV2` |
| 절차 메시 · 몸의 굽힘 | `FishMeshBuilderV2`, `FishVisualSpec`, `FishSurfaceV2` |

실제 플레이 어종은 **멸치, 연어, 만새기, 오징어, 참치** 5종이며, 일부 추가 어종은 동작 검증에 사용합니다.

---

## 🤖 AI 활용 방식

AI를 완성본을 한 번에 생성하는 도구보다는 **더 많은 가설을 짧은 시간 안에 움직여 보는 개발 도구**로 사용했습니다.

| AI-assisted | 직접 판단한 부분 |
| :--- | :--- |
| 구현 대안 탐색 | 어떤 재미를 만들지 정의 |
| HTML Prototype 초안 | 어떤 가설을 먼저 확인할지 선택 |
| 반복 코드와 구조 변경 | 직접 플레이하고 문제를 발견 |
| 비교 도구 / 검증 코드 작성 보조 | 채택 · 폐기 · 재설계 결정 |
| 오류 원인 후보 탐색 | Unity에 남길 규칙과 구조 결정 |

AI가 제안하거나 작성한 결과도 그대로 사용하지 않고, 실제 플레이와 실행 결과를 기준으로 다시 확인했습니다.

현재 게임의 물고기 행동은 **규칙 기반 시뮬레이션**이며 Runtime Deep Learning 모델은 사용하지 않습니다.

---

## ✅ Runtime 검증

HTML에서 빠르게 결정한 규칙을 Unity로 옮긴 뒤에는 반복 실행 가능한 검증 경로를 추가했습니다.

Unity **6000.3.23f1**, seed `20260901`, `5,400 step × 1/60초` 조건의 90초 시뮬레이션 결과입니다.

| 항목 | 기본 Catch | After-Bite 방생 |
| :--- | ---: | ---: |
| Error / Exception / Assert | 0 | 0 |
| 큰 위치 점프 (>0.75 world units) | 0 | 0 |
| Catch 도착 / 집계 | 18 / 18 | 9 / 9 |
| 리스폰 실행 / 예약 | 18 / 18 | 9 / 9 |

이 검증은 **Runtime 상태 전이와 회수 흐름을 확인하기 위한 Smoke Test**이며, 게임의 재미를 수치로 증명하기 위한 테스트는 아닙니다.

위 표는 검증용 어종을 포함한 고정 스텝 시뮬레이션입니다. 일반 플레이에는 프레임 dt를 사용합니다. 별도로 수행한 Windows 빌드의 렌더·어획 결과는 [실행 증거](Media/unity-evidence.txt)에서 확인할 수 있습니다.

---

## 🎥 비교 영상

촬영 예정인 영상은 두 가지입니다.

### HTML Experiment
하나의 화면에서 움직임 관련 프리셋과 Toggle을 바꾸며 어떤 차이가 생기는지 비교합니다.

### HTML → Unity
찌 배치 → 접근 → 입질 → 회수의 동일한 플레이 흐름이 HTML Prototype에서 Unity Runtime으로 어떻게 옮겨졌는지 비교합니다.

영상이 준비되면 이 README 상단의 Play / Download 영역에 바로 연결할 예정입니다.

---

## 🛠️ 소스에서 실행

### HTML

`HTML/index.html`을 브라우저에서 열거나 아래 링크로 바로 실행할 수 있습니다.

[Play HTML Prototype](https://chungheonlee0325.github.io/FishingIsGood/HTML/)

### Unity

1. Unity Hub에서 `UnityProject/`를 **Unity 6000.3.23f1**로 엽니다.
2. [FishingV2Prototype Scene](UnityProject/Assets/FishingV2/Scenes/FishingV2Prototype.unity)을 엽니다.
3. Play를 실행합니다.

자동 검증과 Windows x64 빌드도 공개 프로젝트에서 재실행할 수 있습니다.

```powershell
$unityEditor = 'D:/Unity/6000.3.23f1/Editor/Unity.exe'

& $unityEditor `
  -batchmode `
  -projectPath "$PWD/UnityProject" `
  -executeMethod Fishing.V2.EditorTools.FishingPublicBuild.ValidateAndBuildWindows `
  -logFile "$PWD/public-build.log"
```

---

## 📌 공개 범위

이 저장소는 기존 개발 작업에서 **게임플레이 실험 과정과 Unity Runtime을 정리한 공개용 스냅샷**입니다.

- HTML Prototype과 Unity Project를 함께 제공합니다.
- Runtime Deep Learning 모델은 포함하지 않습니다.
- HTML은 Three.js r128을 사용하며 관련 MIT 고지는 [Three.js 라이선스](ThirdParty/three.js-LICENSE.txt)에 포함되어 있습니다.
- 프로젝트 자체의 별도 오픈소스 라이선스는 현재 지정하지 않았습니다.

---

## 🔗 Related

- [Project Gallery](https://github.com/chungheonLee0325)

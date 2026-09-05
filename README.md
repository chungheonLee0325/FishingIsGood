# Fishing Is Good

**물고기의 움직임을 지켜보고, 찌를 놓을 위치를 고르는 탑다운 낚시 프로토타입.**

반복해서 누르는 것보다 **보는 재미**에 집중했습니다. 직접 개입할 때는 적은 위험과 작은 보상이 돌아오도록, 찌를 옮길 때 과하게 도망가던 반응을 줄였습니다. HTML 버전을 반복해서 만들고 직접 플레이하며 규칙을 다듬은 뒤 Unity Runtime으로 옮겼습니다.

**AI-assisted Game R&D · HTML Prototype → Unity Runtime**  
현재 게임은 규칙 기반 시뮬레이션입니다. Runtime Deep Learning 모델은 사용하지 않습니다.

## Play / Download

| 바로 확인하기 | 링크 |
|---|---|
| 브라우저에서 HTML Prototype 실행 | [Play HTML Prototype](https://fire6376-spec.github.io/FishingIsGood/HTML/) |
| HTML 다운로드 | [HTML ZIP](https://github.com/fire6376-spec/FishingIsGood/releases/download/v0.1.0/FishingIsGood-HTML.zip) · [HTML 원본](HTML/index.html) |
| Windows x64 프로토타입 | [Download Windows Build](https://github.com/fire6376-spec/FishingIsGood/releases/download/v0.1.0/FishingIsGood-Windows-x64.zip) |
| 릴리스와 파일 검증값 | [v0.1.0](https://github.com/fire6376-spec/FishingIsGood/releases/tag/v0.1.0) |
| HTML → Unity 비교 영상 | **촬영 준비 중** — 아직 영상 증거는 게시하지 않았습니다. |

Windows ZIP은 **전체를 압축 해제**한 뒤 `FishingIsGood.exe`를 실행합니다. `_Data` 폴더와 DLL은 실행 파일 옆에 있어야 합니다. Unity Editor는 필요하지 않습니다.

**조작:** 수면을 클릭하면 찌를 배치합니다. 물고기가 물면 자동으로 회수되고, 먼저 클릭해 즉시 회수할 수도 있습니다. Unity는 첫 찌 배치에 수면 진입 연출이 연결됩니다. 개발용 단축키는 `R` 세션 재시작, `T` 연출 건너뛰기입니다.

![Windows 빌드의 물고기·수면·찌 렌더링](Media/unity-gameplay.png)

*2026-09-05 공개 Windows 빌드의 메인 카메라 캡처. 검증 도구로 찌를 주입하고 첫 연출을 건너뛴 월드 렌더이며 OnGUI HUD는 제외되어 있습니다. [어획 비행](Media/unity-catch-flight.png) · [도착 시점](Media/unity-arrival.png) · [검증 상태](Media/unity-evidence.txt).*

## 왜 HTML부터 만들었나

물고기가 자연스럽게 움직이는지, 찌를 놓은 뒤 기다릴 이유가 생기는지, 터치를 하지 않아도 즐거운지는 코드 구조만으로 판단하기 어려웠습니다. HTML은 구현한 버전을 바로 열어 보고 다음 변경을 요청하기 좋은 실험 공간이었습니다.

```text
재미에 대한 질문 → 구현 가설 → AI와 HTML 버전 제작
                              ↓
                        직접 플레이·관찰
                              ↓
                    유지 / 폐기 / 다시 설계
                              ↓
                  Unity의 상태·데이터·실행 흐름으로 이관
```

실제 개발은 **HTML 버전을 바꿔가며 반복**한 과정입니다. 현재 파일에 남은 Toggle과 프리셋은 방문자가 차이를 확인할 수 있는 비교 도구입니다. 당시 모든 실험을 Toggle A/B로 진행했다는 뜻은 아닙니다.

## 직접 비교해 볼 것

### 1. 머리 흔들림을 줄이면 움직임도 좋아질까?

HTML의 **머리 감쇠 → 슬립 상한 → 사행 주기 2배** 프리셋을 차례로 선택해 보세요. 머리 방향과 실제 이동 방향, 큰 물고기의 선회, 사행 어종의 흔들림을 관찰할 수 있습니다. ‘예전’과 ‘꼬리 평활화’도 비교용으로 남아 있습니다.

현재 개체를 유지하며 설정을 바꾸지만, 이는 동일 seed로 재생하는 기록된 리플레이는 아닙니다. 프리셋 안의 수치는 당시 기록이며 현재 버전에서 다시 측정한 성능 보장이 아닙니다.

### 2. 같은 화면의 어색함도 원인이 다르다

| 현재 HTML 옵션 | 관찰할 차이 |
|---|---|
| 좌우 재중심 | 몸이 어느 지점을 축으로 흔들리는가 |
| 물고기끼리 회피 | 개체 간 회피가 경로와 방향에 주는 영향 |
| 정점 휨 사용 | 몸의 굽힘이 관찰 감각에 주는 영향 |
| 어종별 Motion / 먹이 반응 Signature | 같은 상황에서도 어종별 박자와 판단이 다른가 |
| 먹이 경쟁 행동 | 양보·파고들기·추격이 접근 과정에서 읽히는가 |

먹이 경쟁은 **Bite Commitment가 켜져 있고 여러 개체가 접근하는 상황**에서 비교합니다. 기존 경쟁 상태가 즉시 사라지는 스위치는 아닙니다. 어종 선택을 바꾸면 개체가 재배치됩니다.

현재 UI에는 25개의 고정 Toggle이 있습니다. 자동 회수는 별도 UI Toggle이 아닌 기본 동작입니다. After-Bite 5모드는 일반 어획과 다른 경로를 사용하므로 기본 플레이에서 항상 보이는 행동으로 소개하지 않습니다.

## Decision Log

### 01 — 반복 터치보다 물고기를 보는 재미

**Question / Hypothesis:** 방치형의 재미를 반복 터치에 두어야 할까? 물고기 움직임을 보는 것 자체에 재미를 두고, 개입은 작은 위험과 보상을 주는 쪽이 목표에 맞았습니다.

**AI-assisted Prototype / Comparison:** HTML 버전을 반복 구현하며 자동 회수, 즉시 회수, 찌 배치가 만드는 흐름을 확인했습니다. 초기 설계 기록에는 탭을 이득으로 만들려던 안도 남아 있습니다.

**Evidence / Decision:** 직접 판단한 방향은 ‘더 자주 눌러야 하는 게임’이 아니었습니다. **자동 회수를 기본으로 유지하고 찌 배치를 주요 조작으로 정했습니다.** 과거 봇 세션의 점수 차이를 현재 빌드의 밸런스 보장으로 사용하지는 않습니다.

**Result:** Unity에서도 찌 배치, 기본 자동 회수, 선택적 즉시 회수가 남았습니다. 보상은 물고기가 바구니에 도착할 때 집계합니다.

### 02 — 찌 배치에 돌아오는 과한 거절 반응을 줄이기

**Question / Hypothesis:** 찌를 옮길 때 물고기가 과하게 도망가면 직접 개입의 첫 반응이 약해집니다. 배치의 위험을 줄이되 관찰과 위치 선택의 의미를 남기고자 했습니다.

**AI-assisted Prototype / Comparison:** 여러 HTML 버전에서 착수·도피·관심 반응을 구현하고 조정했습니다. 근거리/중거리 Toggle을 독립적으로 조작한 과거 실험으로 서술하지 않습니다.

**Evidence / Decision:** 직접 개입에 작은 보상이 돌아오는 쪽으로 반응을 조정했습니다. 설계에는 가까운 개체의 도피와 주변 개체의 관심을 나누는 판단이 남아 있습니다.

**Result:** Unity는 도피와 유인을 구분하고, 어종별 유인 확률·반복 착수 피로도·반응 보장을 처리합니다. 최신 HTML의 인지 반경 처리와 완전히 같은 구현은 아닙니다. **보존한 의도와 이관 과정의 구현 차이를 함께 봅니다.**

### 03 — 회피를 넣었더니 드드득하는 선회가 생겼다

**Question / Hypothesis:** 물고기가 서로 비키면 자연스러워질 것이라고 봤지만, 새 회피가 이동을 흔들 수 있었습니다.

**AI-assisted Prototype / Comparison:** AI가 작성한 회피 오프셋의 적분·감쇠 방식에서 떨림 문제가 기록되었습니다. 버전별 결과를 확인했고, 현재의 재중심·회피 옵션은 서로 다른 원인을 설명하는 데 쓸 수 있습니다.

**Evidence / Decision:** 피함 → 멀어짐 → 감지 끊김 → 되돌아옴이 반복되고, 위치의 작은 변화가 머리 방향에 영향을 주었습니다. **적분 방식을 버리고 목표에 수렴하는 2단 평활화로 재설계했습니다.**

**Result:** HTML과 Unity에 회피 목표와 실제 오프셋을 단계적으로 따라가는 처리가 남았습니다. 당시의 각가속도·선회 반전 수치는 이번 공개판의 신규 측정 결과로 제시하지 않습니다.

### 04 — 머리 흔들림을 줄이다가 옆으로 미끄러졌다

**Question / Hypothesis:** 머리 감쇠와 평활화로 흔들림을 줄이면 더 자연스럽게 보일까?

**AI-assisted Prototype / Comparison:** 감쇠, 슬립 제한, 사행 주기 변경을 구현 대안으로 다뤘습니다. 현재의 5단계 비교 프리셋에 개선안과 부작용 설명이 남아 있습니다.

**Evidence / Decision:** 머리가 잠잠해져도 실제 이동 방향에서 크게 벗어나면 옆으로 미끄러져 보입니다. **감쇠만으로 해결하려던 접근을 재설계하고, 완만한 궤도와 슬립 제한을 조합했습니다.**

**Result:** Unity의 방향 추종·슬립 제한·평활화 설정에 이어집니다. 최종 코드에는 감쇠도 남아 있으므로 ‘감쇠를 전부 제거했다’는 설명은 정확하지 않습니다.

## HTML에서 Unity에 남은 것

| 행동과 규칙 | Unity Runtime |
|---|---|
| 찌 배치·입질·자동/즉시 회수 | `FishingV2Session`, `BobberV2` |
| 상태별 행동·선회·무리·회피 | `FishAgentV2`, `FishState` |
| 어종별 움직임·먹이·경쟁 | `FishSpeciesConfig`, `MotionSignature`, `FeedSignature`, `ContestSignature` |
| 접근 방식 조합·후속 행동 | `ApproachStyle`, `AfterBiteProfile` |
| 회수 비행·도착 집계·리스폰 | `CatchFlightV2`, Session의 도착 콜백과 계수 |
| 절차 메시·몸의 굽힘 | `FishMeshBuilderV2`, `FishVisualSpec`, `FishSurfaceV2` |

HTML의 DOM·CSS·Three.js를 그대로 이식한 것이 아니라, 행동 의도와 데이터를 Unity의 상태·시뮬레이션·연출 흐름으로 옮겼습니다. 실제 플레이 어종은 멸치·연어·만새기·오징어·참치 5종입니다. needle/minnow/disc는 별도 검증용입니다.

## AI와 사람의 역할

| AI-assisted | Human-owned |
|---|---|
| 구현 대안·HTML 버전 초안 | 핵심 재미와 관찰 기준 정의 |
| 반복 코드 작성·구조 변형 | 직접 플레이와 시각적 문제 제기 |
| 행동 이관·검증 코드 보조 | 채택·폐기·재설계 판단 |
| 오류 원인 탐색과 비교 도구 | 결과를 다시 확인하고 남길 규칙 결정 |

AI를 한 번에 완성본을 생성하는 도구보다, 더 많은 가설을 빠르게 구현하고 확인하는 협업 도구로 사용했습니다. AI가 작성한 설명이나 수치도 현재 소스·실행 결과와 대조했습니다.

## 검증과 현재 범위

Unity **6000.3.23f1**. 대표 Scene은 [FishingV2Prototype](UnityProject/Assets/FishingV2/Scenes/FishingV2Prototype.unity)입니다. 공개본은 Unity MCP 없이 import/build할 수 있도록 개발 도구 의존성을 정리했고, 런타임에서 조회하는 셰이더를 빌드 포함 목록에 명시했습니다.

2026-09-05 공개 복사본에서 기존 검증을 다시 실행했습니다.

| 90초 시뮬레이션 | 기본 Catch | After-Bite 방생 |
|---|---:|---:|
| Error / Exception / Assert | 0 | 0 |
| 큰 위치 점프 (>0.75 world units) | 0 | 0 |
| 최대 화면 영역 내 이동/step | 0.3787 | 0.3977 |
| Catch 도착 / 집계 | 18 / 18 | 9 / 9 |
| 리스폰 실행 / 예약 | 18 / 18 | 9 / 9 |
| After-Bite frame 누적 | 0 | 160 |

검증 조건은 seed `20260901`, 5,400 step × 1/60초, 실제 5종+검증용 3종입니다. 저장 부작용을 피하기 위해 검증 Spot의 제한 시간은 600초로 둡니다. **전체 세션 종료·저장·결과 화면 검사나 재미의 수치적 증명은 아닙니다.** 일반 플레이는 제한된 프레임 dt를 사용합니다.

배포 빌드의 화면·셰이더 확인은 opt-in evidence 실행으로 별도 수행합니다. 이 도구는 첫 연출을 건너뛰고 월드 좌표로 찌를 주입하며, 메인 카메라를 RenderTexture로 촬영합니다. **월드 렌더링 증거이며 OnGUI HUD와 물리적인 마우스 입력 검증은 포함하지 않습니다.** 사용자 비교 영상과 장시간 플레이 평가는 아직 준비 중입니다.

## 소스에서 실행·빌드

1. Unity Hub에서 **`UnityProject/`**를 Unity 6000.3.23f1로 엽니다. 저장소 루트를 Unity 프로젝트로 열지 않습니다.
2. Windows Build Support를 설치하고 대표 Scene을 열어 Play합니다.
3. 자동 검증+Windows x64 빌드는 저장소 루트에서 다음처럼 실행합니다. Editor 경로는 본인 설치 위치에 맞춥니다.

```powershell
$unityEditor = 'D:/Unity/6000.3.23f1/Editor/Unity.exe'
& $unityEditor -batchmode -projectPath "$PWD/UnityProject" -executeMethod Fishing.V2.EditorTools.FishingPublicBuild.ValidateAndBuildWindows -logFile "$PWD/public-build.log"
```

결과는 `Builds/Windows/`에 생성됩니다. Unity Editor에서 메뉴 검증을 실행할 때도 먼저 저장된 Scene을 열어 두어야 합니다.

선택적 배포 빌드 증거 실행:

```powershell
& './Builds/Windows/FishingIsGood.exe' --fishing-evidence "$PWD/Builds/evidence" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile "$PWD/Builds/evidence-player.log"
```

정상 플레이에는 이 인자를 넣지 않습니다. 증거 실행은 `gameplay`, `catch-flight`, `arrival` 이미지와 `report.txt`를 만든 뒤 종료합니다.

## 다음 공개 증거

- **현재 HTML 움직임 비교, 30~45초:** 머리 감쇠 → 슬립 상한 → 사행 주기 2배. 현재 도구를 이용한 회고용 비교로 표시.
- **HTML → Unity, 60~90초:** 찌 배치/접근 → 입질/회수 → 도착/집계. 서로 다른 엔진의 동일 seed 리플레이라고 표현하지 않고 사건 순서로 비교.

## 라이선스와 외부 구성요소

프로젝트 자체의 오픈소스 라이선스는 **아직 지정하지 않았습니다**. 공개 저장소로 제공한다는 사실과 별개의 사용·재배포 허락을 임의로 추가하지 않았습니다.

- HTML에 포함된 **Three.js r128**의 MIT 고지는 [ThirdParty/three.js-LICENSE.txt](ThirdParty/three.js-LICENSE.txt)에 보존했습니다. 해당 고지는 Three.js에 적용되며 이 프로젝트 전체의 라이선스가 아닙니다.
- HTML은 Google Fonts의 IBM Plex 글꼴 CSS를 요청합니다. Three.js와 게임 코드는 HTML에 포함되어 있지만 완전한 무통신 파일이라고 소개하지 않습니다.
- Unity Editor·Runtime·패키지는 각 제공자의 약관과 라이선스를 따릅니다. 필요한 패키지는 manifest와 lock에 기록되어 있습니다.

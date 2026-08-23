# 이세계 트럭 키우기 UI 리디자인 가이드

## 1. 문서 목적

이 문서는 현재 Unity 구현을 기준으로 다음 목표를 달성하기 위한 가이드다.

- 트럭 이동, 몬스터, 보상, 업그레이드, 환생, 저장 등 게임 핵심 동작은 변경하지 않는다.
- HUD, 팝업, 버튼, 색상, 폰트, 아이콘, 레이아웃 등 프론트엔드 UI만 원하는 디자인으로 교체한다.
- 시각 요소가 게임 시스템을 직접 알지 않도록 UI 내부의 의존성을 분리한다.
- 현재 플레이 감각, 입력 차단, 게임 뷰포트 대응을 유지한다.

> 결론: 가장 안전한 방법은 기존 `GameUIController`와 `RebirthUIController`의 외부 계약을 유지한 채, 그 안쪽을 **게임 연결용 Adapter/Presenter**와 **순수 시각 View**로 나누는 것이다. 그러면 `GameManager`를 포함한 핵심 코드는 건드리지 않고 UI 디자인을 반복 교체할 수 있다.

---

## 2. 현재 저장소 구조 요약

### 2.1 기준 구현

현재 기준 구현은 아래 Unity 프로젝트다.

```text
Unity/Isekai_Truck/
```

- Unity: `6000.3.8f1`
- Render Pipeline: URP `17.3.0`
- Input System: `1.18.0`
- UI: uGUI `2.0.0` (`Canvas`, `UnityEngine.UI.Text`, `Image`, `Button`)
- 실제 게임 씬: `Assets/IsekaiTruck/Scenes/Main.unity`
- 카메라 게임 영역 비율: `10:16`
- Canvas 기준 해상도: `1080 x 1920`, Width/Height Match `0.5`

루트의 아래 파일들은 이전 Three.js 웹 버전이다.

```text
index.html
game.js
style.css
modules/
data/
```

과거 동작을 비교할 때 참고할 수는 있지만, 새 UI 작업의 구현 대상은 아니다.

### 2.2 Unity 주요 디렉터리

```text
Assets/IsekaiTruck/
├── Blessings/              # 축복 정의 및 카탈로그 에셋
├── Config/                 # GameConfig ScriptableObject
├── Data/                   # 몬스터 JSON
├── Editor/                 # 단계별 씬/기능 생성 및 검증 도구
├── Materials/              # 현재 머티리얼
├── Prefabs/Monsters/       # 몬스터 프리팹
├── Scenes/Main.unity       # 실제 게임 씬
└── Scripts/
    ├── Blessings/          # 축복 보유, 장착, 분해
    ├── Camera/             # 카메라 추적 및 뷰포트
    ├── Config/             # 밸런스 데이터 타입
    ├── Core/               # 시스템 조립과 메인 루프
    ├── Input/              # 드래그 조이스틱
    ├── Monsters/           # 몬스터 데이터, AI, 관리
    ├── Player/             # 레벨, EXP, 영혼, 포인트
    ├── Rebirth/            # 환생 진행
    ├── Save/               # PlayerPrefs 저장/로드
    ├── Spawn/              # 몬스터 스폰
    ├── Truck/              # 트럭 이동과 최종 능력치
    ├── UI/                 # 현재 HUD와 환생 UI
    ├── Upgrades/           # 트럭 업그레이드 명령
    └── World/              # 월드 타일, Fog, 표시 범위
```

현재 `BlessingLoadoutSystem`과 `BlessingDismantleSystem` 코드는 존재하지만 `GameManager` 및 현재 UI에는 아직 연결되어 있지 않다. 새 UI를 만든다는 이유로 이 기능까지 임의로 연결하면 기능 범위가 달라지므로 별도 요청으로 취급한다.

---

## 3. 현재 UI 구성

현재 UI는 별도 UI 프리팹이 아니라 `Main.unity`의 `Game Canvas` 아래에 직접 직렬화되어 있다.

```text
Game Canvas
├── Input Surface
│   └── Joystick
├── Game UI
│   └── Game Area UI
│       ├── Player HUD
│       │   ├── Level Text
│       │   ├── Soul Text
│       │   ├── Point Text
│       │   ├── EXP Text
│       │   ├── EXP Bar / EXP Fill
│       │   └── Open Upgrade Button
│       └── Upgrade Panel
│           └── Upgrade Box
│               ├── Speed Upgrade Button
│               ├── Size Upgrade Button
│               └── Close Button
└── Rebirth UI
    └── Rebirth Game Area
        ├── Open Rebirth Button
        └── Rebirth Panel
            └── Rebirth Box
                ├── Tier Panel / Tier 1~10
                ├── Candidate Panel / Candidate 1~3
                ├── Confirm Rebirth Button
                └── Close Rebirth Button
```

### 현재 표시 데이터와 명령

| UI 영역 | 읽는 데이터 | 보내는 명령 |
|---|---|---|
| 플레이어 HUD | 레벨, EXP, 필요 EXP, 영혼, 업그레이드 포인트 | 없음 |
| 업그레이드 창 | 남은 포인트, 속도 레벨/최대 속도, 크기 레벨/실제 크기 | 속도 업그레이드, 크기 업그레이드 |
| 환생 상태 | 보상 배율, 최대 환생 레벨, 보유 축복 수 | 없음 |
| 환생 단계 | 단계별 필요 레벨, 잠금/선택 가능 여부 | 단계 선택, 환생 시작 |
| 축복 후보 | 등급, 이름, 설명, 보유 수 | 후보 선택 및 환생 완료 |

---

## 4. 현재 의존 관계 분석

```text
GameManager
├── GameUIController.Initialize(...)
├── RebirthUIController.Initialize(...)
├── 두 UI의 패널 열림 상태 확인
└── 뷰포트 변경을 두 UI에 전달

GameUIController
├── PlayerState 상태 구독
├── TruckController 능력치 조회
├── TruckUpgradeSystem 명령 호출
└── JoystickInput 활성/비활성 전환

RebirthUIController
├── PlayerState 상태 구독
├── RebirthSystem 상태 구독 및 명령 호출
├── BlessingSystem 상태/후보 조회
└── JoystickInput 활성/비활성 전환
```

### 이미 잘 분리된 부분

- UI가 EXP, 레벨업, 업그레이드 수치, 환생 보상 계산을 직접 수행하지 않는다.
- `PlayerState.StateChanged`, `TruckUpgradeSystem.UpgradeApplied`, 환생/축복의 `StateChanged` 이벤트로 화면을 갱신한다.
- 업그레이드는 UI가 트럭 값을 직접 바꾸지 않고 `TruckUpgradeSystem`에 요청한다.
- 환생은 UI가 상태를 직접 수정하지 않고 `RebirthSystem`에 요청한다.

### 현재 결합된 부분

- `GameManager`가 `GameUIController`, `RebirthUIController` 구체 타입을 직접 보유한다.
- 두 UI 컨트롤러가 게임 시스템 타입과 실제 `Text`, `Image`, `Button` 참조를 동시에 보유한다.
- UI 컨트롤러가 `JoystickInput`까지 직접 켜고 끈다.
- 패널 표시 상태가 게임 루프 중지 조건으로 사용된다.

따라서 **현재 코드를 한 줄도 바꾸지 않은 상태에서 완전한 무의존 UI를 만드는 것은 불가능**하다. 대신 핵심 코드는 그대로 두고, 의존성이 있는 부분을 UI 경계의 Adapter에만 남기는 것이 현실적인 목표다.

---

## 5. 변경하면 안 되는 핵심 계약

코어를 수정하지 않으려면 아래 공개 계약은 유지해야 한다.

### `GameUIController`

```csharp
public bool IsUpgradePanelOpen { get; }

public void Initialize(
    PlayerState state,
    TruckController truck,
    TruckUpgradeSystem upgrades,
    JoystickInput input,
    CameraController cameraController
);

public void SetViewport(Rect viewport);
```

### `RebirthUIController`

```csharp
public bool IsPanelOpen { get; }

public void Initialize(
    RebirthSystem rebirth,
    BlessingSystem blessings,
    PlayerState player,
    JoystickInput input,
    CameraController cameraController
);

public void SetViewport(Rect viewport);
```

`GameManager.Update()`는 두 패널 중 하나라도 열려 있으면 트럭, 카메라, 월드, 몬스터, 스폰 업데이트를 건너뛴다. 이 동작은 단순한 UI 표시가 아니라 현재 게임의 일시 정지 규칙이므로 반드시 보존해야 한다.

또한 패널을 열 때 조이스틱을 비활성화하고, 닫을 때 다시 활성화하는 동작도 유지해야 한다. 환생 후보가 생성된 상태에서는 후보를 선택하기 전까지 환생 창을 닫을 수 없다.

---

## 6. 권장 구조

### 6.1 책임 분리

```text
게임 시스템
   │ 상태 이벤트 / Snapshot / 명령 결과
   ▼
GameUIController, RebirthUIController
(Adapter/Presenter: 게임과 UI를 연결하는 유일한 경계)
   │ ViewModel 전달 / 사용자 의도 수신
   ▼
GameHudView, UpgradePanelView, RebirthPanelView
(View: Text, Image, Button, Animator, 색상, 레이아웃만 담당)
   │
   ▼
UI Prefab + Sprite + Font + Material
```

핵심 규칙은 다음과 같다.

- View는 `PlayerState`, `TruckController`, `RebirthSystem`, `BlessingSystem`을 참조하지 않는다.
- View는 전달받은 표시 모델을 렌더링하고 버튼 클릭 의도만 알린다.
- Adapter/Presenter만 게임 시스템의 이벤트를 구독하고 명령 메서드를 호출한다.
- 게임 수치 계산과 성공 여부 판단은 기존 시스템에 남긴다.
- View가 `GameObject.Find`, `FindFirstObjectByType`, Singleton으로 게임 시스템을 찾지 않는다.

### 6.2 권장 폴더

기존 컨트롤러 파일은 Unity `.meta`와 씬 참조 보존을 위해 이동하지 않는 편이 안전하다.

```text
Assets/IsekaiTruck/
├── Art/UI/
│   ├── Fonts/
│   ├── Icons/
│   ├── Sprites/
│   └── Materials/
├── Prefabs/UI/
│   ├── GameHud.prefab
│   ├── UpgradePanel.prefab
│   └── RebirthPanel.prefab
└── Scripts/UI/
    ├── GameUIController.cs          # 기존 외부 계약 유지, Adapter 역할
    ├── RebirthUIController.cs       # 기존 외부 계약 유지, Adapter 역할
    ├── Views/
    │   ├── GameHudView.cs
    │   ├── UpgradePanelView.cs
    │   └── RebirthPanelView.cs
    └── Models/
        ├── PlayerHudViewModel.cs
        ├── TruckUpgradeViewModel.cs
        └── RebirthViewModel.cs
```

현재 프로젝트 전체 스크립트가 기본 `Assembly-CSharp`에 있으므로 UI만 별도 `.asmdef`로 분리하면 기존 코어 타입을 바로 참조할 수 없다. 코어를 건드리지 않는 이번 범위에서는 폴더와 참조 규칙으로 경계를 유지하고, Assembly 분리는 별도 구조 개선 작업으로 남기는 편이 안전하다.

### 6.3 View의 형태 예시

View는 게임 타입 대신 UI 전용 데이터만 받는다.

```csharp
public readonly struct PlayerHudViewModel
{
    public PlayerHudViewModel(string level, string exp, float expRatio, string soul, string points)
    {
        Level = level;
        Exp = exp;
        ExpRatio = expRatio;
        Soul = soul;
        Points = points;
    }

    public string Level { get; }
    public string Exp { get; }
    public float ExpRatio { get; }
    public string Soul { get; }
    public string Points { get; }
}
```

```csharp
public sealed class GameHudView : MonoBehaviour
{
    [SerializeField] private Text levelText;
    [SerializeField] private Text expText;
    [SerializeField] private Image expFill;
    [SerializeField] private Text soulText;
    [SerializeField] private Text pointText;
    [SerializeField] private Button upgradeButton;

    public event Action UpgradeRequested;

    public void Render(PlayerHudViewModel model)
    {
        levelText.text = model.Level;
        expText.text = model.Exp;
        expFill.fillAmount = model.ExpRatio;
        soulText.text = model.Soul;
        pointText.text = model.Points;
    }
}
```

이 구조에서는 아트와 레이아웃을 완전히 교체해도 Adapter가 동일한 View API만 호출하면 된다. View의 버튼은 `UpgradeRequested`처럼 **사용자 의도**만 전달하며, 업그레이드 성공 여부나 포인트 차감은 판단하지 않는다.

복잡도가 아직 낮으므로 모든 View에 인터페이스, 베이스 클래스, 전역 이벤트 버스를 추가할 필요는 없다.

---

## 7. 작업 방법 선택

### 방법 A: C# 변경 없이 디자인만 교체

현재 기능과 구조를 가장 안전하게 유지하는 방법이다.

1. `Main.unity`를 백업하거나 별도 작업 브랜치를 만든다.
2. `Game UI`, `Rebirth UI` 아래의 색상, Sprite, 폰트, RectTransform, 자식 장식 오브젝트를 변경한다.
3. 기존 `GameUIController`와 `RebirthUIController` 컴포넌트는 유지한다.
4. 기존 직렬화 필드가 요구하는 `Text`, `Image`, `Button`, 패널 참조를 새 오브젝트에 다시 연결한다.
5. 버튼 기능은 Inspector의 Persistent Listener가 아니라 컨트롤러가 런타임에 연결하므로 별도 `OnClick` 등록을 추가하지 않는다.
6. 모든 비상호작용 장식 `Image`의 `Raycast Target`을 끈다.
7. 기능 검증 후 UI 루트를 프리팹으로 분리한다.

장점:

- 핵심 코드와 UI 코드 모두 그대로 유지한다.
- 기능 회귀 위험이 가장 낮다.
- 빠르게 디자인 시안을 적용할 수 있다.

한계:

- 시각 View와 게임 연결 코드의 의존성은 기존 컨트롤러 안에 남는다.
- 화면 구조를 크게 바꾸거나 재사용 가능한 컴포넌트를 만들 때 불편하다.

### 방법 B: 코어를 유지하고 UI 내부 의존성까지 분리

장기적으로 권장하는 방법이다.

1. 기존 두 컨트롤러의 공개 계약을 그대로 유지한다.
2. 컨트롤러에서 `Text`, `Image`, `Button` 필드를 별도 View 컴포넌트로 이동한다.
3. 컨트롤러는 게임 상태를 UI 전용 ViewModel로 변환한다.
4. View는 ViewModel을 그리며 클릭 이벤트만 컨트롤러에 전달한다.
5. 패널 열림 상태, 조이스틱 차단, 이벤트 구독 해제는 기존 컨트롤러 경계에서 유지한다.
6. 새 View를 프리팹화하고 씬에는 Adapter와 프리팹 인스턴스만 연결한다.

변경 범위는 `Scripts/UI`, `Prefabs/UI`, `Art/UI`, `Main.unity`로 제한한다. `Core`, `Player`, `Truck`, `Monsters`, `Upgrades`, `Rebirth`, `Blessings`, `Save`의 코드는 수정하지 않는다.

---

## 8. 실제 리디자인 순서

### 1단계: 기능 목록 고정

디자인 전에 기존 기능을 체크리스트로 고정한다.

- HUD: 레벨, EXP 숫자/게이지, 영혼, 포인트
- 업그레이드: 열기, 닫기, 속도/크기 정보, 버튼 활성 상태
- 환생: 상태, 단계 목록, 선택 상태, 확인, 축복 후보 3개, 강제 선택
- 입력: 전체 게임 영역 드래그 조이스틱
- 반응형: 카메라 뷰포트와 동일한 게임 영역

### 2단계: 디자인 토큰 정의

색과 크기를 각 오브젝트에 무작위로 직접 입력하지 않고 작은 UI Theme 에셋 또는 제한된 공통 프리팹으로 관리한다.

권장 항목:

- 배경/패널/강조/비활성/위험 색상
- 제목/본문/보조 텍스트 크기
- 패널 여백, 버튼 높이, 모서리 Sprite
- 축복 등급 `C/U/R/SR` 색상
- 버튼 Normal/Highlighted/Pressed/Disabled 상태

단, 테마 시스템을 위해 범용 UI 프레임워크나 과도한 상속 구조를 만들 필요는 없다.

### 3단계: 정적 View 제작

실제 게임 시스템 없이 View만 배치하고 아래 상태를 Inspector 또는 간단한 Editor Preview로 확인한다.

- EXP 0%, 50%, 100%
- 업그레이드 가능/불가 버튼
- 긴 축복 이름과 2~3줄 설명
- 잠김/선택 가능/선택됨 환생 단계
- 패널 열림/닫힘

### 4단계: Adapter 연결

- `PlayerState.GetState()` 결과를 HUD ViewModel로 변환한다.
- `TruckController.GetStats()` 결과를 업그레이드 ViewModel로 변환한다.
- 환생 단계와 후보 데이터를 리스트 ViewModel로 변환한다.
- 버튼 이벤트를 기존 `TryUpgradeSpeed`, `TryUpgradeSize`, `BeginRebirth`, `CompleteRebirth`에 연결한다.
- 성공을 가정해 View 값을 먼저 바꾸지 말고, 시스템 이벤트 후 전체 상태를 다시 렌더링한다.

### 5단계: 씬 연결

- UI는 반드시 `Game Canvas` 아래에 둔다.
- HUD와 팝업은 전체 화면이 아니라 컨트롤러가 `SetViewport`로 조절하는 `gameArea` 아래에 둔다.
- `GameManager`의 기존 두 UI 참조가 새 Adapter를 가리키는지 확인한다.
- UI 루트의 활성 상태 때문에 `Initialize`가 실행되지 않는 일이 없도록 Adapter 오브젝트 자체는 활성 상태로 둔다. 닫힌 패널만 비활성화한다.

### 6단계: 프리팹화

현재 UI는 씬에 직접 들어 있으므로 다음 단위로 프리팹화하는 것이 적절하다.

- `GameHud.prefab`
- `UpgradePanel.prefab`
- `RebirthPanel.prefab`

Adapter는 씬에 남기고 View 프리팹을 참조하게 하면, 디자인 교체 시 `GameManager` 연결을 다시 할 필요가 없다.

---

## 9. 반응형 및 입력 주의사항

### 카메라와 UI 뷰포트

카메라는 화면 비율과 관계없이 `10:16` 게임 영역을 유지하고 레터박스/필러박스를 만든다. `GameUIController`, `RebirthUIController`, `JoystickInput`은 모두 카메라의 `ViewportRect`에 맞춰 RectTransform 앵커를 변경한다.

따라서:

- HUD를 최상위 Canvas 화면 모서리에 직접 고정하지 않는다.
- 모든 게임 UI는 `gameArea` 내부 앵커를 기준으로 배치한다.
- Canvas 기준 해상도 `1080 x 1920`과 실제 게임 영역 `10:16`이 같지 않으므로 한 해상도만 보고 절대 좌표를 과도하게 사용하지 않는다.
- 세로형, 정사각형, 와이드 화면에서 각각 확인한다.

### 조이스틱과 Raycast

`Input Surface`는 포인터 다운/드래그/업 이벤트로 조이스틱을 제어한다.

- 클릭 가능한 UI는 Input Surface보다 정상적으로 Raycast 우선순위를 가져야 한다.
- 장식용 `Image`와 `Text`의 `Raycast Target`은 끈다.
- 팝업 배경은 필요하면 Raycast를 받아 뒤쪽 조이스틱 입력을 차단한다.
- 패널을 열 때 `JoystickInput.SetInputEnabled(false)`가 호출되어야 한다.
- 버튼 위 드래그가 조이스틱 입력으로 새지 않는지 모바일 터치로 확인한다.

### Safe Area

현재 코드에는 노치와 홈 인디케이터용 Safe Area 처리가 없다. Safe Area 지원이 필요하면 게임 코어가 아니라 UI View 계층에 `SafeAreaContainer`를 추가하는 방식으로 구현한다. 카메라 `gameArea`와 Safe Area의 교집합 안에 HUD를 배치하되, 조이스틱의 입력 가능 영역 자체를 임의로 줄이지 않도록 별도로 검증한다.

---

## 10. 기존 Editor Setup 도구 주의

다음 메뉴용 Editor 스크립트는 현재 UI를 코드로 생성한다.

- `Editor/SeventhStageSetup.cs`
- `Editor/RebirthFeatureSetup.cs`

각 Setup을 다시 실행하면 씬의 기존 `Game UI` 또는 `Rebirth UI`를 삭제하고 기본 UI를 재생성한다. 커스텀 디자인을 적용한 뒤에는 이 Setup 메뉴를 실행하지 않는다.

향후에도 Setup 도구가 필요하다면 다음 중 하나를 별도 작업으로 선택한다.

- 기본 UI 생성 코드를 새 View 프리팹 인스턴스 생성 방식으로 수정한다.
- Setup은 폐기하지 않고 검증 전용 메뉴만 유지한다.
- 커스텀 UI 루트 이름을 바꿔 삭제를 피하는 임시 우회는 씬에 중복 UI를 만들 수 있으므로 권장하지 않는다.

기존 `Verify()` 역시 현재 컨트롤러의 직렬화 필드 이름을 직접 검사한다. 방법 A에서는 그대로 사용할 수 있지만, 방법 B로 View를 분리하면 새 구조에 맞춘 UI 검증 코드가 필요하다.

---

## 11. 검증 체크리스트

### 컴파일 및 씬 연결

- [ ] Unity Console에 컴파일 오류가 없다.
- [ ] `Main.unity`의 `GameManager`에 두 UI Adapter가 연결되어 있다.
- [ ] 모든 View 직렬화 참조가 연결되어 있다.
- [ ] 플레이 시작 시 저장된 플레이어 상태가 HUD에 표시된다.
- [ ] 이벤트가 중복 구독되어 버튼 한 번에 명령이 여러 번 실행되지 않는다.
- [ ] 파괴 시 모든 이벤트와 버튼 리스너가 해제된다.

### HUD와 업그레이드

- [ ] 몬스터 처치 후 EXP, 영혼, 레벨, 포인트가 즉시 갱신된다.
- [ ] EXP 게이지가 `0~1` 범위를 벗어나지 않는다.
- [ ] 포인트가 없으면 두 업그레이드 버튼이 비활성화된다.
- [ ] 속도 업그레이드 후 속도 레벨과 최대 속도가 갱신된다.
- [ ] 크기 업그레이드 후 크기 레벨과 실제 크기가 갱신된다.
- [ ] 창을 열면 게임 진행과 조이스틱 입력이 중지된다.
- [ ] 창을 닫으면 입력과 게임 진행이 복원된다.

### 환생

- [ ] 플레이어 레벨에 따라 단계 잠금/선택 상태가 정확하다.
- [ ] 최대 단계와 낮은 단계의 안내 문구가 유지된다.
- [ ] 환생 시작 후 후보가 3개 표시된다.
- [ ] 후보 선택 전에는 환생 창을 닫을 수 없다.
- [ ] 후보 선택 후 창이 닫히고 입력이 복원된다.
- [ ] 환생 후 레벨, 업그레이드, 배율, 축복 수가 즉시 갱신된다.

### 화면과 입력

- [ ] 10:16 게임 영역 안에서 HUD가 잘리지 않는다.
- [ ] 1080x1920, 정사각형, 가로형 화면에서 앵커가 정상이다.
- [ ] 버튼 클릭이 조이스틱 드래그로 전달되지 않는다.
- [ ] 장식 이미지가 불필요하게 입력을 막지 않는다.
- [ ] 긴 한글 텍스트와 축복 설명이 겹치거나 잘리지 않는다.
- [ ] 모바일 Safe Area가 필요하다면 노치/홈 영역을 침범하지 않는다.

### 기존 플레이 동작

- [ ] 트럭 이동, 가속, 관성, 회전 감각이 바뀌지 않았다.
- [ ] 몬스터 배회/도망/충돌 동작이 바뀌지 않았다.
- [ ] 카메라 추적과 트럭 크기 기반 줌이 유지된다.
- [ ] 몬스터 스폰 수치와 보상 밸런스가 바뀌지 않았다.
- [ ] 저장/로드 데이터가 유지된다.

---

## 12. 변경 금지 경계

UI 리디자인 작업에서는 원칙적으로 아래 파일을 수정하지 않는다.

```text
Scripts/Core/GameManager.cs
Scripts/Player/PlayerState.cs
Scripts/Truck/TruckController.cs
Scripts/Upgrades/TruckUpgradeSystem.cs
Scripts/Rebirth/RebirthSystem.cs
Scripts/Blessings/BlessingSystem.cs
Scripts/Save/PlayerProgressSaveSystem.cs
Scripts/Monsters/*
Scripts/Spawn/*
Scripts/World/*
Scripts/Camera/*
Config/GameConfig.asset
```

허용 범위는 다음과 같이 잡는 것이 안전하다.

```text
Scripts/UI/*
Art/UI/*
Prefabs/UI/*
Scenes/Main.unity의 UI 오브젝트와 UI 직렬화 참조
필요한 경우 Editor의 UI 검증 코드
```

`JoystickInput`은 UI처럼 보이지만 트럭 이동 입력의 일부다. 디자인을 위해 `Joystick`의 Sprite와 RectTransform을 바꾸는 것은 가능하지만, 축 계산, 방향 반전, 최대 거리, 포인터 처리 코드는 수정하지 않는다.

---

## 13. 권장 최종 작업 범위

첫 번째 리디자인에서는 아래 정도가 적절하다.

1. uGUI를 그대로 사용한다.
2. 게임 코어 코드는 수정하지 않는다.
3. 두 기존 UI 컨트롤러의 외부 계약을 유지한다.
4. HUD, 업그레이드, 환생 View를 각각 프리팹으로 분리한다.
5. 디자인 에셋을 `Art/UI`에 모은다.
6. 방법 A로 먼저 디자인을 적용하고 기능을 검증한다.
7. 화면 구조를 반복 변경할 필요가 확인되면 방법 B로 View와 Adapter를 분리한다.

이 순서라면 한 번에 디자인과 구조를 모두 크게 바꾸지 않으면서, 현재 게임 동작을 보존하고 이후 UI 교체 비용도 낮출 수 있다.


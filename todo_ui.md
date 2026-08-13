# IsekaiTruck UI 1차 구현 가이드라인 및 로드맵

## 1. 문서 목적

Unity 기반 IsekaiTruck MVP에 시작 화면, 게임 HUD, 경험치/레벨, 주행 시간, 연료 아이템, 영혼 표시, 속도 게이지와 하단 메뉴를 추가한다.

이번 작업에서는 기존 3D 게임 플레이를 유지하면서 **UI와 UI 표시를 위해 필요한 최소한의 게임 상태만 확장**한다. 실제 아트워크와 하단 메뉴의 세부 페이지는 구현하지 않는다.

---

## 2. 프로젝트 조사 결과

### Unity 환경

- Unity: `6000.3.8f1`
- Render Pipeline: URP
- UI: `com.unity.ugui 2.0.0` 설치됨
- Input: `com.unity.inputsystem 1.18.0` 설치됨
- 게임 씬: `Assets/Scenes/Main.unity`
- 현재 별도의 Canvas, HUD, GameManager, GameStateManager는 없음

따라서 별도 패키지를 설치하지 않고 **Unity uGUI 기반 단일 Scene 구조**를 사용한다.

### 현재 Scene 구조

```text
Main
├── Main Camera
├── Directional Light
├── Global Volume
├── Environment
│   └── Ground
├── Gameplay
│   ├── Truck
│   └── Monsters
└── Systems
    ├── PlayerProgress
    └── SpawnManager
```

### 재사용할 기존 파일

| 파일 | 현재 책임 | UI 작업에서 재사용/확장할 부분 |
|---|---|---|
| `Player/TruckController.cs` | WASD 입력, Rigidbody 이동과 회전 | 기존 이동 유지, 읽기 전용 현재 속도/최대 속도 공개 |
| `Player/CameraFollow.cs` | 고정 쿼터뷰, Truck 중심 추적 | 변경하지 않음 |
| `Player/PlayerProgress.cs` | 처치 수, EXP, Soul 런타임 저장 | 레벨, 필요 EXP, 이월 레벨업, 상태 변경 이벤트 추가 |
| `Monsters/MonsterController.cs` | 배회, 도주, Trigger 처치 | 기존 처치 흐름 유지, Inspector 보상값을 PlayerProgress로 전달 |
| `Spawning/SpawnManager.cs` | 15마리 생성/유지와 원거리 제거 | 시작 전 비활성화, 게임 시작 시 활성화 |
| `Prefabs/Monster.prefab` | 단일 Monster 타입과 보상 설정 | 개발용 EXP/Soul 보상값을 Inspector에서 설정 |
| `Editor/CreateMainScene.cs` | Main Scene과 Prefab 자동 생성 | Canvas, UI Hierarchy, GameState와 참조를 반복 생성 가능하게 확장 |

### 현재 상태에서 부족한 부분

- 게임이 Play Mode 진입 즉시 시작됨
- PlayerProgress에 레벨과 필요 경험치 공식이 없음
- TruckController가 현재 속도를 외부에 제공하지 않음
- 주행 시간과 연료 아이템 시스템이 없음
- HUD에 전달할 상태 변경 이벤트가 없음
- Korean UI Font Asset이 없음
- 속도 게이지용 이미지/스프라이트가 없음

---

## 3. 핵심 설계 결정

### 3-1. 단일 Scene과 Panel 전환

현재 게임이 `Main.unity` 하나로 구성되어 있으므로 StartScene/GameScene을 새로 나누지 않는다.

```text
Main.unity
├── 기존 3D 게임
└── Canvas
    ├── StartPanel
    └── HUDPanel
```

게임 실행 시 `StartPanel`만 표시하고, `게임 시작하기` 버튼을 누르면 StartPanel을 숨기고 HUDPanel을 표시한다.

### 3-2. 게임 상태는 GameStateManager가 관리

```text
Start
├── TruckController 비활성
├── SpawnManager 비활성
├── DrivingTimeManager 카운트 정지
├── StartPanel 표시
└── HUDPanel 숨김

Playing
├── TruckController 활성
├── SpawnManager 활성
├── DrivingTimeManager 카운트 시작
├── StartPanel 숨김
└── HUDPanel 표시
```

`Time.timeScale = 0`에 전적으로 의존하지 않는다. 각 시스템을 GameStateManager가 명시적으로 활성화하여 이후 Pause와 Result 상태를 추가하기 쉽게 만든다.

### 3-3. UI는 상태를 표시하고 명령만 전달

- PlayerProgress가 레벨, EXP, Soul을 소유한다.
- DrivingTimeManager가 남은 주행 시간을 소유한다.
- TruckController/Rigidbody가 실제 속도를 제공한다.
- HUDController는 위 상태를 읽어 UI에 표시한다.
- UI 스크립트는 EXP, Soul, 시간을 자체적으로 계산하거나 저장하지 않는다.

### 3-4. 기존 게임 로직 보존

- Truck의 WASD 입력 및 물리 이동 공식은 변경하지 않는다.
- CameraFollow의 위치와 회전 로직은 변경하지 않는다.
- Monster AI와 SpawnManager의 거리/개체 수 설정은 변경하지 않는다.
- Monster 처치 Trigger 흐름에 레벨 UI 갱신용 이벤트만 연결한다.

### 3-5. 반응형 UI 기준

- Canvas Render Mode: Screen Space - Overlay
- Canvas Scaler: Scale With Screen Size
- Reference Resolution: `1920 × 1080`
- Screen Match Mode: Match Width Or Height
- Match: `0.5`
- Left/Right/Top/Bottom 요소는 해당 화면 가장자리에 Anchor
- 화면 중앙 3D 영역은 별도 RenderTexture 없이 기존 Camera 화면을 그대로 사용
- Side Panel은 반투명 배경을 사용해 게임 화면을 완전히 가리지 않음

---

## 4. 목표 UI Hierarchy

```text
Main
├── Main Camera
├── Environment
├── Gameplay
├── Systems
│   ├── GameStateManager
│   ├── PlayerProgress
│   ├── DrivingTimeManager
│   └── SpawnManager
├── Canvas
│   ├── StartPanel
│   │   ├── Background
│   │   ├── TitleText
│   │   └── StartButton
│   │       └── Label
│   └── HUDPanel
│       ├── LeftPanel
│       │   ├── LevelLabel
│       │   └── DrivingTimeGroup
│       │       ├── DrivingTimeTitle
│       │       └── DrivingTimeValue
│       ├── ExperienceBar
│       │   ├── Background
│       │   ├── Fill
│       │   └── ExperienceLabel
│       ├── RightPanel
│       │   ├── GoddessPlaceholder
│       │   ├── SoulTitle
│       │   ├── SoulValue
│       │   └── SpeedGauge
│       │       ├── ArcBackground
│       │       ├── ArcFill
│       │       ├── Needle
│       │       ├── SpeedValue
│       │       └── SpeedUnit
│       └── BottomMenu
│           ├── RebirthButton
│           ├── TruckUpgradeButton
│           ├── DriveButton
│           ├── CollectionButton
│           └── SettingsButton
└── EventSystem
```

`EventSystem`에는 현재 Input System과 호환되는 `InputSystemUIInputModule`을 사용한다.

---

## 5. 추가할 파일

| 예정 파일 | 역할 |
|---|---|
| `Scripts/Core/GameStateManager.cs` | Start/Playing 상태와 시스템/UI 활성화 제어 |
| `Scripts/Gameplay/DrivingTimeManager.cs` | 시작 시간, 시간 감소, 연료 시간 추가, 0초 이벤트 제공 |
| `Scripts/Items/FuelPickup.cs` | Truck Trigger 접촉 시 시간을 추가하고 자신을 제거 |
| `Scripts/UI/HUDController.cs` | PlayerProgress, DrivingTimeManager, TruckController와 각 UI 연결 |
| `Scripts/UI/ExperienceBarUI.cs` | EXP Fill, 레벨 및 EXP 문구 표시 |
| `Scripts/UI/DrivingTimeUI.cs` | 남은 시간을 `MM:SS` 형식으로 표시 |
| `Scripts/UI/SoulUI.cs` | 현재 Soul 표시 |
| `Scripts/UI/SpeedGaugeUI.cs` | 실제 Truck 속도를 숫자, Arc와 Needle에 반영 |
| `Scripts/UI/BottomMenuUI.cs` | 5개 버튼 클릭을 받고 현재는 Debug.Log만 출력 |
| `Scripts/UI/ArcGraphic.cs` | 외부 이미지 없이 약 210도 원호를 그리는 uGUI Graphic |
| `Prefabs/FuelPickup.prefab` | 기본 Primitive 기반 개발용 연료 아이템 |

구현 과정에서 단순 표시 컴포넌트를 `HUDController` 하나로 충분히 관리할 수 있다면 UI 파일 수를 줄인다. 역할 분리는 유지하되 의미 없는 1~2줄짜리 래퍼 스크립트는 만들지 않는다.

---

## 6. 수정할 파일

| 기존 파일 | 예정 변경 |
|---|---|
| `Player/PlayerProgress.cs` | Level, CurrentExp, RequiredExp, AddExp, AddSoul, 다중 레벨업, 변경 이벤트 추가 |
| `Player/TruckController.cs` | 기존 이동은 유지하고 CurrentSpeed/MaxSpeed 읽기 전용 프로퍼티 추가 |
| `Monsters/MonsterController.cs` | 구조는 유지하고 개발용 보상값 및 PlayerProgress 연동 검증 |
| `Editor/CreateMainScene.cs` | Systems, Canvas, EventSystem, FuelPickup과 모든 Inspector 참조 자동 생성 |
| `Scenes/Main.unity` | 생성 스크립트를 통해 UI와 게임 상태 구조 반영 |
| `Prefabs/Monster.prefab` | 개발 테스트용 EXP/Soul 값 설정 |
| `todo.md` | 전체 MVP 진행 상태만 요약 갱신 |
| `todo_ui.md` | UI 세부 진행 상태와 결정 사항 기록 |

`CameraFollow.cs`는 이번 작업에서 수정하지 않는다.

---

## 7. 데이터 및 이벤트 흐름

### Monster 처치

```text
MonsterController.OnTriggerEnter
→ PlayerProgress.RegisterMonsterDefeat(expReward, soulReward)
→ AddExperience(expReward)
→ 필요하면 LevelUp 반복
→ AddSoul(soulReward)
→ PlayerProgress 상태 변경 이벤트
→ HUDController가 Level/EXP/Soul UI 갱신
→ Monster 제거
→ SpawnManager가 15마리로 보충
```

### 연료 획득

```text
FuelPickup.OnTriggerEnter
→ DrivingTimeManager.AddTime(fuelTimeBonus)
→ 남은 시간 변경 이벤트
→ DrivingTimeUI 갱신
→ FuelPickup 제거
```

### 속도 표시

```text
Truck Rigidbody 실제 평면 속도
→ TruckController.CurrentSpeed
→ SpeedGaugeUI
→ 숫자 + Needle + Arc Fill 갱신
```

속도 UI는 실제 물리 속도와 표시 단위를 분리한다. 내부 단위는 Unity units/s를 사용하고, 화면 표시용 `speedDisplayMultiplier`를 Inspector에서 조정할 수 있도록 한다. `km/h` 표시는 기본적으로 units/s × 3.6을 사용한다.

---

## 8. 단계별 구현 로드맵

## Phase UI-0 — 사전 결정과 안전장치

- [x] Korean 글꼴 처리 방식 확정: `Assets/Fonts/WarsOfPrasia.ttf`
- [x] 개발용 시작 주행 시간 확정: 90초
- [x] 개발용 Monster EXP/Soul 보상값 확정: EXP 10, Soul 1
- [x] FuelPickup 테스트 배치 개수 확정: 3개
- [x] 현재 Main Scene과 핵심 Prefab 참조 상태 확인
- [x] 기존 Unity 배치 컴파일 통과 확인

권장 개발용 임시값:

```text
시작 주행 시간: 90초
연료 보너스: 10초
Monster EXP: 10
Monster Soul: 1
첫 필요 EXP: 100
레벨당 필요 EXP 증가: 25
```

이 값들은 최종 밸런스가 아니며 모두 Inspector에서 변경 가능하게 만든다.

## Phase UI-1 — 게임 상태와 시작 화면

- [x] `GameStateManager.cs` 생성
- [x] Start/Playing 상태 정의
- [x] Canvas와 Canvas Scaler 생성
- [x] EventSystem과 InputSystemUIInputModule 생성
- [x] 전체 화면 `StartPanel` 생성
- [x] 게임 제목 Placeholder 생성
- [x] `게임 시작하기` 버튼 생성
- [x] 게임 시작 전 TruckController 비활성화
- [x] 게임 시작 전 SpawnManager 비활성화
- [x] 게임 시작 전 주행 시간 감소 정지
- [ ] 시작 전 Monster가 생성되거나 움직이지 않는지 Play Mode 확인
- [x] 버튼 클릭 시 StartPanel 숨김
- [x] 버튼 클릭 시 HUDPanel 표시
- [x] 버튼 클릭 시 Truck/Spawn/Timer 활성화
- [x] 버튼 중복 클릭 방지

완료 조건:

- Play Mode 진입 직후 Truck과 Monster 시스템이 작동하지 않는다.
- `게임 시작하기`를 누른 뒤 기존 게임이 정상적으로 시작된다.
- WASD 조작과 CameraFollow가 기존과 동일하게 동작한다.

## Phase UI-2 — PlayerProgress와 경험치/레벨

- [x] 초기 Level을 1로 설정
- [x] CurrentExperience, RequiredExperience 공개 읽기 프로퍼티 추가
- [x] `AddExperience(int)` 구현
- [x] `RequiredExp = baseRequiredExp + (Level - 1) × requiredExpIncrease` 구현
- [x] 초과 EXP 이월 구현
- [x] 한 번에 여러 레벨이 오르는 경우 반복 처리
- [x] PlayerProgress 상태 변경 이벤트 추가
- [x] Monster 처치 EXP 연결
- [x] Monster 처치 Soul 연결
- [x] EXP/Soul 음수 방지
- [ ] 레벨과 EXP 상태를 Console/Inspector에서 우선 검증

완료 조건:

- EXP 95 상태에서 EXP 10을 받으면 Level 2, EXP 5가 된다.
- 큰 EXP를 한 번에 받아도 필요한 만큼 여러 레벨이 오른다.
- HUD가 PlayerProgress 상태를 소유하지 않는다.

## Phase UI-3 — 주행 시간과 연료 아이템

- [x] `DrivingTimeManager.cs` 생성
- [x] 시작 주행 시간을 Inspector 설정으로 제공
- [x] Playing 상태에서만 `deltaTime`만큼 감소
- [x] 남은 시간이 0 아래로 내려가지 않도록 Clamp
- [x] 시간이 0이 되었음을 알리는 이벤트/API 제공
- [x] 이번 단계에서는 0초 이후 복잡한 Game Over를 실행하지 않음
- [x] `FuelPickup.cs` 생성
- [x] FuelPickup을 Truck 비간섭 Trigger로 구성
- [x] `fuelTimeBonus`를 Inspector에서 변경 가능하게 구성
- [x] FuelPickup 획득 시 시간 추가 및 한 번만 제거
- [x] 기본 Primitive와 Material로 FuelPickup Prefab 생성
- [x] Main Scene에 개발 확인용 FuelPickup 3개 배치

완료 조건:

- 시작 전에는 시간이 감소하지 않는다.
- 게임 시작 후 시간이 초 단위로 감소한다.
- FuelPickup 획득 시 설정한 시간만큼 증가한다.
- Truck 움직임이 Pickup Trigger 때문에 방해받지 않는다.

## Phase UI-4 — HUD 기본 레이아웃

- [x] HUDPanel을 Start 상태에서 숨기고 Playing 상태에서 표시
- [x] LeftPanel 생성 및 왼쪽 Anchor 적용
- [x] Center Top 경험치 바 생성 및 상단 중앙 Anchor 적용
- [x] RightPanel 생성 및 오른쪽 Anchor 적용
- [x] BottomMenu 생성 및 하단 Stretch Anchor 적용
- [x] Side Panel 배경을 반투명 처리
- [ ] 중앙 3D 게임 화면의 핵심 시야가 가려지지 않는지 Play Mode 확인
- [ ] `1920×1080`, `1366×768`, `1280×720`에서 배치 확인
- [ ] WebGL Canvas 크기 변경 시 Anchor 동작 확인

완료 조건:

- 모든 HUD 영역이 화면 크기에 따라 가장자리를 기준으로 유지된다.
- Truck, Monster와 Camera 동작이 UI 추가 전과 동일하다.

## Phase UI-5 — Left Panel과 경험치 바

- [x] `LV. 1` 형식의 레벨 표시
- [x] 주행 가능 시간 제목 표시
- [x] 남은 시간을 `MM:SS` 형식으로 표시
- [x] 경험치 바 Background와 Fill 구성
- [x] EXP 비율을 `CurrentExp / RequiredExp`로 반영
- [ ] 레벨업 직후 Fill이 이월 EXP 비율로 돌아가는지 Play Mode 확인
- [x] PlayerProgress 이벤트 구독/해제 수명주기 처리
- [x] DrivingTimeManager 이벤트 구독/해제 수명주기 처리

완료 조건:

- 레벨, EXP Fill과 시간이 실제 상태와 일치한다.
- 매 프레임 불필요한 문자열/오브젝트 할당을 최소화한다.

## Phase UI-6 — Right Panel

- [x] 외부 이미지 없이 Goddess Placeholder 생성
- [x] Goddess Sprite를 나중에 교체할 수 있도록 UI Image 슬롯 유지
- [x] Soul 제목과 현재 값 표시
- [x] 약 210도 Arc를 그리는 `ArcGraphic` 구현
- [x] 속도 Needle 구성
- [x] 현재 속도 숫자와 `km/h` 단위 표시
- [x] Rigidbody의 XZ 평면 속도를 사용
- [x] 표시 최대 속도와 multiplier를 Inspector 설정으로 제공
- [ ] 정지/가속/감속 시 Arc, Needle, 숫자가 일치하는지 Play Mode 확인
- [x] 최대값을 넘어도 게이지가 범위를 벗어나지 않도록 Clamp

완료 조건:

- Goddess Placeholder, Soul, 속도 게이지가 오른쪽 패널에 표시된다.
- 속도 게이지가 Truck의 실제 이동과 함께 변한다.
- 속도 UI가 Truck 이동값을 수정하지 않는다.

## Phase UI-7 — 하단 메뉴

- [x] 균등 배치된 버튼 5개 생성
- [x] `환생` 버튼 생성
- [x] `트럭 업글` 버튼 생성
- [x] `운전` 버튼 생성
- [x] `도감` 버튼 생성
- [x] `환경설정` 버튼 생성
- [x] `BottomMenuUI.cs`에서 클릭 이벤트 연결
- [x] 각 클릭 시 구분 가능한 Debug.Log 출력
- [x] 세부 페이지/Popup은 만들지 않음

완료 조건:

- 모든 버튼이 클릭되고 각기 다른 로그를 한 번씩 남긴다.
- 버튼이 Truck의 WASD 입력을 변경하지 않는다.

## Phase UI-8 — 통합 검증과 문서화

- [x] Unity 배치 모드 C# 컴파일 통과
- [x] Main Scene의 주요 직렬화 참조 확인
- [x] 정적 검사에서 Missing Script, Missing Reference 없음
- [ ] Start → Playing 전환 확인
- [ ] 기존 Truck 이동과 CameraFollow 회귀 확인
- [ ] 15마리 Spawn/AI/처치 회귀 확인
- [ ] 처치 → EXP/Soul → HUD 갱신 확인
- [ ] EXP 이월 및 레벨업 확인
- [ ] Timer 감소 및 FuelPickup 증가 확인
- [ ] 실제 속도 게이지 반영 확인
- [ ] 하단 메뉴 클릭 로그 확인
- [ ] Play Mode 재진입 시 이벤트 중복 구독 여부 확인
- [ ] Console Error와 반복 Warning 없음
- [ ] `todo.md`와 `todo_ui.md` 체크 상태 갱신

---

## 9. Inspector 연결 계획

Scene은 `CreateMainScene.cs`가 아래 참조를 자동 연결하도록 한다. 사용자가 수동으로 Drag & Drop하지 않아도 실행 가능한 상태를 목표로 한다.

### GameStateManager

- TruckController
- SpawnManager
- DrivingTimeManager
- StartPanel
- HUDPanel
- StartButton

### HUDController

- PlayerProgress
- DrivingTimeManager
- TruckController
- Level Text
- Experience Fill/Label
- Driving Time Text
- Soul Text
- SpeedGaugeUI

### SpawnManager / Monster

- 기존 Truck, Monsters Parent, PlayerProgress 참조 유지
- Monster Prefab의 EXP/Soul Reward만 Inspector 설정

### FuelPickup

- DrivingTimeManager 참조
- Truck Transform 또는 Rigidbody 참조
- Fuel Time Bonus

---

## 10. UI 디자인 가이드라인

- 기능과 정보 계층을 우선하고 장식은 최소화한다.
- 기본 색상은 반투명 어두운 Panel + 밝은 Text + 구분되는 Accent Color를 사용한다.
- 중앙 3D 영역을 최대한 넓게 유지한다.
- Left/Right Panel 너비는 Reference Resolution 기준 약 220~260px에서 시작한다.
- Bottom Menu 높이는 약 80~100px에서 시작한다.
- Experience Bar는 좌우 Panel 사이의 상단 중앙에 배치한다.
- 버튼은 최소 클릭 영역을 확보하고 글자가 잘리지 않게 한다.
- Goddess는 단순 원/몸통 조합 또는 기본 UI Sprite Placeholder로 만든다.
- 실제 Sprite가 준비되면 GoddessPlaceholder의 Image만 교체할 수 있게 한다.
- 속도 Arc는 Shader나 외부 패키지 대신 uGUI 정점 또는 기본 Image 조합으로 만든다.
- 모든 주요 색상, 최대 속도, 표시 multiplier와 타이머 수치는 Inspector에서 조정 가능하게 한다.

---

## 11. 미정 사항 및 구현 전 확인

### Korean 글꼴

프로젝트의 `Assets/Fonts/WarsOfPrasia.ttf`를 공통 uGUI Font로 사용한다. Unity Import와 한국어 글리프 포함 여부를 확인했으며 Font Data도 빌드에 포함되도록 설정되어 있다.

파일명은 대소문자를 구분하여 `WarsOfPrasia.ttf`로 참조한다. 이번 1차 구현은 TextMeshPro Font Asset을 별도로 생성하지 않고 uGUI `Text`가 TTF를 직접 사용한다.

### 개발용 밸런스 값

아래 개발용 임시값을 확정해 적용했다.

- 시작 주행 시간: 90초
- FuelPickup 증가 시간: 10초
- Monster EXP: 10
- Monster Soul: 1
- 첫 필요 EXP: 100
- 레벨당 필요 EXP 증가: 25
- 속도 게이지 표시: 0~30km/h, Unity units/s × 3.6

최종 수치는 MVP Play Mode 확인 후 Inspector에서 조정한다.

### 0초 처리

이번 단계에서는 남은 시간을 0으로 고정하고 `TimeExpired` 이벤트만 발생시킨다. Result Panel, 게임 정지와 재시작은 후속 작업으로 남긴다.

---

## 12. 이번 작업에서 구현하지 않는 것

- 모바일 조이스틱
- 실제 Goddess 일러스트
- 외부 UI/이미지/사운드 Asset 다운로드
- 하단 메뉴의 세부 화면과 Popup
- 환생 및 트럭 업그레이드 실제 규칙
- 처치/레벨업 애니메이션과 사운드
- 복잡한 Game Over 및 Result Panel
- Pause 메뉴
- 영구 저장/불러오기
- 다중 Monster 데이터와 최종 밸런싱
- 기존 Truck 이동 또는 Camera 로직 재작성

---

## 13. 완료 조건

- [ ] 게임 시작 화면이 존재한다.
- [ ] `게임 시작하기` 버튼으로 게임을 시작할 수 있다.
- [ ] 시작 전 Truck, Spawn, Timer가 작동하지 않는다.
- [ ] 기존 WASD Truck 조작이 유지된다.
- [ ] 기존 Truck/Monster가 중앙 게임 화면에서 정상 동작한다.
- [ ] 왼쪽에 현재 Level이 표시된다.
- [ ] 왼쪽에 남은 주행 시간이 표시된다.
- [ ] 주행 시간이 게임 중 감소한다.
- [ ] FuelPickup을 획득하면 주행 시간이 증가한다.
- [ ] 중앙 상단에 경험치 바가 표시된다.
- [ ] Monster 처치 시 경험치가 증가한다.
- [ ] 경험치가 가득 차면 초과 EXP를 이월하고 Level이 증가한다.
- [ ] 오른쪽 상단에 Goddess Placeholder가 표시된다.
- [ ] 오른쪽에 Soul 개수가 표시된다.
- [ ] 오른쪽에 약 210도 형태의 속도 게이지가 표시된다.
- [ ] 속도 게이지가 실제 Truck 속도를 반영한다.
- [ ] 하단에 환생/트럭 업글/운전/도감/환경설정 버튼이 존재한다.
- [ ] 하단 버튼 클릭 시 각기 다른 Debug.Log가 출력된다.
- [ ] 하단 버튼의 세부 페이지는 구현되어 있지 않다.
- [ ] 해상도 변경 시 UI Anchor와 Scale이 정상 작동한다.
- [ ] 기존 CameraFollow와 15마리 Monster 시스템이 깨지지 않는다.
- [ ] Missing Reference와 Console Error가 없다.

---

## 14. 실제 진행 순서 요약

1. Korean 글꼴과 개발용 임시 수치 결정
2. GameStateManager와 시작 화면 구현
3. PlayerProgress의 EXP/Level/Soul 확장
4. DrivingTimeManager와 FuelPickup 구현
5. 반응형 HUD Layout 구성
6. Level/EXP/Time/Soul UI 연결
7. 210도 속도 게이지와 실제 속도 연결
8. 하단 메뉴 버튼과 Debug.Log 연결
9. Unity 컴파일 및 Scene 참조 자동 검증
10. Play Mode 통합 테스트 후 TODO 갱신

---

## 15. UI 레이아웃 리팩터링 — 3열 와이어프레임

기존 HUD 전체 폭 기준 배치를 18% / 64% / 18%의 명시적인 세로 패널 구조로 변경한다. 게임 상태와 데이터 계산은 수정하지 않고 기존 HUD 바인딩을 재사용한다.

### 구현된 Hierarchy

```text
Canvas
├── StartPanel
└── HUDPanel
    ├── LeftPanel                 # 화면 너비 18%
    │   ├── LogoArea
    │   │   └── LogoPlaceholder
    │   ├── LevelWidget
    │   │   ├── Title
    │   │   ├── RingBackground
    │   │   ├── RingFill
    │   │   └── LevelLabel
    │   └── DrivingTimeWidget
    │       ├── DrivingTimeTitle
    │       ├── RingBackground
    │       ├── RingFill
    │       └── DrivingTimeValue
    ├── CenterPanel               # 화면 너비 64%
    │   ├── ExpArea
    │   │   └── ExperienceBar
    │   ├── GameViewArea
    │   └── BottomMenu
    │       ├── RebirthButton
    │       ├── TruckUpgradeButton
    │       ├── DriveButton
    │       ├── CollectionButton
    │       └── SettingsButton
    └── RightPanel                # 화면 너비 18%
        ├── GoddessArea
        │   └── PlaceholderLabel
        ├── SoulWidget
        │   ├── SoulTitle
        │   ├── RingBackground
        │   ├── RingFill
        │   └── SoulValue
        └── SpeedGauge
            ├── ArcBackground
            ├── ArcFill
            ├── Needle
            ├── SpeedValue
            └── SpeedUnit
```

### 구현 체크리스트

- [x] LeftPanel 18%, CenterPanel 64%, RightPanel 18% Anchor 적용
- [x] 기존 어두운 남색/회색/청록색 스타일 유지
- [x] LeftPanel 상단 Logo Placeholder 추가
- [x] Level을 360도 원형 Ring HUD로 변경
- [x] Driving Time을 360도 원형 Ring HUD로 변경
- [x] Driving Time Ring을 기존 Timer 상태에 연결
- [x] EXP Bar를 CenterPanel/ExpArea 아래로 이동
- [x] GameViewArea를 CenterPanel의 주요 영역으로 명시
- [x] BottomMenu를 CenterPanel 하단으로 이동
- [x] 하단 버튼을 `92×86` 크기로 중앙 정렬
- [x] 하단 버튼마다 교체 가능한 Icon Image 슬롯 추가
- [x] Goddess Placeholder 영역을 RightPanel 상단 약 37%로 확대
- [x] Soul을 360도 원형 Ring HUD로 변경
- [x] SpeedGauge를 RightPanel 하단 약 30% 영역으로 확대
- [x] SpeedGauge의 기존 210도 Arc/Needle/실제 속도 연결 유지
- [x] 기존 BottomMenu Debug.Log 이벤트 연결 유지
- [x] 기존 HUDController의 Level/EXP/Time/Soul/Speed 바인딩 유지
- [x] Unity 배치 모드 C# 컴파일 통과
- [x] 주요 UI 직렬화 참조 null 여부 확인
- [ ] Play Mode에서 세 패널의 실제 시각적 비율 확인
- [ ] Play Mode에서 원형 Level/Time/Soul HUD 표시 확인
- [ ] Play Mode에서 Driving Time Ring 감소 확인
- [ ] Play Mode에서 EXP Bar가 CenterPanel 밖으로 침범하지 않는지 확인
- [ ] Play Mode에서 BottomMenu가 CenterPanel 밖으로 침범하지 않는지 확인
- [ ] Play Mode에서 5개 버튼 클릭 로그 회귀 확인
- [ ] Play Mode에서 SpeedGauge 크기와 Needle 동작 확인
- [ ] Play Mode에서 WASD/Monster/Fuel/EXP/Level/Soul 회귀 확인
- [ ] 1920×1080, 1366×768, 1280×720 해상도 배치 확인

---

## 16. 주행 종료와 Result 화면

남은 주행 시간이 0이 되면 기존 `DrivingTimeManager.TimeExpired` 이벤트를 사용해 Result 상태로 전환한다.

### 구현 흐름

```text
DrivingTimeManager.TimeExpired
→ GameStateManager: Playing → Result
→ Truck 입력과 Rigidbody 속도 정지
→ SpawnManager 정지
→ 현재 MonsterController 정지
→ ResultPanel 표시
→ Level / 처치 수 / Soul 표시
→ 다시 시작 클릭
→ Main.unity 재로드
→ 모든 런타임 값 초기화 및 Start 화면 복귀
```

### 구현 체크리스트

- [x] `GameStateManager.GameState.Result` 추가
- [x] 기존 `TimeExpired` 이벤트 구독/해제
- [x] 중복 Result 전환 방지
- [x] Result 진입 시 TruckController 비활성화
- [x] Result 진입 시 Truck Rigidbody 속도와 회전 속도 제거
- [x] Result 진입 시 SpawnManager 비활성화
- [x] Result 진입 시 현재 MonsterController 비활성화
- [x] 전체 화면 반투명 `ResultPanel` 생성
- [x] 도달 Level 표시
- [x] 처치한 Monster 수 표시
- [x] 획득 Soul 표시
- [x] `다시 시작` 버튼 생성
- [x] 다시 시작 시 현재 Main Scene 재로드
- [x] 결과 UI와 GameStateManager 직렬화 참조 확인
- [x] Unity 배치 모드 C# 컴파일 통과
- [ ] Play Mode에서 0초에 Result 화면이 한 번만 표시되는지 확인
- [ ] Result 상태에서 Truck과 Monster가 완전히 정지하는지 확인
- [ ] Result 통계가 PlayerProgress와 일치하는지 확인
- [ ] 다시 시작 후 Level/EXP/Soul/처치 수/Timer가 초기화되는지 확인
- [ ] 다시 시작 후 Start 화면에서 정상적으로 다시 게임을 시작할 수 있는지 확인

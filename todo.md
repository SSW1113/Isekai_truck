# IsekaiTruck Unity 전환 TODO 및 로드맵

## 목표

Three.js 프로토타입의 핵심 재미를 Unity URP 프로젝트로 단계적으로 이전한다.

첫 번째 목표는 좋은 그래픽이 아니라 다음 게임 루프가 회색 기본 도형만으로 동작하는 것이다.

> 트럭 이동 → 몬스터 생성 → 몬스터 추격/충돌 → 보상 획득 → 성장 → 다시 플레이

Unity 프로젝트 위치: `unity/IsekaiTruck/`

## 현재 상태

- [x] Unity 프로젝트 생성
- [x] 프로젝트 이름과 위치 설정: `unity/IsekaiTruck/`
- [x] Universal 3D 템플릿 및 URP 설정
- [x] Unity 6 `6000.3.8f1`에서 프로젝트 열기 및 기본 컴파일
- [x] 외부 에셋을 설치하지 않은 상태 유지
- [x] Unity용 `.gitignore` 추가
- [x] 기본 `SampleScene`을 실제 게임용 `Main` 씬으로 교체
- [x] Plane, Cube, Sphere로 기본 게임 공간 구성
- [x] Truck 이동 및 고정 쿼터뷰 중심 추적 구현
- [x] Monster Prefab, 배회 및 도주 AI 구현
- [x] 15마리 자동 스폰 및 원거리 제거 구현
- [x] Trigger 접촉 처치 및 런타임 진행도 저장 구현
- [x] 시작 화면 및 `게임 시작하기` 상태 전환 구현
- [x] Level/EXP/Soul/주행 시간/속도 HUD 1차 구현
- [x] FuelPickup과 주행 시간 증가 구현
- [ ] 성장 업그레이드와 Result 흐름을 포함한 첫 플레이 가능 MVP 제작

현재 `Main.unity`에서는 회색 기본 도형만으로 Truck 이동, 카메라 추적, Monster AI, 자동 스폰, 접촉 처치까지 플레이할 수 있다. 사용자가 Play Mode에서 이 흐름과 15마리 자동 유지를 확인했다.

현재 Monster 보상은 밸런스 확정 전이므로 EXP `0`, Soul `0`이다. `PlayerProgress`는 처치 수, EXP, Soul을 현재 실행 중에만 보관하며 Play Mode 종료 시 초기화된다.

다음 우선 작업은 Phase 7 중 **시작 화면과 게임 시작 상태**를 먼저 구현하는 것이다. 그 다음 Phase 6의 성장 시스템과 Phase 7의 HUD/결과 화면을 이어서 구현한다.

---

## 개발 가이드라인

### 1. 회색 도형으로 먼저 검증한다

- Truck은 `Cube`, Monster는 `Sphere`, Ground는 `Plane`으로 만든다.
- 이동, 충돌, 스폰, 보상, 게임 상태가 완성되기 전에는 외부 3D 에셋을 받지 않는다.
- 에셋 교체는 게임 로직과 분리한다. 모델이 바뀌어도 루트 오브젝트와 스크립트는 그대로 유지해야 한다.

### 2. GameObject는 역할, Component는 기능이다

권장 Truck 구성:

```text
Truck
├── Transform
├── Mesh Filter
├── Mesh Renderer
├── Box Collider
├── Rigidbody
└── TruckController.cs
```

권장 Monster 구성:

```text
Monster
├── Transform
├── Mesh Filter
├── Mesh Renderer
├── Sphere Collider
└── MonsterController.cs
```

- 하나의 스크립트가 입력, 이동, UI, 스폰을 모두 담당하지 않게 한다.
- 씬 오브젝트 간 참조는 Inspector의 직렬화 필드로 명시한다.
- `Find`, `FindObjectOfType`, 문자열 기반 검색에 의존하지 않는다.
- 물리 이동은 `FixedUpdate`에서 `Rigidbody`를 통해 처리한다.
- 입력 수집과 물리 이동을 분리한다.

### 3. 씬과 코드의 책임을 분리한다

- 씬: 오브젝트 배치와 참조 연결
- Prefab: 반복 생성되는 Monster 등 오브젝트의 표준 구성
- MonoBehaviour: 런타임 행동
- ScriptableObject 또는 설정 클래스: 밸런스 수치와 몬스터 데이터
- UI: 상태를 표시하고 사용자 명령을 전달하며, 게임 규칙을 직접 계산하지 않음

### 4. 작은 단위로 완성한다

각 Phase는 아래 조건을 모두 만족한 뒤 다음으로 넘어간다.

- Unity Console에 새 Error가 없다.
- Play Mode에서 직접 재현 가능하다.
- 완료 조건을 눈으로 확인할 수 있다.
- 변경된 파일만 Git에 포함된다.

### 5. 프로토타입의 수치를 그대로 복사하지 않는다

Three.js는 프레임마다 위치를 더하지만 Unity는 초 단위와 물리를 사용한다. 예를 들어 `speed = 0.1`을 그대로 옮기지 말고 Unity에서 체감 속도를 다시 조정한다. 먼저 동작을 동일하게 만든 뒤 수치를 튜닝한다.

### 6. 버전 관리 원칙

- `Assets/`, `Packages/`, `ProjectSettings/`만 프로젝트의 핵심 버전 관리 대상으로 삼는다.
- `Library/`, `Temp/`, `Logs/`, `Obj/`, `UserSettings/` 및 IDE 생성 파일은 커밋하지 않는다.
- Unity Editor 밖에서 `.meta` 파일을 임의로 삭제하거나 이름을 바꾸지 않는다.
- 씬과 Prefab을 크게 변경하기 전에 작업 범위를 작게 나누어 커밋한다.

---

## 목표 폴더 구조

초기에는 필요한 폴더만 만들고, 빈 폴더를 미리 대량 생성하지 않는다.

```text
Assets/
├── Scenes/
│   └── Main.unity
├── Scripts/
│   ├── Core/
│   ├── Player/
│   ├── Monsters/
│   ├── Spawning/
│   └── UI/
├── Prefabs/
├── Data/
└── Settings/              # Universal 3D 템플릿의 URP 설정
```

첫 씬의 목표 Hierarchy:

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
├── Systems
│   ├── GameManager
│   ├── SpawnManager
│   └── GameStateManager
└── Canvas
    ├── StartPanel
    ├── HUDPanel
    └── ResultPanel
```

초기 MVP에서는 아직 사용하지 않는 Manager나 Panel을 빈 오브젝트로 모두 만들 필요는 없다. 기능이 생기는 Phase에서 추가한다.

---

## 로드맵

## Phase 0 — 저장소와 프로젝트 정리

목표: Unity가 생성하는 캐시가 저장소에 들어가지 않는 안전한 작업 기반을 만든다.

- [x] 저장소 루트에 Unity용 `.gitignore` 추가
- [x] `Library/`, `Temp/`, `Logs/`, `Obj/`, `UserSettings/` 제외 확인
- [x] `.csproj`, `.sln`, `.slnx` 등 IDE 생성 파일 제외 확인
- [x] Unity 프로젝트의 핵심 파일만 Git 변경 목록에 나타나는지 확인
- [x] 현재 씬을 `Assets/Scenes/Main.unity`로 저장
- [x] Build Profiles/Build Settings의 시작 씬을 `Main.unity`로 변경
- [ ] 템플릿 튜토리얼 파일은 필요 여부를 확인한 뒤 별도 정리

완료 조건:

- `git status`에 `Library/` 내부 파일이 표시되지 않는다.
- Unity를 다시 열었을 때 `Main.unity`가 정상 로드된다.

## Phase 1 — 기본 씬 구성

목표: 아무 코드 없이 게임 공간을 확인한다.

- [x] `Environment` 빈 GameObject 생성
- [x] `Ground` Plane 생성 후 `Environment` 아래 배치
- [x] `Gameplay` 빈 GameObject 생성
- [x] `Truck` Cube 생성 후 `Gameplay` 아래 배치
- [x] Truck 크기를 대략 `(1.5, 1, 3)` 비율로 설정
- [x] `Monster` Sphere 하나를 생성해 테스트 위치에 배치
- [x] Ground, Truck, Monster가 잘 구분되도록 단색 Material 적용
- [x] Main Camera를 트럭이 보이는 탑다운/쿼터뷰 위치에 배치
- [x] Play Mode에서 오브젝트와 그림자, URP 렌더링 확인

완료 조건:

- 게임 화면에서 Plane 위의 Cube와 Sphere가 모두 보인다.
- 외부 모델, 텍스처, 데모 씬을 사용하지 않는다.

## Phase 2 — 트럭 이동과 카메라

목표: 키보드 입력으로 트럭을 운전하고 카메라가 따라간다.

- [x] `Assets/Scripts/Player/TruckController.cs` 생성
- [x] Truck에 `Rigidbody`와 `BoxCollider` 구성
- [x] WASD/방향키 입력 구현
- [x] 전진 가속, 감속/관성, 최대 속도 구현
- [x] 진행 방향을 향한 부드러운 회전 구현
- [x] 필요 없는 Rigidbody 회전축 고정
- [x] `CameraFollow.cs` 생성
- [x] 고정 쿼터뷰에서 보간 없이 Truck 중심 추적 구현
- [ ] 프레임률이 달라도 이동 속도가 크게 달라지지 않는지 확인

완료 조건:

- 트럭이 Ground 위에서 이동하고 입력을 놓으면 자연스럽게 감속한다.
- 카메라가 흔들림 없이 트럭을 따라간다.
- 트럭이 쓰러지거나 지면 아래로 빠지지 않는다.

## Phase 3 — 몬스터 Prefab과 기본 AI

목표: Sphere 몬스터가 배회하고 트럭이 가까워지면 도망간다.

- [x] Monster를 Prefab으로 변환
- [x] `Assets/Scripts/Monsters/MonsterController.cs` 생성
- [x] 무작위 배회 구현
- [x] 일정 거리 안의 Truck 감지 구현
- [x] Truck 반대 방향으로 도주 구현
- [x] 너무 가까울 때 도주 방향이 급격히 뒤집히지 않도록 방향 잠금 구현
- [x] Inspector에서 이동 속도, 도주 거리, 크기 조정 가능하게 구성
- [x] 테스트용 몬스터 여러 개 배치

완료 조건:

- 몬스터가 평소에는 배회한다.
- 트럭 접근 시 트럭 반대 방향으로 도망간다.
- Console Error 없이 여러 몬스터가 동시에 동작한다.

사용자 검증: 완료. Monster의 배회, 도주 및 다중 개체 동작을 Play Mode에서 확인했다.

## Phase 4 — 스폰과 제거

목표: 트럭 주변에 몬스터 개체 수가 자동으로 유지된다.

- [x] `Systems` 빈 GameObject 생성
- [x] `Assets/Scripts/Spawning/SpawnManager.cs` 생성
- [x] Truck에서 최소/최대 거리 사이의 무작위 위치에 스폰
- [x] 목표 몬스터 수 설정
- [x] 프레임마다 대량 생성하지 않고 일정 간격으로 분할 생성
- [x] Truck에서 너무 멀어진 몬스터 제거
- [x] `Gameplay/Monsters` 아래에 런타임 개체 정리
- [x] 먼저 단일 몬스터 타입으로 검증

완료 조건:

- 트럭이 이동해도 주변 몬스터 수가 목표치로 회복된다.
- 멀어진 개체가 계속 누적되지 않는다.
- 100개 목표로 올리기 전에 10~20개로 안정성을 검증한다.

사용자 검증: 완료. 목표 수 15마리 유지, 원거리 제거 및 자동 보충을 Play Mode에서 확인했다.

## Phase 5 — 충돌, 처치, 보상

목표: 트럭이 몬스터와 충돌하면 처치되고 보상이 지급된다.

- [x] Truck을 방해하지 않는 Trigger 방식으로 충돌 규칙 확정
- [x] 충돌한 Monster를 한 번만 처치하도록 보호
- [x] Monster 제거 처리
- [x] `GameManager.cs` 또는 별도 PlayerProgress 클래스 생성
- [x] 경험치와 Soul 보상 저장
- [x] Console 또는 임시 UI로 보상 결과 확인
- [ ] 트럭 크기가 커져도 Collider와 판정이 일치하는지 확인

완료 조건:

- 한 몬스터가 중복 보상을 주지 않는다.
- 충돌 직후 몬스터가 제거되고 경험치/Soul 값이 정확히 증가한다.

사용자 검증: 완료. 접촉 즉시 제거, Truck 비간섭, 처치 수 증가 및 자동 보충을 Play Mode에서 확인했다. EXP/Soul 보상량은 아직 `0`으로 유지한다.

## Phase 6 — 레벨과 업그레이드

목표: 프로토타입의 성장 루프를 복원한다.

- [ ] 시작 레벨, 경험치, Soul, 업그레이드 포인트 구현 (Level/EXP/Soul 완료, 포인트 미구현)
- [x] 다음 레벨 필요 경험치 공식 구현
- [x] 한 번에 여러 레벨이 오르는 경우 처리
- [ ] 레벨당 업그레이드 포인트 지급
- [ ] 속도 업그레이드 구현
- [ ] 크기 업그레이드 구현
- [ ] 크기 변경 시 시각 크기, Collider, 카메라 거리 검증
- [ ] 밸런스 값을 코드 상수가 아닌 Inspector 또는 데이터 자산으로 이동

완료 조건:

- 몬스터 처치만으로 레벨업과 업그레이드가 가능하다.
- 속도/크기 업그레이드 효과가 플레이 중 즉시 반영된다.

## Phase 7 — UI와 게임 상태

목표: 시작, 플레이, 결과 흐름을 화면으로 제어한다.

### Phase 7A — 시작 화면과 게임 시작 상태 (다음 작업)

- [x] Canvas 생성
- [x] 반투명 단색 배경의 `StartPanel` 생성
- [x] 게임 제목 `Isekai Truck` 표시
- [x] `게임 시작하기` 버튼 생성
- [x] `GameStateManager.cs`에 Start, Playing 상태 정의
- [x] 시작 상태에서는 `TruckController` 비활성화
- [x] 시작 상태에서는 `SpawnManager` 비활성화
- [x] 시작 상태에서는 Monster가 생성되거나 움직이지 않도록 제어
- [x] 버튼 클릭 시 StartPanel 숨김 및 Playing 상태 전환
- [x] 시작 화면 뒤에 정지된 Truck과 Ground 표시
- [ ] Play Mode 진입 직후 게임이 자동 시작하지 않는지 확인
- [ ] 버튼을 한 번 눌렀을 때 게임이 정상 시작되는지 확인

### Phase 7B — HUD와 결과 흐름

- [x] `HUDPanel`에 레벨, 경험치, Soul 표시
- [ ] 업그레이드 포인트 및 속도/크기 버튼 추가
- [x] `ResultPanel` 생성
- [x] `GameStateManager.cs`에 Result 상태 추가
- [x] Start/Playing 상태에 따라 입력, 스폰, UI 활성화 제어
- [x] 다시 시작 시 `Main.unity`를 다시 로드하여 런타임 상태 초기화
- [ ] UI가 게임 규칙을 직접 수정하지 않고 Manager API를 호출하도록 구성

완료 조건:

- 시작 → 플레이 → 결과 → 재시작 흐름이 끊김 없이 동작한다.
- HUD 값이 실제 게임 상태와 일치한다.

## Phase 8 — 데이터화와 다중 몬스터

목표: `data/monsters.json`의 역할을 Unity 데이터 구조로 이전한다.

- [ ] 몬스터 타입 데이터 구조 설계
- [ ] ScriptableObject와 JSON 중 Unity 워크플로에 맞는 방식을 선택
- [ ] 이름, 색상, 크기, 속도, 도주 거리, 경험치, Soul, 스폰 가중치 이전
- [ ] 가중치 기반 무작위 타입 선택 구현
- [ ] 잘못된 데이터에 대한 검증 및 기본값 처리
- [ ] 최소 3종 몬스터 동작 확인

완료 조건:

- 코드 수정 없이 데이터 변경만으로 몬스터 능력치와 출현 확률을 조절할 수 있다.

## Phase 9 — 안정화와 성능 확인

목표: 에셋 교체 전에 회색 도형 MVP를 고정한다.

- [ ] 10분 이상 연속 플레이 테스트
- [ ] 몬스터 수가 비정상적으로 증가하지 않는지 확인
- [ ] Profiler로 CPU, GC Alloc, 물리 비용 확인
- [ ] 필요하면 Instantiate/Destroy를 Object Pool로 교체
- [ ] 씬 재시작 시 이벤트와 런타임 오브젝트가 중복되지 않는지 확인
- [ ] 주요 설정값에 유효 범위와 Tooltip 추가
- [ ] Console의 Error와 반복 Warning 제거
- [ ] MVP 플레이 방법을 README에 기록

완료 조건:

- 회색 Cube/Sphere/Plane만으로 핵심 게임 루프가 안정적으로 동작한다.
- 이 시점의 빌드가 향후 그래픽 작업의 기준 버전이 된다.

## Phase 10 — 에셋 교체와 연출

목표: 검증된 게임 오브젝트의 시각 요소만 실제 에셋으로 교체한다.

- [ ] 필요한 에셋 목록과 스타일 기준 작성
- [ ] 라이선스, Unity/URP 버전 호환성, 폴리곤 수 확인
- [ ] 에셋은 별도 테스트 씬 또는 브랜치에서 먼저 Import
- [ ] 데모 씬과 불필요한 샘플 콘텐츠를 게임 씬에 섞지 않기
- [ ] Truck 루트 아래에 모델을 자식으로 넣어 로직/물리 구조 유지
- [ ] Monster도 동일한 방식으로 Mesh만 교체
- [ ] URP Material과 Shader 오류 확인
- [ ] 크기, Pivot, Collider, 그림자 조정
- [ ] 사운드, 파티클, 피격/처치 연출을 순차적으로 추가

완료 조건:

- 모델을 제거하고 기본 도형으로 되돌려도 게임 로직이 계속 동작한다.
- 새 에셋으로 인한 Console Error와 깨진 Material이 없다.

---

## Three.js → Unity 책임 매핑

| 기존 파일 | Unity 대상 | 책임 |
|---|---|---|
| `modules/truck.js` | `TruckController.cs` | 이동, 회전, 속도/크기 능력치 |
| `modules/input.js` | Input System 또는 입력 어댑터 | 키보드/게임패드/터치 입력 |
| `modules/camera.js` | `CameraFollow.cs` | 추적과 줌 |
| `modules/monsters.js` | `MonsterController.cs`, Monster Prefab | 배회, 도주, 충돌 반응 |
| `modules/spawn.js` | `SpawnManager.cs` | 생성 위치, 목표 수, 원거리 제거 |
| `modules/player.js` | `PlayerProgress.cs` | 레벨, 경험치, Soul, 포인트 |
| `modules/ui.js` | HUD 및 UI Controller | 상태 표시와 업그레이드 명령 |
| `modules/config.js` | Inspector 설정/ScriptableObject | 밸런스 값 |
| `data/monsters.json` | Monster 데이터 자산 | 타입별 능력치와 스폰 가중치 |
| `game.js` | `GameManager.cs` | 시스템 연결과 게임 흐름 |

---

## MVP 완료 정의

아래 항목이 모두 충족되면 Unity 첫 MVP가 완료된 것으로 본다.

- [x] `Main.unity` 하나로 게임을 실행할 수 있다.
- [x] Truck Cube를 직접 운전할 수 있다.
- [x] Camera가 Truck을 따라간다.
- [x] Monster Sphere가 자동 생성되고 배회한다.
- [x] Truck 접근 시 Monster가 도망간다.
- [x] 충돌 시 Monster가 제거되고 처치 진행도가 저장된다.
- [ ] 경험치 누적으로 레벨이 오른다.
- [ ] 속도와 크기를 업그레이드할 수 있다.
- [ ] 시작/HUD/결과 UI 흐름이 동작한다.
- [x] 기본 플레이에서 몬스터가 15마리로 자동 유지된다.
- [ ] 장시간 플레이해도 몬스터가 무한 누적되지 않는다.
- [ ] 외부 그래픽 에셋 없이 위 기능이 모두 동작한다.
- [x] 현재 구현 범위에서 Console에 Error가 없다.

## 당장 진행할 작업

1. Phase 0의 Unity `.gitignore`를 추가한다.
2. `SampleScene`을 `Main.unity`로 저장하고 시작 씬으로 지정한다.
3. Plane, Cube, Sphere를 배치해 Phase 1을 완료한다.
4. `TruckController.cs`를 만들고 키보드 이동부터 구현한다.

# 이세계 트럭 키우기 - 개발 및 Unity 이전 지침

## 프로젝트 개요

현재 Three.js 기반 브라우저 게임을 기준 구현으로 사용하며, 동일한 게임 구조와 플레이 감각을 유지한 채 Unity Web 빌드로 이전한다.

플레이어는 트럭을 조작하여 맵을 돌아다니며 몬스터를 치어 처치한다.
몬스터를 처치하면 EXP와 영혼을 획득하고 레벨업 및 트럭 업그레이드가 가능하다.

## 기본 원칙

- 기존 기능을 임의로 삭제하거나 크게 변경하지 않는다.
- 기능을 추가할 때 현재 플레이 감각을 최대한 유지한다.
- 한 파일에 기능을 몰아넣지 않고 현재 모듈 구조를 유지한다.
- 사용자가 요청하지 않은 대규모 리팩터링은 하지 않는다.
- 수정 전에 관련 파일을 먼저 읽고 현재 구현을 확인한다.
- 단순 변수나 짧은 식은 불필요하게 여러 줄로 나누지 않는다.
- 주석은 짧고 기능 중심으로 작성한다.
- 기존 코드의 네이밍, 들여쓰기, 줄바꿈, 주석 방식과 모듈 구성 등 현재 코드 컨벤션을 유지한다.
- Unity 이전 과정에서 기존 Three.js 프로젝트를 기준 구현(reference implementation)으로 취급한다.
- Unity에 맞게 API와 언어는 변경하되, 각 모듈의 책임과 게임 동작은 가능한 한 1:1로 대응시킨다.
- Unity 이전이 완료되고 사용자가 승인하기 전까지 기존 Three.js 소스 파일을 삭제하거나 대체하지 않는다.
- Unity 기능이 더 편리하다는 이유만으로 기존 게임 규칙이나 플레이 감각을 임의로 바꾸지 않는다.

## 기존 Three.js 코드 컨벤션

현재 프로젝트에 이미 사용 중인 코드 스타일을 유지한다.
새 기능을 구현하거나 기존 코드를 수정할 때 임의로 다른 스타일로 변경하지 않는다.

### 기본 스타일

- JavaScript ES Modules 방식을 유지한다.
- 모듈 간 기능은 `import` / `export`를 사용한다.
- 들여쓰기는 공백 4칸을 사용한다.
- 문자열은 기본적으로 작은따옴표 `'`를 사용한다.
- 문장 끝에는 세미콜론 `;`을 사용한다.
- 변수와 함수 이름은 camelCase를 사용한다.
- 설정 객체 이름은 UPPER_SNAKE_CASE를 사용한다.
- 기존 코드에서 이미 사용 중인 네이밍 규칙을 우선한다.

### 줄바꿈

- 단순 변수 선언과 짧은 계산식은 한 줄로 작성한다.
- 긴 계산식이나 조건식은 가독성이 좋아지는 경우에만 여러 줄로 나눈다.
- 기존 파일의 줄바꿈 스타일을 임의로 전체 포맷팅하지 않는다.

예:

```js
const dx = mesh.position.x - truck.position.x;
const dz = mesh.position.z - truck.position.z;
const distance = Math.hypot(dx, dz);
```

### 주석

- 주석은 짧고 기능 중심으로 작성한다.
- 코드 자체로 알 수 있는 내용을 길게 설명하지 않는다.
- 복잡한 로직이나 구현 이유가 필요한 경우에만 추가 설명을 작성한다.

예:

```js
// 트럭 이동
// 몬스터 AI
// 몬스터 충돌
// 배회 방향 변경
```

### 함수와 모듈

- 함수 하나는 가능한 한 하나의 역할을 담당한다.
- 각 파일의 기존 책임을 유지한다.
- 기존 함수가 같은 역할을 수행하고 있다면 비슷한 함수를 중복 생성하지 않는다.
- 새 기능이 현재 파일의 역할과 맞지 않을 경우 새 모듈을 고려하되, 사용자의 요청 없이 대규모 구조 변경은 하지 않는다.

### 설정값과 데이터

- 게임 밸런스 관련 수치는 가능한 한 `modules/config.js`에서 관리한다.
- 이미 설정 객체에 존재하는 값을 다른 파일에 하드코딩하지 않는다.
- 몬스터별 개별 능력치는 `data/monsters.json`에서 관리한다.
- 공통 게임 로직은 JavaScript 모듈에서 관리한다.

### 기존 동작 보존

- 기능 추가나 리팩터링 과정에서 요청받지 않은 기존 동작을 변경하지 않는다.
- 현재 플레이 감각과 확정된 밸런스 값을 임의로 조정하지 않는다.
- 특히 `directionLockMultiplier = 1.72`, 트럭 이동 감각, 몬스터 도망 속도, 카메라 동작, 현재 스폰 설정은 사용자의 요청 없이 변경하지 않는다.
- 리팩터링이 필요하더라도 결과적인 게임 동작은 기존과 동일하게 유지한다.

### 코드 수정 원칙

- 수정 전에 관련 파일의 현재 구현과 주변 코드를 먼저 확인한다.
- 기존 코드 스타일과 동일한 방식으로 구현할 수 있다면 새로운 라이브러리나 복잡한 패턴을 불필요하게 추가하지 않는다.
- 사용자가 요청하지 않은 코드 정리, 파일명 변경, 함수명 변경, 전체 포맷팅을 하지 않는다.


## Unity 이전 원칙

Unity 이전의 목표는 게임을 새로 설계하는 것이 아니라 현재 Three.js 버전의 기능, 구조, 밸런스, 플레이 감각을 Unity에서 재현하는 것이다.

### 원본 보존

- 기존 Three.js 프로젝트는 Unity 버전이 안정될 때까지 그대로 유지한다.
- 기존 JavaScript 코드를 먼저 읽고 실제 동작을 확인한 뒤 C#으로 옮긴다.
- AGENTS.md의 설명과 실제 코드가 다를 경우 임의로 추측하지 말고 차이를 사용자에게 보고한다.
- 한 번에 전체 프로젝트를 재작성하지 않는다.
- 시스템 단위로 이전하고 각 단계가 동작하는지 확인한 뒤 다음 시스템으로 진행한다.

권장 이전 순서:

1. 월드와 카메라
2. 트럭 이동
3. 몬스터 생성과 AI
4. 충돌과 처치 보상
5. 스폰 시스템
6. 플레이어 레벨/EXP/영혼
7. 업그레이드 시스템
8. UI
9. 실제 3D 모델과 이펙트
10. Web 빌드 최적화

### 구조 대응

기존 JavaScript 파일의 역할을 Unity에서도 최대한 유지한다.

```text
game.js
→ GameManager.cs

modules/config.js
→ GameConfig.cs 또는 ScriptableObject

modules/world.js
→ WorldManager.cs

modules/input.js
→ JoystickInput.cs

modules/truck.js
→ TruckController.cs

modules/camera.js
→ CameraController.cs

modules/monsters.js
→ MonsterManager.cs / MonsterController.cs

modules/spawn.js
→ MonsterSpawner.cs

modules/player.js
→ PlayerState.cs

modules/ui.js
→ GameUIController.cs

data/monsters.json
→ 우선 동일 데이터 구조 유지
```

이름은 Unity 프로젝트 상황에 맞게 조정할 수 있지만 각 시스템의 책임을 합치지 않는다.

예:
- `MonsterSpawner`는 스폰 시점/위치/종류만 담당한다.
- 몬스터 AI를 `MonsterSpawner`에 넣지 않는다.
- `TruckController`에 EXP나 UI 로직을 넣지 않는다.
- `GameManager`에 모든 코드를 몰아넣지 않는다.

### 플레이 감각 보존

Unity 이전 초기에 물리적으로 더 현실적인 구현으로 변경하지 않는다.

특히 트럭은 처음부터 `WheelCollider` 자동차 물리로 재설계하지 않는다.
현재 Three.js의 이동 알고리즘을 C#으로 최대한 그대로 옮긴다.

현재 트럭 이동의 핵심:

```text
조이스틱 입력
→ 입력 방향 계산
→ 목표 회전 방향 계산
→ 현재 회전을 turnSpeed만큼 보간
→ acceleration로 속도 증가
→ 현재 트럭이 바라보는 방향으로 이동

조이스틱 해제
→ friction 적용
→ 마지막 진행 방향으로 짧은 관성 이동
```

Unity의 Rigidbody나 CharacterController를 사용할 수는 있지만,
기존 이동 결과와 조작감을 유지하는 것이 우선이다.

사용자가 요청하기 전에는:
- WheelCollider 기반 자동차 물리로 변경하지 않는다.
- 드리프트 시스템을 임의로 추가하지 않는다.
- 가속/마찰/회전 수치를 임의 조정하지 않는다.

### 몬스터 AI 보존

현재 몬스터 AI 규칙을 그대로 유지한다.

평상시:
- 랜덤 방향으로 배회
- 배회 속도는 `type.speed * 0.2`

트럭 감지:
- `fleeDistance` 안에 들어오면 트럭 반대 방향으로 도망
- 도망 속도는 몬스터 데이터의 `speed` 그대로 사용

방향 고정:
- 가까워지기 전에는 매 프레임 도망 방향 갱신
- `directionLockDistance` 안에서는 마지막 도망 방향 유지
- `directionLockMultiplier = 1.72` 유지

이전 테스트에서 취소된 다음 기능은 다시 추가하지 않는다:
- 느린 방향전환
- 도망 방향 갱신 딜레이
- 근거리 도망 속도 감소
- 커브 감속
- flee acceleration

### 충돌과 트럭 크기

현재 핵심 계산을 유지한다.

```text
truckScale = 실제 현재 트럭 크기

collisionDistance =
    baseCollisionDistance * truckScale

directionLockDistance =
    collisionDistance * directionLockMultiplier
```

Unity Collider를 사용하더라도 기존 충돌 범위와 플레이 감각을 크게 바꾸지 않는다.

트럭 크기는 업그레이드뿐 아니라 향후 일시적 거대화 스킬도 반영할 수 있도록
항상 실제 현재 `Transform.localScale`을 기준으로 판단한다.

### 카메라 보존

- 카메라는 트럭 위치를 따라간다.
- 트럭이 회전해도 카메라의 수평 방향은 같이 회전하지 않는다.
- 실제 트럭 크기가 커지면 자동으로 줌아웃한다.
- 줌이 커지면 Fog 범위와 필요한 월드 표시 범위도 증가한다.
- 일반적인 회전형 third-person 카메라로 임의 변경하지 않는다.

### 월드 보존

현재 게임은 경계가 없는 이동 구조다.

Unity에서는 플레이어 주변 타일/청크를 재사용하는 방식으로 같은 결과를 만든다.

- 처음부터 거대한 하나의 Plane으로 임의 변경하지 않는다.
- 플레이어 주변에 필요한 영역만 유지한다.
- 생성된 타일/청크는 가능하면 재사용한다.
- 트럭 크기에 따른 카메라 줌과 Fog 증가를 고려해 필요한 월드 범위를 확장한다.

### 스폰 시스템 보존

현재 설정:

```text
targetCount = 100
minDistance = 35
maxDistance = 70
despawnDistance = 80
spawnInterval = 100ms
spawnPerInterval = 1
```

Unity에서도 시간 단위 동작을 유지한다.

현재 `spawnWeight`는 플레이어 레벨에 따라 변하지 않는다.

레벨 기반 스폰 밸런싱은 논의 후 롤백된 기능이므로
사용자가 다시 요청하기 전까지 구현하지 않는다.

### 데이터 보존

Unity 이전 초기에 데이터까지 동시에 재설계하지 않는다.

`monsters.json`의 필드와 의미를 우선 그대로 유지한다.

현재 주요 필드:

```text
name
color
size
speed
fleeDistance
exp
soul
spawnWeight
```

JSON을 ScriptableObject로 이전하는 것은 가능하지만:
- 필드 의미를 변경하지 않는다.
- 기본 값을 변경하지 않는다.
- 데이터와 게임 로직 분리를 유지한다.
- 사용자 승인 없이 등급 시스템이나 새 spawnWeight 계산식을 추가하지 않는다.

## Unity C# 코드 컨벤션

Unity 쪽 새 코드는 기존 프로젝트의 간결한 스타일을 계승한다.

- 들여쓰기는 공백 4칸을 사용한다.
- 중괄호는 일반적인 C# Allman 스타일을 사용한다.
- 클래스, public 메서드, 프로퍼티는 PascalCase를 사용한다.
- private 필드와 지역 변수는 camelCase를 사용한다.
- bool 이름은 `is`, `has`, `can` 형태를 우선한다.
- 짧은 선언과 계산식은 불필요하게 여러 줄로 나누지 않는다.
- 주석은 짧고 기능 중심으로 작성한다.
- 게임 밸런스 숫자를 로직 안에 중복 하드코딩하지 않는다.
- `public` 필드를 편의상 남발하지 않고 필요한 경우 `[SerializeField] private`를 우선한다.
- `GameObject.Find`, `FindObjectOfType` 같은 전역 검색을 반복 호출하지 않는다.
- `Update()`에서 매 프레임 불필요한 객체나 컬렉션 생성을 피한다.
- 몬스터 수가 많아질 것을 고려해 반복적인 Instantiate/Destroy 남용을 피하고 필요 시 풀링을 고려한다.
- 단, 최적화 때문에 기존 구조와 동작을 임의로 바꾸지 않는다.

## Unity 이전 중 금지 사항

사용자의 명시적 요청 없이 다음 작업을 하지 않는다.

- 기존 Three.js 프로젝트 삭제
- 전체 구조 재설계
- ECS/DOTS 이전
- WheelCollider 기반 차량 물리 적용
- 카메라 회전 방식 변경
- 새로운 스킬 시스템 추가
- 몬스터 등급 시스템 추가
- 레벨 기반 스폰 시스템 추가
- 밸런스 값 자동 조정
- 불필요한 외부 Unity 패키지 설치
- 실제 3D 에셋을 임의로 다운로드하여 추가

## Unity 이전 작업 방식

Codex는 다음 순서로 작업한다.

1. 현재 Three.js 프로젝트와 AGENTS.md를 읽는다.
2. 관련 JavaScript 파일의 실제 구현을 확인한다.
3. Unity에서 대응할 클래스와 책임을 제안한다.
4. 기존 동작과 달라질 수 있는 부분을 먼저 알린다.
5. 가능한 한 작은 단위로 구현한다.
6. 컴파일 오류를 확인한다.
7. 구현한 기능이 기존 Three.js 동작과 어떻게 대응되는지 보고한다.
8. 사용자가 다음 단계 진행을 요청하기 전에는 관계없는 시스템까지 확장하지 않는다.

각 단계 완료 시 아래 항목을 간단히 보고한다.

```text
변경한 파일
현재 구현된 기능
기존 Three.js와의 대응 관계
의도적으로 변경하지 않은 기능
확인이 필요한 점
```


## 파일 역할

game.js
- 게임 시스템 연결
- 메인 게임 루프

modules/config.js
- 밸런스 설정값

modules/world.js
- 무한 바닥 타일
- Fog
- 카메라 줌에 따라 Fog 및 타일 범위 조절

modules/input.js
- 가상 조이스틱

modules/truck.js
- 트럭 이동
- 속도 업그레이드
- 크기 업그레이드

modules/camera.js
- 트럭 위치 추적
- 트럭 실제 크기에 따른 자동 줌아웃
- 트럭 회전에 따라 카메라가 회전하지 않음

modules/monsters.js
- 몬스터 생성 및 제거
- 몬스터 AI
- 트럭 충돌 판정

modules/spawn.js
- 몬스터 종류 선택
- 스폰 위치
- 몬스터 수 유지

modules/player.js
- 레벨
- 경험치
- 영혼
- 업그레이드 포인트

modules/ui.js
- 플레이어 HUD
- 트럭 업그레이드 UI

data/monsters.json
- 몬스터별 능력치 및 spawnWeight

## 현재 중요한 설정

### 트럭

- 기본 최대속도: 0.12
- 가속도: 0.001
- 마찰: 0.94
- 회전속도: 0.03
- 속도 업그레이드당 +0.01
- 크기 업그레이드당 +12%

트럭 이동은 velocity 기반 미끄러짐 방식이 아니다.
조이스틱 입력 중에는 트럭이 바라보는 방향으로 이동하고,
조이스틱을 놓았을 때만 짧은 관성이 존재한다.

## 몬스터 AI

평상시:
- 몬스터는 랜덤 방향으로 천천히 배회한다.
- 배회 속도는 type.speed * 0.2

트럭 감지:
- fleeDistance 안에 들어오면 트럭 반대 방향으로 도망간다.
- 도망 속도는 monsters.json의 speed 값을 그대로 사용한다.

도망 방향 제한:
- 트럭에 매우 가까워지면 몬스터는 도망 방향을 더 이상 갱신하지 않는다.
- directionLockMultiplier = 1.72
- 이 값은 현재 플레이 테스트에서 만족스러운 값이므로 임의로 변경하지 않는다.

몬스터의 방향전환 속도를 제한하는 방식,
도망 방향 갱신 주기를 늦추는 방식,
트럭 가까이에서 몬스터 속도를 낮추는 방식,
커브에서 몬스터 속도를 낮추는 방식은 이전 테스트에서 취소했다.

## 트럭 크기 판정

충돌 거리와 directionLockDistance는
userData.sizeScale이 아니라 실제 현재 truck.scale을 기준으로 계산한다.

예:

const truckScale = Math.max(truck.scale.x, truck.scale.z);

const collisionDistance =
    MONSTER_CONFIG.collisionDistance * truckScale;

const directionLockDistance =
    collisionDistance * MONSTER_CONFIG.directionLockMultiplier;

이 구조는 향후 일시적 거대화 스킬을 고려한 것이다.

## 카메라

카메라는 트럭 위치를 따라가지만 트럭이 회전해도 같이 회전하지 않는다.

카메라 시선은 초기 설정 후 고정한다.
update() 안에서 lookAt()을 반복 호출하지 않는다.

트럭 실제 scale이 증가하면 자동으로 줌아웃한다.
향후 일시적 거대화 스킬도 자동 반영되어야 한다.

카메라 줌이 증가하면:
- Fog 거리 증가
- 필요한 월드 타일 범위 증가

타일은 필요할 때 생성하고 이후 재사용한다.

## 월드

기본 바닥:
- tileSize = 50
- 기본 5x5 타일

Fog:
- 기본 near = 55
- 기본 far = 90
- 색상 = 0x87ceeb

맵에는 경계가 없으며 트럭이 계속 이동할 수 있다.

## 몬스터 스폰

현재 설정:

targetCount = 100
minDistance = 35
maxDistance = 70
despawnDistance = 80
spawnInterval = 100
spawnPerInterval = 1

현재 spawnWeight는 플레이어 레벨에 따라 변하지 않는다.

레벨에 따라:
- 몬스터 수 증가
- 스폰 주기 변화
- 고등급 몬스터 spawnWeight 증가

기능을 논의했지만 아직 적용하지 않았으므로 구현하지 않는다.

## 플레이어

필요 경험치:

baseRequiredExp = 100
expGrowth = 1.5

필요 EXP 계산:

100 * level^1.5

레벨업 시 업그레이드 포인트 1 획득.

업그레이드 포인트로:
- 트럭 속도 증가
- 트럭 크기 증가

중 하나를 선택한다.
업그레이드 종류는 추후 추가될 수 있으니 확장성을 고려해서 구현한다.

## 향후 예정 기능

아직 구현하지 않음:

- 트럭 스킬 시스템 (예: 일시적 거대화 스킬)
- 현재는 플레이어에게서 도망다니는 몬스터만 존재하지만, 플레이어를 쫓아와 부딪혀 공격하는 몬스터도 추가할 예정
- 레벨 기반 스폰 밸런싱
- 다른 맵으로 이동하는 기능

이 항목들은 사용자가 구현을 요청하기 전까지 임의로 추가하지 않는다.
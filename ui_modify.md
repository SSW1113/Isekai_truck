# 현재 게임 UI 개선 방향

현재 UI는 **정보는 잘 나뉘어 있지만, 게임 UI라기보다는 웹 대시보드에 가까운 구조**다.

카툰풍 상업 게임을 목표로 한다면 가장 먼저 수정해야 할 것은 단순한 색상이 아니라 다음 세 가지다.

1. **레이아웃 비중**
2. **시각적 위계**
3. **Shape Language(형태 언어)**

아트 방향은 **CookieRun: Crumble** 같은 카툰풍을 참고하고, UI 구조는 **CookieRun: Kingdom**, **Survivor.io**처럼 이미 검증된 게임들도 함께 참고하는 것이 좋다.

---

## 1. 가장 큰 문제: 게임 화면이 너무 작음

현재 화면은 대략 다음 비율로 구성되어 있다.

> **왼쪽 UI 31% / 게임 화면 38% / 오른쪽 UI 31%**

이 비율은 액션 중심 게임에서는 문제가 크다.

게임의 핵심은 중앙에서 **트럭을 조작하고 몬스터 및 아이템과 상호작용하는 것**인데, 정작 화면의 60% 이상을 HUD가 차지하고 있다.

좋은 액션 게임에서는 그 반대가 되어야 한다.

> **게임플레이가 주인공이고 UI는 게임플레이를 보조해야 한다.**

### 현재 구조

```text
┌────────────┬────────────────┬────────────┐
│            │                │            │
│    UI      │      GAME      │     UI     │
│    31%     │      38%       │     31%    │
│            │                │            │
└────────────┴────────────────┴────────────┘
```

### 추천 구조

```text
┌──────────┬──────────────────────────┬───────────┐
│          │                          │           │
│   UI     │                          │  여신     │
│  18~20%  │        GAME 58~62%       │  20~22%   │
│          │                          │           │
└──────────┴──────────────────────────┴───────────┘
```

또는 더 공격적으로 **전체 화면의 약 70%를 게임 영역으로 확보**해도 좋다.

특히 현재 왼쪽 패널 중앙에 큰 빈 공간이 존재하기 때문에, 좌측 패널을 현재 크기로 유지해야 할 이유가 거의 없다.

---

## 2. 현재 디자인은 카툰 UI보다 파스텔 Prototype UI에 가까움

현재 디자인에는 카툰적인 요소가 일부 들어가 있다.

- 두꺼운 외곽선
- 둥근 모서리
- 파스텔 색상
- 작은 반짝이 장식
- 여러 색상의 카드

하지만 이런 요소를 추가하는 것만으로 CookieRun 스타일이 되는 것은 아니다.

현재 대부분의 UI가 사실상 동일한 구조를 가지고 있다.

```text
둥근 사각형
↓
검은 테두리
↓
색칠
↓
상단의 흰색 장식 막대
↓
가운데 텍스트
```

LEVEL, EXP, SOUL, SPEED 등이 모두 이 구조를 반복한다.

그래서 현재 화면은

> **게임 세계에 존재하는 UI**

라기보다는

> **Unity에서 같은 Prefab의 색상만 변경한 UI**

처럼 보이는 문제가 있다.

### CookieRun 계열에서 참고해야 하는 핵심: Shape Language

CookieRun 계열에서 가져와야 할 핵심은 단순한 색상이 아니라 **정보에 따라 UI의 실루엣이 달라지는 형태 언어**다.

```text
LEVEL
→ 리본 / 배지

EXP
→ 젤리처럼 볼록한 게이지

SOUL
→ 영혼 아이콘 + 숫자

SPEED
→ 계기판 / 바람 / 속도선

UPGRADE
→ 크고 말랑한 CTA 버튼

GODDESS
→ 액자 / 메달 / 말풍선
```

즉,

> **정보의 종류에 따라 UI의 형태 자체가 달라져야 한다.**

---

## 3. 테두리가 너무 많음

현재 거의 모든 UI가

> **검은색 → 갈색 → 내부 색상**

형태로 2~3중 테두리를 사용하고 있다.

개별 요소만 보면 나쁘지 않지만, 화면 전체에서 반복되면서 상당히 무거운 인상을 만든다.

현재 강한 테두리가 적용되어 있는 요소는 다음과 같다.

- 왼쪽 전체 패널
- LEVEL 카드
- EXP 카드
- 업그레이드 카드
- 업그레이드 버튼
- 오른쪽 전체 패널
- 여신 카드
- 여신 대화창
- 영혼 카드
- 속도 카드

모든 요소가 비슷한 시각적 강도를 가지기 때문에

> **사용자가 어디를 먼저 봐야 하는지 구분하기 어렵다.**

### Border Hierarchy를 만들어야 함

#### 1단계 — 최상위 패널

- 얇은 외곽선
- 또는 약한 그림자

#### 2단계 — 일반 정보 UI

- 테두리를 최소화
- 배경색과 약한 명암으로 구분

#### 3단계 — 중요한 버튼

- 강한 외곽선
- 그림자
- 입체감

#### 4단계 — 현재 상호작용 가능한 요소

- Glow
- Bounce
- Highlight
- 작은 반짝임

즉,

> **모든 UI가 동시에 "나 중요해!"라고 말하면 안 된다.**

---

## 4. 색상은 많지만 색상의 의미가 없음

현재 UI는 대략 다음과 같은 색을 사용하고 있다.

- LEVEL = 초록
- EXP = 보라
- Upgrade = 주황
- Goddess = 연보라
- Soul = 분홍
- Speed = 파랑

개별 색상 자체는 문제가 없다.

문제는

> **왜 이 UI가 이 색인지 플레이어가 학습할 수 있는 규칙이 없다는 것**

이다.

게임 전체에서 사용할 **Semantic Color System**을 정의하는 것이 좋다.

| 의미 | 추천 색상 계열 |
|---|---|
| 성장 / EXP | 보라 |
| 영혼 / 환생 | 핑크·보라 |
| 주행 / 속도 | 하늘색 |
| 보상 / 획득 | 노랑 / 금색 |
| 업그레이드 가능 | 주황 |
| 기본 패널 | 크림색 |

이 규칙을 게임 전체에 적용하면 플레이어는 자연스럽게 색상의 의미를 학습한다.

예를 들어

> **보라색 UI = 성장과 관련된 정보**

라는 규칙이 만들어진다.

---

## 5. LEVEL과 EXP는 하나의 UI로 통합

현재는 LEVEL과 EXP가 서로 다른 큰 카드를 사용하고 있다.

```text
┌──────────────┐
│    LEVEL     │
│    Lv. 6     │
└──────────────┘

┌──────────────┐
│ EXP          │
│ 1370 / 1470  │
│ ━━━━━━━━━━━  │
└──────────────┘
```

하지만 LEVEL과 EXP는 같은 성장 시스템에 포함되는 정보이므로 굳이 두 개의 큰 카드를 사용할 필요가 없다.

### 추천

```text
╭──────────────────────────╮
│ ⭐ LV.6                   │
│ ███████████████░ 1370/1470│
╰──────────────────────────╯
```

또는 조금 더 카툰스럽게 만들면

```text
       ┌── LV.6 ──┐
    ╭──╯          ╰──╮
    │ ██████████░░░  │
    ╰────────────────╯
```

처럼 구성할 수 있다.

이렇게 하면 왼쪽 상단 UI 높이를 상당히 줄일 수 있다.

---

## 6. 업그레이드 UI와 성장 UI의 관계가 너무 멀리 떨어져 있음

현재 구조는 대략 다음과 같다.

```text
LEVEL / EXP



      큰 공백



POINT / UPGRADE
```

하지만 실제 게임 시스템에서는

```text
EXP 획득
↓
Level 증가
↓
Point 획득
↓
Upgrade
```

라는 하나의 Progression Loop다.

따라서 시각적으로도 이 관계가 연결되어 있어야 한다.

### 추천 구조

```text
┌─────────────────────┐
│        LV. 6        │
│ ███████████░ 1370   │
└─────────────────────┘

     +1 POINT!
         ↓

┌─────────────────────┐
│   ⚡ 업그레이드     │
│       1 사용 가능   │
└─────────────────────┘
```

업그레이드 가능한 순간에는 다음 정도의 피드백을 추가한다.

- 버튼이 살짝 커졌다 작아지는 Bounce
- 작은 반짝이
- `!` 표시
- 금색 Rim / Highlight
- 짧은 효과음

지속적으로 강한 애니메이션을 사용하기보다 **업그레이드 가능 상태에서만 강조**하는 것이 좋다.

---

## 7. 여신 UI를 게임의 핵심 차별점으로 활용

현재 여신 UI는 사실상 일반적인 NPC 프로필 창에 가깝다.

```text
[ 빈 bar ]

      ○
    사람 그림

[여신이 지켜보고 있습니다]
```

하지만 여신이 플레이 상황을 관찰하고 실시간으로 반응하는 시스템이라면, 이 요소는 게임의 강한 차별점이 될 수 있다.

따라서 여신을 단순한 HUD 정보가 아니라

> **게임플레이에 참여하는 캐릭터**

처럼 보여주는 것이 좋다.

### 추천 구조

```text
        ✦
    ╭─────────╮
    │ GODDESS │
    │ Portrait│
    ╰─────────╯
       ╲
        ╲  "조금 빠른 것 같은데...?"
         ╭─────────────────╮
         │                 │
         ╰─────────────────╯
```

Portrait와 대화창을 하나의 카드 안에 가두기보다는 **대화창이 Portrait 밖으로 튀어나오는 형태**가 더 좋다.

### 상황별 여신 반응 예시

#### 속도 증가

> "오... 제법 빨라졌는데?"

#### 위험 상태

> "잠깐, 이대로 가면 위험해!"

#### 레벨업

> "조금은 쓸 만해졌네?"

#### 몬스터 대량 처치

> "그렇게까지 할 필요가 있었어...?"

#### 업그레이드 가능

> "힘을 조금 더 줄까?"

#### 환생 가능

> "이번 생은 여기까지 할래?"

이렇게 하면 여신이 단순한 UI가 아니라 **게임의 Personality를 담당하는 시스템**이 된다.

---

## 8. 영혼 UI는 더 압축할 수 있음

현재 `영혼 863`이라는 정보를 표시하기 위해 카드 하나를 크게 사용하고 있다.

```text
┌──────────────────────┐
│       영혼           │
│                      │
│        863           │
└──────────────────────┘
```

하지만 실제로 필요한 정보량은 매우 적다.

### 추천

```text
👻 영혼 863
```

또는

```text
╭──────────────╮
│ ◉ 863 영혼   │
╰──────────────╯
```

카툰 UI라고 해서 반드시 UI 크기가 커야 하는 것은 아니다.

오히려 **아이콘 + 숫자 조합으로 정보를 빠르게 읽을 수 있도록 압축하는 것**이 중요하다.

---

## 9. 속도 UI는 게임 화면 가까이에 배치

`0 km/h`는 지속적으로 변하는 **실시간 Gameplay Information**이다.

현재 구조에서는 플레이어가 속도를 확인하려면

```text
트럭
↓
게임 화면
↓
오른쪽 패널
↓
패널 아래쪽
↓
속도 확인
```

처럼 시선을 크게 이동해야 한다.

속도는 중앙 게임 화면의 우측 하단처럼 **플레이어 캐릭터와 가까운 HUD 영역**에 표시하는 것이 더 적절하다.

### 예시

```text
                      ╭─────────╮
                      │ 72 km/h │
                      ╰─────────╯
```

또는 작은 Speedometer 형태로 만들 수 있다.

```text
         72
     ╭────────╮
   ◜            ◝
  ◜              ◝
       km/h
```

가속 시에는 다음과 같은 효과를 추가할 수 있다.

- 숫자가 빠르게 Roll
- Speed UI가 살짝 뒤로 밀림
- 바람선 증가
- 숫자가 순간적으로 확대
- 고속에서 작은 Screen Shake
- 속도에 따라 UI 흔들림 증가

`속도`라는 텍스트는 제거해도 된다.

> **Speedometer + km/h만으로 충분히 의미를 전달할 수 있다.**

---

## 10. 환생 버튼 디자인과 위치 수정

현재 중앙 상단의 회색 `환생` 버튼은 전체 UI와 스타일이 완전히 다르다.

Unity 기본 Button처럼 보이기 때문에 우선적으로 수정할 필요가 있다.

환생은 영혼 시스템과 직접 연결되어 있으므로 **영혼 UI와 하나의 그룹으로 구성하는 것**이 좋다.

### 예시

```text
👻 영혼 863

     ↓

╭──────────────────╮
│ ✨ 환생 가능!    │
╰──────────────────╯
```

또는 평소에는 환생 버튼을 숨기고

> **환생 조건을 만족했을 때만 버튼을 등장**

시키는 방법도 좋다.

이 경우 등장할 때

- Fade In
- Scale Bounce
- Glow
- 여신 반응

등을 함께 사용하면 훨씬 자연스럽다.

---

## 11. 의미 없는 장식 대신 게임의 소재를 장식으로 활용

현재 화면 곳곳에 작은 `✦` 장식이 존재한다.

하지만 같은 장식이 반복되면서 특별한 의미를 가지지 못한다.

카툰풍 게임에서는 단순한 장식보다 **게임의 세계관과 연결된 소재를 UI Decoration으로 사용하는 것**이 좋다.

현재 게임에는 다음과 같은 소재가 있다.

- 트럭
- 자동차
- 이세계
- 여신
- 영혼
- 몬스터
- 도로
- 속도
- 업그레이드

이를 UI 장식으로 활용할 수 있다.

| UI | 장식 소재 |
|---|---|
| 패널 모서리 | 볼트 / 나사 |
| Speed | 바람선 / 타이어 자국 |
| Level | 별 / 메달 |
| Soul | 유령 / 불꽃 |
| Upgrade | 렌치 / 기어 |
| Goddess | 날개 / 후광 |
| EXP | 빛나는 조각 |
| Button | 트럭 번호판 |
| 구분선 | 도로 중앙선 |

이렇게 하면 단순히 CookieRun UI를 따라 하는 것이 아니라

> **이 게임만의 Shape Language와 Visual Identity**

를 만들 수 있다.

---

## 12. UI와 3D 게임 화면의 스타일이 서로 다름

현재 중앙 게임 화면은

- 단순한 사각형 트럭
- Checkerboard 지형
- 낮은 채도
- 비교적 현실적인 Perspective
- 단순한 Material

형태다.

반면 UI는

- Pastel Color
- 둥근 Cartoon Panel
- Sparkle
- 두꺼운 Outline
- 밝은 색상

을 사용하고 있다.

그래서 현재 상태에서는

> **UI와 게임 화면이 서로 다른 두 게임처럼 보일 수 있다.**

UI만 CookieRun 스타일로 강화하면 오히려 이 차이가 더 커질 수 있다.

따라서 3D 게임 화면 역시 어느 정도 카툰 스타일로 통일해야 한다.

### 3D 화면에 추가하면 좋은 요소

- Toon Shader
- 부드럽고 단순화된 그림자
- 높은 색상 채도
- 캐릭터 / 오브젝트 Outline
- 과장된 Squash & Stretch
- Hit Effect
- 아이템 주변 Glow
- Cartoon Dust
- Speed Line
- Impact Effect
- 몬스터 사망 시 과장된 연출
- 트럭 가속 시 Body Tilt
- 트럭 회전 시 약간의 Lean

UI뿐 아니라 **실제 게임 화면 자체에도 Cartoon Feedback을 추가해야 한다.**

---

## 13. 가장 부족한 부분: Juice

현재 게임 UI는 데이터를 **표시**하고 있다.

하지만 좋은 상업 게임은 데이터를 단순히 표시하는 것이 아니라

> **데이터의 변화를 연출한다.**

### 영혼 획득

단순히

```text
863 → 864
```

로 변경하는 것이 아니라

```text
몬스터 사망
↓
작은 영혼 등장
↓
영혼이 플레이어에게 빨려 들어감
↓
영혼이 HUD의 Soul Icon 방향으로 이동
↓
863 → 864
↓
숫자 Pop
↓
작은 Spark / Sound
```

처럼 Feedback Loop를 만든다.

### EXP 획득

```text
+20 EXP
↓
EXP 게이지가 Tween으로 증가
↓
MAX 도달
↓
LEVEL UP!
↓
Lv.6 → Lv.7
↓
Upgrade Point +1
↓
Upgrade 버튼 Bounce
```

### 속도 증가

```text
43
48
57
66
72 km/h
```

단순 숫자 변경이 아니라

- 숫자 Roll
- 순간 Scale Up
- Speed UI 흔들림
- 바람선 증가
- 트럭 뒤 Dust 증가
- Camera FOV 변화
- 고속 상태에서 Screen Effect 증가

등을 사용할 수 있다.

---

# 최종 추천 레이아웃

전체적인 구조는 다음과 같이 가져가는 것이 좋다.

```text
╔══════════════╦══════════════════════════════════╦════════════════╗
║              ║   거리 / 기타 핵심 HUD          ║                ║
║   ⭐ LV.6    ║                                  ║    GODDESS     ║
║ ███████░░    ║                                  ║    Portrait    ║
║              ║                                  ║                ║
║              ║             GAME                 ║  ╭──────────╮  ║
║              ║                                  ║  │ "..."    │  ║
║              ║            🚚                    ║  ╰──────────╯  ║
║              ║                                  ║                ║
║              ║                                  ║  👻 863       ║
║              ║                         72 km/h   ║                ║
║ ╭──────────╮ ║                                  ║                ║
║ │UPGRADE ! │ ║                                  ║                ║
║ ╰──────────╯ ║                                  ║                ║
╚══════════════╩══════════════════════════════════╩════════════════╝
       19%                       60%                       21%
```

핵심은 현재의

> **좌우 패널을 사용하는 독특한 레이아웃**

자체를 없애는 것이 아니다.

좌우 패널 구조는 유지하되,

> **게임 화면이 화면의 주인공이 되도록 UI의 크기와 정보 밀도를 줄이는 것**

이 중요하다.

---

# UI 수정 우선순위

| 우선순위 | 수정사항 | 중요도 |
|---|---|---|
| **1** | 중앙 게임 영역 확대 | ★★★★★ |
| **2** | 동일한 사각형 카드 반복 제거 | ★★★★★ |
| **3** | LEVEL + EXP 통합 | ★★★★★ |
| **4** | 여신을 UI의 핵심 캐릭터로 재설계 | ★★★★★ |
| **5** | UI Animation / Feedback / Juice 추가 | ★★★★★ |
| **6** | 트럭과 맵의 Toon 스타일 통일 | ★★★★★ |
| **7** | Speed를 Gameplay 영역으로 이동 | ★★★★☆ |
| **8** | Soul UI 압축 | ★★★★☆ |
| **9** | Upgrade와 성장 UI 연결 | ★★★★☆ |
| **10** | Border 강도 단계화 | ★★★★☆ |
| **11** | 색상에 의미 부여 | ★★★☆☆ |

---

# 핵심 디자인 원칙

현재 UI는

> **예쁜 색의 UI 박스를 좌우에 배열한 화면**

에 가깝다.

목표는

> **캐릭터와 게임 세계 자체가 HUD까지 침범하는 카툰 게임 화면**

이 되어야 한다.

특히 CookieRun: Crumble 같은 카툰 스타일을 목표로 한다면 단순히 파스텔 색상을 추가하거나 모서리를 둥글게 만드는 것보다 다음이 훨씬 중요하다.

## 1. 사각형을 줄인다

동일한 Rounded Rectangle Card의 반복을 피한다.

## 2. 아이콘을 늘린다

텍스트만으로 정보를 전달하지 않고 아이콘과 형태를 이용한다.

## 3. UI마다 실루엣을 다르게 만든다

LEVEL, SOUL, SPEED, UPGRADE, GODDESS가 각각 다른 형태를 가진다.

## 4. 캐릭터를 UI에 적극적으로 사용한다

특히 여신은 단순 Portrait가 아니라 플레이 상황에 반응하는 핵심 UI 캐릭터로 활용한다.

## 5. UI를 정적인 정보판으로 만들지 않는다

획득, 성장, 가속, 레벨업, 환생 등 모든 중요한 상태 변화에 Animation과 Feedback을 추가한다.

## 6. 게임플레이 영역을 가장 크게 만든다

HUD가 게임 화면을 압도해서는 안 된다.

---

# 한 문장으로 정리

> **CookieRun 스타일을 만들기 위해 중요한 것은 파스텔 색상이나 두꺼운 테두리가 아니라, 서로 다른 Shape Language + 캐릭터 중심의 UI + 강한 Gameplay Feedback이다.**

---

# 현재 Unity 코드 기준 구현 계획

이 섹션은 위 디자인 방향을 현재 Unity 프로젝트에 실제로 적용할 때 참고하는 개발 지침이다.

기준 Unity 프로젝트는 다음 경로다.

```text
Unity/Isekai_Truck/
```

루트의 Three.js 파일은 과거 구현이므로 새 UI 작업의 대상이 아니다.

## UI 생성 원본과 수정 규칙

현재 Main HUD는 UI Prefab이 아니라 다음 Editor 스크립트가 생성한다.

```text
Assets/IsekaiTruck/Editor/SeventhStageSetup.cs
```

`SeventhStageSetup.Setup()`은 기존 `Game UI`를 삭제한 뒤 전체 HUD를 다시 생성한다. 따라서 `Main.unity`에서만 Hierarchy나 스타일을 수정하면 Setup을 다시 실행했을 때 변경 사항이 사라진다.

Main HUD를 수정할 때는 반드시 다음 항목을 함께 반영한다.

1. `SeventhStageSetup.cs`의 생성 코드
2. `GameUIController.cs`의 직렬화 참조 및 표시 로직
3. `SeventhStageSetup.Verify()`의 계층 및 참조 검증
4. `Main.unity`의 현재 씬 인스턴스

환생 UI의 생성 원본은 다음 파일이다.

```text
Assets/IsekaiTruck/Editor/RebirthFeatureSetup.cs
```

환생 UI를 수정할 때도 `RebirthUIController.cs`, `RebirthFeatureSetup.Verify()`, `Main.unity`를 함께 갱신한다.

현재 EXP 게이지는 타원형 Capsule이 아니라 가로형 `Progress Bar`로 변경되어 있다. 이후 UI를 재구성할 때도 원형 `Knob` 스프라이트를 다시 적용하지 않고, 현재의 사각형 계열 Bar 스타일을 유지한다.

## 구현 대상 파일

주요 수정 파일은 다음과 같다.

```text
Assets/IsekaiTruck/Editor/SeventhStageSetup.cs
Assets/IsekaiTruck/Editor/RebirthFeatureSetup.cs
Assets/IsekaiTruck/Scripts/UI/GameUIController.cs
Assets/IsekaiTruck/Scripts/UI/RebirthUIController.cs
Assets/IsekaiTruck/Scenes/Main.unity
```

필요한 경우 아래처럼 작은 UI 효과 컴포넌트를 추가할 수 있다.

```text
Assets/IsekaiTruck/Scripts/UI/Effects/
├── UIValuePopEffect.cs
├── UIAttentionEffect.cs
└── UIProgressBarEffect.cs
```

외부 Tween 또는 UI 패키지는 추가하지 않는다. 애니메이션은 Coroutine, `Update()` 또는 기존 uGUI 기능으로 구현한다.

## 1. 중앙 게임 영역 확대

현재 카메라 Viewport 비율은 `GameConfig.asset`의 `11:16`이다. `GameUIController.SetViewport()`는 카메라 바깥의 실제 여백을 좌우 패널로 사용한다.

16:9 화면에서 `11:16` Viewport가 차지하는 가로 폭은 약 39%다. 따라서 좌우 패널 Anchor만 줄여서는 중앙 게임 화면을 60%로 확대할 수 없다.

중앙 영역을 약 60%로 확대하려면 다음 설정과 연결 시스템을 함께 검토해야 한다.

```text
Assets/IsekaiTruck/Config/GameConfig.asset
Assets/IsekaiTruck/Scripts/Config/GameConfig.cs
Assets/IsekaiTruck/Scripts/Camera/CameraController.cs
Assets/IsekaiTruck/Scripts/UI/GameUIController.cs
Assets/IsekaiTruck/Scripts/Input/JoystickInput.cs
Assets/IsekaiTruck/Scripts/World/WorldManager.cs
```

예상 Viewport 비율은 대략 `16:15`에서 `17:16` 사이다. 정확한 값은 1920x1080과 1280x720 화면에서 실제 플레이 테스트 후 확정한다.

이 변경은 단순 UI 변경이 아니라 다음 요소에 영향을 줄 수 있다.

- 카메라가 보여주는 좌우 범위
- 몬스터와 월드의 가시 범위
- 조이스틱 입력 영역
- 팝업과 환생 UI 배치
- 기존 플레이 감각

따라서 게임플레이 담당자와 Viewport 비율을 확정하기 전에는 `GameConfig`와 카메라 비율을 임의로 변경하지 않는다. 비율 변경이 승인되지 않으면 기존 `11:16`을 유지한 상태에서 정보 밀도와 시각적 무게만 줄인다.

## 2. LEVEL과 EXP 통합

현재의 `LevelCard`와 `ExpCard`를 하나의 성장 HUD로 통합한다.

추천 계층은 다음과 같다.

```text
Growth HUD
├── Level Badge
├── Level Text
├── EXP Bar
├── EXP Text
└── Level Up Effect Anchor
```

`GameUIController`의 다음 기존 참조는 유지한다.

```text
levelText
expText
expFill
```

레벨, 현재 EXP, 필요 EXP와 게이지 비율 계산은 현재 `PlayerState`와 `GameUIController.Refresh()` 흐름을 그대로 사용한다. UI에서 레벨업이나 필요 경험치를 새로 계산하지 않는다.

## 3. 왼쪽 패널 재구성

왼쪽 패널은 성장 흐름이 위에서 아래로 자연스럽게 이어지도록 구성한다.

```text
LeftPanel
├── Growth HUD
│   ├── Level
│   └── EXP
├── Upgrade CTA
│   ├── Point Badge
│   ├── Upgrade Button
│   └── Available Indicator
└── Fuel Reserved Area
```

`Fuel Reserved Area`는 향후 연료 기능을 위한 빈 컨테이너로만 유지한다. 가짜 연료 값이나 연료 시스템은 구현하지 않고, 현재처럼 큰 공백을 만들지 않도록 크기만 줄인다.

업그레이드 버튼은 성장 HUD 바로 아래에 배치하여 다음 흐름을 시각적으로 연결한다.

```text
EXP 획득 → 레벨업 → 포인트 획득 → 업그레이드
```

업그레이드 포인트가 `0`에서 `1 이상`으로 변경되는 순간에만 다음 피드백을 제공한다.

- 버튼 Scale Bounce 1회
- `!` 또는 사용 가능 배지 표시
- 금색 Rim 또는 Highlight
- 짧은 반짝임

지속적으로 강한 애니메이션을 반복하지 않는다.

## 4. 용도별 Shape Language 적용

현재 대부분의 UI가 `CreateCartoonPanel()`을 사용하여 같은 둥근 사각형 구조를 반복한다. 새 구조에서는 모든 UI를 하나의 공통 Panel 모양으로 만들지 않는다.

각 정보의 형태는 다음처럼 구분한다.

| 정보 | 형태 |
|---|---|
| 성장 | Level Badge와 EXP Bar가 결합된 형태 |
| 영혼 | 아이콘과 숫자로 구성된 작은 Chip |
| 속도 | Speedometer, 속도선 또는 기울어진 HUD |
| 업그레이드 | 깊이감과 강한 외곽선이 있는 CTA 버튼 |
| 여신 | Portrait Frame과 바깥으로 돌출된 Speech Bubble |
| 일반 패널 | 약한 배경 명암과 최소한의 테두리 |

`CreateCartoonPanel()`은 일반 패널에만 제한적으로 사용하고, 성장·영혼·속도·여신·업그레이드는 각각 전용 생성 메서드로 구성한다.

불필요한 인터페이스나 상속 구조를 만들지 않고 `SeventhStageSetup` 안의 명확한 생성 메서드 또는 작은 View 컴포넌트 정도로만 분리한다.

## 5. Border Hierarchy

모든 요소에 같은 두께의 Outline과 Depth를 적용하지 않는다.

```text
최상위 좌우 패널
→ 얇은 외곽선 또는 약한 그림자

일반 정보 UI
→ 배경색과 약한 명암 중심, 테두리 최소화

중요 CTA 버튼
→ 강한 외곽선, Depth, 눌림 효과 유지

현재 상호작용 가능 요소
→ 일시적인 Glow, Bounce, Highlight
```

기존 `CartoonButtonPressEffect`는 업그레이드와 중요한 버튼에 계속 사용할 수 있다.

## 6. Semantic Color System

색상은 다음 의미 체계를 유지한다.

| 의미 | 색상 계열 |
|---|---|
| 성장 / EXP | 보라 |
| 영혼 / 환생 | 핑크·보라 |
| 주행 / 속도 | 하늘색 |
| 보상 / 획득 | 노랑·금색 |
| 업그레이드 가능 | 주황 |
| 기본 패널 | 크림색 또는 낮은 채도의 중립색 |

색상 상수는 가능하면 `SeventhStageSetup.CreateUI()` 시작 부분에 의미 기반 이름으로 모은다. 같은 의미에 서로 다른 임의 색상을 추가하지 않는다.

## 7. 여신 UI 재구성

여신 UI는 다음 계층을 기준으로 재구성한다.

```text
Goddess Area
├── Portrait Frame
│   └── Goddess Visual
├── Speech Bubble
│   └── Goddess Message
└── Soul/Rebirth Group
```

Portrait와 말풍선을 하나의 일반 카드 안에 가두지 않고, 말풍선이 Portrait 바깥으로 일부 돌출된 형태를 사용한다.

여신 실루엣과 임시 문구는 실제 여신 이미지와 반응 시스템이 준비되기 전까지 Placeholder로 유지한다.

다음 상황별 반응은 현재 게임에 없는 새 기능이므로 단순 UI 리디자인과 분리한다.

- 속도 증가
- 위험 상태
- 레벨업
- 몬스터 대량 처치
- 업그레이드 가능
- 환생 가능

실제 반응 기능을 구현할 때는 별도 `GoddessUIController` 또는 Presenter를 두고, 게임 시스템 이벤트를 받아 메시지와 연출만 선택하도록 한다. `GameUIController`에 여신 반응 조건을 모두 몰아넣지 않는다.

## 8. 영혼 UI 압축

현재 `SoulCard`는 다음처럼 작은 정보 단위로 변경한다.

```text
Soul Chip
├── Soul Icon
└── Soul Text
```

기존 `GameUIController.soulText`와 `PlayerState.StateChanged` 연결은 그대로 유지한다. UI가 영혼 값을 직접 계산하거나 수정하지 않는다.

환생 진입 UI와 가까이 배치하여 영혼과 환생의 관계가 자연스럽게 보이도록 한다.

## 9. 속도 UI를 게임 영역으로 이동

현재 우측 패널의 `SpeedCard`를 제거하고 속도 HUD를 중앙 `Game Area UI` 우측 하단으로 이동한다.

```text
Game Area UI
├── Speed HUD
│   ├── Speed Lines
│   ├── Speed Text
│   └── km/h
└── Upgrade Panel
```

기존 `speedText` 참조와 `TruckController.CurrentSpeedPerSecond * 3.6f` 표시 로직은 유지한다. 실제 속도, 가속도, 마찰과 밸런스 값은 변경하지 않는다.

초기 구현에서는 숫자와 `km/h`만 명확하게 표시하고, 과도한 흔들림이나 Screen Shake는 추가하지 않는다. 숫자 Roll, Scale 반응, 속도선 강도 변화는 별도 효과 단계에서 추가한다.

## 10. 환생 버튼 재디자인

현재 환생 버튼은 `Rebirth UI` 아래에서 별도로 생성되므로 Main HUD와 생성 소유권이 다르다.

수정 방향은 다음과 같다.

- 중앙 상단의 Unity 기본 스타일 버튼 제거
- 영혼 UI 근처에 환생 진입 요소 배치
- 기존 `RebirthUIController.openButton` 클릭 계약 유지
- 환생 가능 여부에 따라 노출 또는 강조
- 환생 Panel과 축복 후보 선택 흐름은 그대로 유지
- 환생 Panel이 열릴 때 게임 정지와 입력 차단 유지

`SeventhStageSetup`이 `Game UI`를 삭제할 때 환생 버튼까지 함께 삭제되지 않도록 소유권을 명확히 해야 한다. Main HUD 아래로 무작정 Reparent하지 않는다.

권장 방식은 `Rebirth UI` 자체 계층은 유지하되, `Rebirth Game Area`의 RectTransform을 우측 영혼 영역과 시각적으로 일치시키는 것이다. 생성 순서와 참조 파손 가능성을 검증한 뒤 통합 여부를 결정한다.

## 11. UI Feedback와 Juice

현재 `GameUIController.Refresh()`는 값을 즉시 교체한다. 다음 효과는 상태 변화가 있을 때만 재생한다.

### EXP

- EXP Bar를 목표 비율까지 부드럽게 증가
- 레벨업 시 Level Badge Pop
- 레벨업 문구 또는 짧은 Highlight
- 업그레이드 포인트 획득 후 Upgrade CTA 강조

### 영혼

- 숫자 증가 시 짧은 Scale Pop
- Soul Icon에 짧은 Highlight

### 속도

- 반올림된 속도 값이 크게 바뀔 때 짧은 Scale 반응
- 속도 구간에 따라 Speed Line 강도 조절

효과 구현 시 이전 `PlayerSnapshot` 또는 이전 표시값을 저장하여 변화량을 판단한다. 매 프레임 새 List, 배열 또는 불필요한 문자열을 생성하지 않는다.

몬스터에서 HUD까지 날아오는 영혼, Camera FOV 변화, Screen Shake, 3D Dust와 Hit Effect는 UI만의 변경이 아니므로 별도 게임 이펙트 작업으로 분리한다.

## 12. 장식 소재 변경

반복되는 의미 없는 Sparkle은 줄이고 게임 소재와 연결된 장식을 사용한다.

| UI | 사용할 수 있는 소재 |
|---|---|
| 패널 모서리 | 볼트·나사 |
| Speed | 바람선·타이어 자국 |
| Level | 별·메달 |
| Soul | 유령·불꽃 |
| Upgrade | 렌치·기어·번호판 |
| Goddess | 날개·후광 |
| EXP | 빛나는 조각 |
| 구분선 | 도로 중앙선 |

새 외부 에셋을 임의로 추가하지 않는다. 초기 구현에서는 기존 uGUI `Image` 조합으로 표현하고, 실제 Sprite 제작은 별도 아트 작업으로 교체 가능하게 구성한다.

## 13. UI 범위를 벗어나는 항목

다음 항목은 이 UI 개편과 동시에 구현하지 않는다.

- Toon Shader 적용
- 트럭과 몬스터 Outline
- 트럭 Squash & Stretch
- 카메라 FOV 및 Screen Shake 변경
- 몬스터 사망 연출
- 3D Soul 획득 이펙트
- Dust, Speed Line 등 월드 파티클

이 항목들은 3D 아트, 게임플레이 및 이펙트 담당 범위로 별도 작업한다. UI 개편을 이유로 `TruckController`, `MonsterController`, `WorldManager`, `CameraController`의 핵심 동작을 변경하지 않는다.

## 구현 순서

기능과 구조를 한 번에 크게 바꾸지 않고 다음 순서로 진행한다.

### 1단계 — 정적 레이아웃과 Shape

1. LEVEL과 EXP 통합
2. 왼쪽 패널 공백 축소
3. Upgrade CTA를 성장 HUD 가까이 이동
4. Soul UI 압축
5. Speed HUD를 중앙 게임 영역으로 이동
6. 여신 Portrait와 말풍선 구조 변경
7. Border Hierarchy와 Semantic Color 적용

### 2단계 — 환생 UI 연결

1. 환생 버튼을 영혼 UI와 시각적으로 그룹화
2. 환생 가능 상태 표시
3. 기존 환생 Panel과 축복 선택 기능 회귀 검증

### 3단계 — UI Feedback

1. EXP Bar Tween
2. Level Up Pop
3. Soul 숫자 Pop
4. Upgrade Available 강조
5. Speed HUD 반응

### 4단계 — 별도 승인 항목

1. 중앙 Viewport 비율 변경
2. 실제 여신 반응 시스템
3. 3D Toon 스타일 및 월드 이펙트

## 반드시 유지할 기존 계약

다음 동작은 UI 개편 중 변경하지 않는다.

- `GameUIController.IsUpgradePanelOpen`
- `GameUIController.Initialize(...)`
- `GameUIController.SetViewport(...)`
- `RebirthUIController.IsPanelOpen`
- `RebirthUIController.Initialize(...)`
- `RebirthUIController.SetViewport(...)`
- 업그레이드 Panel을 열 때 조이스틱 입력 차단
- Panel을 닫을 때 입력 복원
- 환생 후보 선택 전에는 환생 Panel을 닫을 수 없는 규칙
- 업그레이드 또는 환생 Panel이 열렸을 때 게임 업데이트 정지
- `PlayerState.StateChanged` 기반 HUD 갱신
- `TruckUpgradeSystem`을 통한 업그레이드 명령
- `RebirthSystem`을 통한 환생 명령
- 현재 EXP, 영혼, 속도와 업그레이드 계산 방식
- 카메라 추적, 줌 및 트럭 조작 방식

## 검증 항목

각 단계 작업 후 다음을 확인한다.

1. Main 씬이 컴파일 오류 없이 열린다.
2. `SeventhStageSetup`을 다시 실행해도 새 UI 구조가 유지된다.
3. 레벨, EXP, 영혼과 포인트가 상태 변경 시 갱신된다.
4. EXP Bar가 타원형으로 돌아가지 않고 가로형 Bar로 표시된다.
5. 속도가 중앙 HUD에서 km/h로 갱신된다.
6. 업그레이드 버튼과 Panel이 기존처럼 동작한다.
7. 업그레이드 Panel이 열리면 조이스틱과 키보드 입력이 차단된다.
8. 환생 버튼, 환생 단계와 축복 후보 선택이 기존처럼 동작한다.
9. 환생 후보가 존재할 때 Panel을 닫을 수 없다.
10. 1920x1080과 1280x720에서 UI가 겹치지 않는다.
11. 좁은 화면에서 핵심 HUD 접근 방법이 유지된다.
12. `SeventhStageSetup.Verify()`와 `RebirthFeatureSetup.Verify()`가 통과한다.
13. 게임 밸런스, 카메라 추적, 몬스터와 트럭 동작이 변경되지 않는다.

## 최종 구현 원칙

이 UI 개편의 목표는 기존 HUD에 장식을 더 많이 붙이는 것이 아니다.

```text
게임 화면의 비중 확대
+ 정보별로 다른 Shape Language
+ 성장 흐름이 연결된 배치
+ 필요한 순간에만 강한 Feedback
= 게임플레이를 보조하는 카툰 HUD
```

현재 정상 동작하는 게임 시스템은 유지하고, 시각 구조와 UI 피드백을 작은 단계로 나누어 적용한다.

## 지정 색상 팔레트

UI를 다시 생성하거나 수정할 때 다음 색상 역할을 유지한다.

- Entry 씬 배경: `#FCCE7E`
- Main 씬 좌우 패널: `#A78C9B`
- Growth/Level/EXP: `#FC7EC6` 계열
- Upgrade CTA: `#FCCE7E` 계열
- Soul/Rebirth: `#647B7D` 계열
- Speed HUD: `#7EF1FC` 계열

파생 색상은 테두리, 게이지 트랙, 눌림 상태처럼 명도 구분이 필요한 곳에만 사용한다. 레이아웃, 폰트, 기능 연결은 색상 적용 과정에서 변경하지 않는다.

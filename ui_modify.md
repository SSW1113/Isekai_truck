# Main 씬 게임 UI 3단 레이아웃 개편

## 작업 목표

현재 Unity 프로젝트의 `Main` 씬 UI를 기존 상단 HUD 중심 구조에서 다음과 같은 3영역 구조로 개편한다.

```text
왼쪽 정보 패널 | 중앙 게임 플레이 영역 | 오른쪽 정보 패널
```

첨부한 1번 이미지는 현재 UI, 2번 이미지는 목표 레이아웃 참고용이다.

2번 이미지의 구조만 참고하고, 좌우 패널은 연보라색이 아닌 무채색 회색 계열로 구성한다. 전체 디자인은 화려한 완성형 UI보다 깔끔한 프로토타입 스타일을 우선한다.

## 현재 프로젝트 구조

작업 전에 다음 파일과 연결 관계를 확인하고 기존 기능을 보존한다.

- `Assets/IsekaiTruck/Scenes/Main.unity`
- `Assets/IsekaiTruck/Scripts/UI/GameUIController.cs`
- `Assets/IsekaiTruck/Editor/SeventhStageSetup.cs`
- `Assets/IsekaiTruck/Scripts/Core/GameManager.cs`
- `Assets/IsekaiTruck/Scripts/Camera/CameraController.cs`
- `Assets/IsekaiTruck/Scripts/Truck/TruckController.cs`
- `Assets/IsekaiTruck/Scripts/Player/PlayerState.cs`

현재 UI는 `SeventhStageSetup.CreateUI()`에서 생성되므로, `Main.unity`만 수정하지 말고 `SeventhStageSetup.cs`의 UI 생성 코드도 동일한 구조로 변경한다. 이후 Setup 메뉴를 다시 실행해도 기존 UI로 돌아가지 않아야 한다.

현재 프로젝트는 `UnityEngine.UI.Text`, `Image`, `Button`을 사용한다. 이번 작업을 위해 UI 전체를 TextMeshPro로 전환하거나 새 패키지를 설치하지 않는다.

## 핵심 레이아웃

`Game Canvas > Game UI` 아래를 다음 구조로 재구성한다.

```text
Game UI
├─ Left Side Panel
│  ├─ Level Section
│  ├─ EXP Section
│  ├─ Fuel Reserved Area
│  └─ Upgrade Section
├─ Game Area UI
│  ├─ 중앙 플레이 HUD
│  └─ Upgrade Panel
└─ Right Side Panel
   ├─ Goddess Interaction Area
   │  ├─ Goddess Silhouette
   │  └─ Goddess Message
   ├─ Soul Section
   └─ Speed Section
```

중앙 `Game Area UI`는 기존 카메라의 실제 게임 뷰 영역과 정확히 일치시킨다.

현재 `CameraController`는 `GameConfig.Camera.ViewportAspect`의 `10:16` 비율을 기준으로 `ViewportRect`를 계산한다. 이 구조를 변경하지 말고 기존 `GameUIController.SetViewport()`를 확장하여 다음처럼 배치한다.

- 중앙 영역: `viewport.xMin ~ viewport.xMax`
- 왼쪽 패널: 화면 왼쪽 끝부터 `viewport.xMin`
- 오른쪽 패널: `viewport.xMax`부터 화면 오른쪽 끝
- 좌우 패널 높이: Canvas 전체 높이

카메라 FOV, 추적 방식, 줌, 월드 표시 범위 및 `10:16` 플레이 비율은 변경하지 않는다.

가로 화면에서는 기존 세로형 게임 뷰 양옆의 여백이 좌우 패널로 사용되어야 한다. 좁은 화면에서는 UI가 겹치거나 중앙 플레이 영역을 덮지 않도록 최소한의 축소 또는 숨김 처리를 적용한다.

## 왼쪽 패널

왼쪽 패널 배경은 어두운 회색 계열의 불투명 패널로 구성한다.

예시 색상:

```csharp
new Color(0.18f, 0.19f, 0.21f, 1f)
```

내부 정보 카드는 패널 배경과 구분되는 조금 밝은 회색을 사용한다. 보라색 계열은 사용하지 않는다.

상단에는 레벨 표시 영역을 배치한다.

```text
레벨
Lv. 8
```

현재 `GameUIController.levelText`와 `PlayerState` 갱신 구조를 그대로 사용하여 실제 플레이어 레벨이 표시되도록 한다. 고정된 임시 레벨을 표시하지 않는다.

레벨 아래에는 기존 EXP 정보를 이동한다.

```text
EXP
현재 EXP / 필요 EXP
[EXP 게이지]
```

기존 `expText`, `expFill` 참조와 갱신 기능을 유지한다.

향후 연료 UI를 추가할 수 있도록 `Fuel Reserved Area`라는 이름의 빈 `RectTransform` 영역을 확보한다. 아직 연료 시스템이나 가짜 연료 수치를 구현하지 않는다.

기존 업그레이드 포인트와 업그레이드 버튼은 삭제하지 않는다. 왼쪽 패널 하단 또는 레이아웃상 자연스러운 위치로 이동한다.

```text
포인트 0
[업그레이드]
```

기존 업그레이드 창 열기, 닫기, 속도 업그레이드, 크기 업그레이드 기능은 모두 그대로 동작해야 한다.

## 중앙 게임 플레이 영역

중앙 영역에는 기존 게임 카메라 화면이 그대로 보여야 한다.

다음 동작을 변경하지 않는다.

- 트럭 조작
- 조이스틱 입력 범위
- 카메라 추적 및 줌
- 몬스터 표시와 스폰
- 월드 타일과 Fog
- 업그레이드 팝업
- 환생 및 축복 UI

기존 전체 너비 상단 바인 `Player HUD`는 제거하거나 재구성한다. 레벨, EXP, 영혼, 포인트를 기존처럼 중앙 게임 화면 상단에 가로로 나열하지 않는다.

중앙 플레이 영역 위에는 불필요한 불투명 패널을 덮지 않는다.

`Upgrade Panel`은 기존과 동일하게 중앙 `Game Area UI` 안에서 열리도록 유지한다. 업그레이드 창이 열릴 때 조이스틱 입력이 비활성화되고, 닫으면 복원되는 동작도 보존한다.

## 오른쪽 패널

오른쪽 패널도 왼쪽과 동일한 회색 계열을 사용한다.

상단에는 `Goddess Interaction Area`를 만든다. 실제 여신 시스템이나 상호작용 로직은 구현하지 않고, 향후 기능을 연결할 수 있는 독립된 UI 컨테이너로 구성한다.

### 여신 임시 실루엣

외부 이미지나 새 에셋을 추가하지 않는다.

여러 개의 검은색 `Image` UI 요소를 조합해 머리, 몸통, 팔 정도만 표현한 단순한 인간형 실루엣을 만든다. 지나치게 디테일하게 만들 필요는 없다.

오브젝트 구조와 이름은 추후 실제 여신 이미지로 교체하기 쉽게 구성한다.

```text
Goddess Interaction Area
├─ Goddess Silhouette
│  ├─ Head
│  ├─ Body
│  ├─ Left Arm
│  └─ Right Arm
└─ Goddess Message
```

여신 영역 하단에는 향후 대사나 상호작용 문구가 들어갈 `Goddess Message` 텍스트 영역을 확보한다. 현재는 다음과 같은 짧은 임시 문구를 사용할 수 있다.

```text
여신이 지켜보고 있습니다
```

실제 여신 표정 시스템, 대화 시스템, 클릭 이벤트는 미리 구현하지 않는다.

### 영혼 표시

여신 영역 아래에는 영혼 정보를 별도의 카드로 표시한다.

```text
영혼
283
```

기존 `GameUIController.soulText`와 `PlayerState.Soul` 갱신 구조를 그대로 연결한다. 고정된 임시 숫자를 표시하지 않는다.

### 현재 속도 표시

영혼 아래에는 현재 트럭 속도를 표시한다.

```text
속도
12 km/h
```

`TruckController.CurrentSpeedPerSecond`를 읽어 표시한다. Unity 월드 단위를 미터로 가정하여 표시값만 다음과 같이 변환한다.

```csharp
displaySpeedKmh = truckController.CurrentSpeedPerSecond * 3.6f;
```

이 변환은 UI 표시 전용이다. 실제 트럭 속도, 가속도, 마찰 또는 게임 밸런스 값은 변경하지 않는다.

속도 텍스트는 매 프레임 불필요한 문자열을 생성하지 않도록 반올림된 표시값이 바뀔 때만 갱신하거나 낮은 주기로 갱신한다.

`GameUIController`에 `speedText` 참조를 추가하고 `SetReferences()`와 `SeventhStageSetup.Verify()`에도 연결 검증을 추가한다.

## 반응형 배치

앵커와 레이아웃 컴포넌트를 사용해 해상도 변화에 대응한다.

- 좌우 패널은 `RectTransform` 앵커로 카메라 Viewport 바깥 영역에 맞춘다.
- 내부 세로 배치는 필요한 경우 `VerticalLayoutGroup`을 사용한다.
- 고정 픽셀 좌표만으로 전체 UI를 구성하지 않는다.
- 텍스트가 좁은 패널 밖으로 넘치지 않도록 정렬과 크기를 조절한다.
- 중앙 게임 화면의 Viewport를 좌우 UI가 덮지 않아야 한다.
- 우선 검증 해상도는 `1920x1080`, `1280x720`로 한다.
- 기존 `CanvasScaler` 설정은 다른 UI에 영향을 줄 수 있으므로 특별한 이유 없이 변경하지 않는다.

## 기존 기능 보존

다음 기존 참조와 기능을 삭제하지 않는다.

- `levelText`
- `expText`
- `expFill`
- `soulText`
- `pointText`
- `upgradePointText`
- `speedLevelText`
- `sizeLevelText`
- `speedStatText`
- `sizeStatText`
- `openButton`
- `closeButton`
- `speedButton`
- `sizeButton`
- `upgradePanel`
- `gameArea`

환생 UI와 축복 UI는 별도 시스템이므로 이번 작업 범위에서 구조를 변경하지 않는다.

UI 개편을 이유로 `GameManager`, `PlayerState`, `TruckController`, `CameraController`의 핵심 동작을 리팩터링하지 않는다.

## 검증

작업 후 다음 사항을 확인한다.

1. Main 씬이 컴파일 오류 없이 열린다.
2. 가로 화면에서 회색 왼쪽 패널, 중앙 게임 화면, 회색 오른쪽 패널이 구분된다.
3. 레벨과 EXP가 플레이어 상태 변경 시 갱신된다.
4. 몬스터 처치 후 영혼 수치가 갱신된다.
5. 트럭 이동 중 현재 속도가 `km/h`로 갱신된다.
6. 업그레이드 버튼과 팝업이 기존처럼 동작한다.
7. 업그레이드 팝업이 열리면 조이스틱 입력이 차단되고 닫으면 복원된다.
8. 환생 및 축복 UI가 기존처럼 중앙 게임 영역에서 동작한다.
9. `SeventhStageSetup.Verify()`가 새 UI 참조까지 검사하고 통과한다.
10. `SeventhStageSetup`을 다시 실행해도 새 3단 UI 구조가 유지된다.
11. 좌우 패널에 보라색이 사용되지 않았다.
12. 게임 밸런스와 카메라 동작이 변경되지 않았다.

## 작업 후 보고

다음 형식으로 결과를 요약한다.

```text
변경한 파일
새 UI 계층 구조
왼쪽 패널에 배치한 정보
오른쪽 패널에 배치한 정보
속도 표시 연결 방식
기존 시스템과 연결한 방식
기존 동작 중 유지한 부분
검증 결과
추가로 확인할 사항
```

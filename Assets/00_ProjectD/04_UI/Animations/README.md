# UI 애니메이션 편집

- 배낭 카드: InventoryCard.controller. Item appearance 레이어에서 선택/hover/press 색과 위치를, Result emphasis 레이어에서 결과 강조를 수정한다. 36칸이 공유한다.
- 퀵슬롯: QuickSlot.controller. 동일한 구조로 8칸이 공유한다.
- 장비 슬롯: Part0_Equipment ~ Part8_Equipment.controller. Floating 레이어는 Art의 RectTransform을 직접 애니메이션한다. Hover/Press/Selection/Result emphasis는 개별 셰이더에 전달할 값을 애니메이션한다.
- 패널: 각 *_Panel.controller의 Open/Close/Refresh 상태. OnOpened/OnClosed 이벤트는 입력 활성화와 종료 완료에 사용하므로 유지한다.
- 제작 결과: CraftResult 컨트롤러의 Open/Close 클립. 배경, 제목, 아이템, 확인 버튼의 순서는 클립에서 조절한다.
- 로그인/스플래시/씬 이동: LoginPrompt, Splash, SceneTransition 컨트롤러. SplashSequence의 OnSplashFinished와 SceneCover/SceneReveal의 OnFadeFinished 이벤트를 유지한다.
- 게이지: Fill_Gauge 컨트롤러의 ValueChange/Warning/Result emphasis. 실제 수치는 C#이 제공하고 클립이 보간 진행률과 강조를 제어한다.
- 모든 UI Animator는 UnscaledTime으로 재생한다. 게임 시간 정지 중에도 동작한다.
- C#에는 데이터 바인딩, 입력/상태 전달, 비동기 씬 로드, 셰이더의 개별 머티리얼 및 실시간 시계, 휠 스크롤 목표값 계산을 둔다.
- Editor/UiAnimationMigration*은 기존 UI를 이전한 일회성 제작 도구다. 수정은 생성된 씬/프리팹/클립/컨트롤러에서 진행하며 마이그레이션을 재실행하지 않는다.

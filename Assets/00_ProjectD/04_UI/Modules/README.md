# 공용 uGUI 에셋

현재 UI는 씬에 연결된 프리팹, Sprite, Material 및 Animator 에셋을 사용한다.

- `ConfirmButton.prefab`, `CancelButton.prefab`, `DeleteButton.prefab`: 공용 액션 버튼.
- `CraftResult.prefab`: 제작 결과 UI 원본.
- `SortDropdown.prefab`: 정렬 선택 UI 원본.
- `WorldDebugPanel.prefab`: 월드 디버그 UI.
- 버튼 배경과 아이콘, TMP 텍스트는 분리되어 있다. 배경은 기존 Sprite border와 Image의 Sliced 설정을 유지한다.
- `UiModuleButton`은 `SetIcon`과 `SetLabel`만 제공한다. 시각 상태는 프리팹의 기존 UI 컴포넌트와 Animator에서 편집한다.

## 2026-10-26 미사용 에셋 정리

현재 Assets 의존성과 직렬화 GUID 참조를 확인하여 초기 `Panel.prefab`, `ItemSlot.prefab`, 코드에서 사용하지 않는 `WhiteTheme.asset` 등 11개를 삭제했다. 구버전의 `SetTheme` / `Apply Theme` 설명은 더 이상 유효하지 않다.

삭제 목록: `Tools/UIReview/UiCleanupDeleted.txt`
복구용 원본 및 meta: `Tools/UIReview/Archive/UnusedUI-20261026-002641.zip`

사용 중인 에셋 의존성은 삭제 전후 동일하다. 씬과 프리팹의 레이아웃은 수정하지 않았다.

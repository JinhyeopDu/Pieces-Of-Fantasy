# Inventory & Reward System

## 1. 개요

PoF의 인벤토리 / 보상 시스템은  
탐험, 전투, 아이템 사용, 성장 시스템을 연결하는 핵심 구조입니다.

이 시스템의 목표는 다음과 같습니다.

- 전투 보상을 자연스럽게 탐험으로 연결
- 아이템 획득 / 보유 / 사용 흐름 통합
- 데이터 기반 아이템 관리
- UI를 통한 직관적인 피드백 제공

---

## 2. 인벤토리 구조

인벤토리는 `GameContext`를 기준으로 관리됩니다.

### 핵심 구조
- `InventoryRuntime`
- `ItemStack`
- `ItemData`

### 역할

#### ItemData
- 아이템 ID
- 이름
- 아이콘
- 설명
- 타입
- 최대 스택 수

#### ItemStack
- 어떤 아이템인지
- 현재 몇 개를 가지고 있는지

#### InventoryRuntime
- 현재 인벤토리에 들어 있는 스택 목록

👉 정적 데이터와 실제 보유 상태를 분리한 구조입니다.

---

## 3. 보상 획득 흐름

전투 종료 후 보상은 즉시 인벤토리에 반영되며,  
동시에 Reward Queue를 통해 UI 피드백으로 연결됩니다.

### 흐름

1. 전투 종료
2. `DropTable`에서 드랍 결과 생성
3. `GameContext.AddItem()`으로 인벤토리에 반영
4. `GameContext.QueueReward()`로 보상 큐 등록
5. Exploration 복귀 후 Reward Toast UI 출력

👉 보상 데이터와 보상 표시를 분리하여 설계했습니다.

---

## 4. DropTable 구조

드랍은 `DropTable` 기반으로 처리합니다.

### 특징
- 적마다 DropTable 설정 가능
- 확률 기반 드랍
- 수량 기반 드랍 가능
- 전투 결과와 자연스럽게 연결

### 설계 의도
- 적 데이터에서 보상 정책을 직접 정의할 수 있게 구성
- 전투 로직과 드랍 정책을 분리
- 향후 드랍 확장에 유리한 구조 확보

---

## 5. 아이템 획득 처리

아이템 획득은 `GameContext`를 통해 처리됩니다.

### 처리 내용
- 기존 스택 검색
- 최대 스택 수 고려
- 필요 시 새 스택 생성
- 인벤토리 변경 이벤트 발생
- 보상 큐 등록 가능

### 장점
- 모든 아이템 흐름을 한 곳에서 관리 가능
- UI / 탐험 / 성장 시스템이 같은 인벤토리를 참조 가능
- 저장 / 로드와 연결이 쉬움

---

## 6. 아이템 사용 시스템

인벤토리의 아이템은 실제 캐릭터 상태에 영향을 줄 수 있습니다.

### 예시
- HP 회복 아이템
- 버프 아이템
- 성장 재료
- 조건 해제용 키 아이템

### 흐름
1. 인벤토리 UI에서 아이템 선택
2. 사용 대상 캐릭터 선택
3. 효과 적용
4. 수량 차감
5. UI 갱신

---

## 7. UI 구조

### 인벤토리 UI
- 아이템 목록 표시
- 카테고리 탭
- 정렬 기능
- 상세 정보 패널
- 사용 팝업

### 보상 UI
- 획득 아이템 토스트
- 수량 표시
- 탐험 복귀 후 순차 출력

### 관련 특징
- 아이템 사용과 보상 획득의 피드백이 분리되어 있음
- 단순 데이터 변경이 아니라 UI 반응까지 포함한 구조

---

## 8. 관련 핵심 스크립트

### Core
- `Assets/Game/02.Scripts/Core/GameContext.cs`

### Data
- `Assets/Game/02.Scripts/Core/Data/ItemData.cs`
- `Assets/Game/02.Scripts/Core/Data/DropTable.cs`

### Inventory UI
- `Assets/Game/02.Scripts/UI/Inventory/InventoryController.cs`
- `Assets/Game/02.Scripts/UI/Inventory/InventoryView.cs`
- `Assets/Game/02.Scripts/UI/Inventory/ItemSlotView.cs`
- `Assets/Game/02.Scripts/UI/Inventory/ItemUsePopupController.cs`
- `Assets/Game/02.Scripts/UI/Inventory/DetailPanelView.cs`

### Reward UI
- `Assets/Game/02.Scripts/UI/ExplorationRewardToastController.cs`
- `Assets/Game/02.Scripts/UI/RewardToastLine.cs`

---

## 9. 설계 의도

### 1) Game Loop 연결
전투 보상이 단순 수치 증가로 끝나지 않고,  
인벤토리와 성장 시스템으로 이어지도록 설계했습니다.

### 2) SSOT 유지
아이템 상태는 GameContext 기준으로 관리하여  
탐험, 전투, UI, 저장/로드가 같은 데이터를 보도록 구성했습니다.

### 3) 데이터 중심 구조
아이템 자체 설명은 ItemData,  
실제 보유 상태는 InventoryRuntime / ItemStack으로 분리했습니다.

### 4) 사용자 피드백 강화
보상 큐와 토스트 UI를 통해  
획득 결과를 즉시 체감할 수 있도록 설계했습니다.
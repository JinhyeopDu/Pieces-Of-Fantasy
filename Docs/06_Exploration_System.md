# Exploration System

## 1. 개요

PoF의 탐험 시스템은  
플레이어 이동, 오브젝트 상호작용, 전투 진입을 담당하는 핵심 시스템입니다.

탐험은 단순 이동이 아니라 다음 루프의 시작점입니다.

탐험 → 전투 → 보상 → 성장 → 탐험

---

## 2. 플레이어 구조

플레이어는 CharacterData 기반 프리팹으로 구성됩니다.

### 구성 요소
- CharacterController 기반 이동
- CharacterData 연결
- CharacterRuntime 생성
- 카메라 추적

### 특징
- 캐릭터 변경 시 동일 구조 유지
- 탐험 상태와 전투 상태 분리

### 관련 스크립트
- `PlayerControllerHumanoid.cs`
- `ExplorationPartySwitcher.cs`

---

## 3. 이동 시스템

### 이동 방식
- Input System 기반 이동 처리
- CharacterController를 사용한 물리 이동
- 카메라 방향 기준 이동

### 특징
- 카메라 중심 이동 방식
- 자연스러운 방향 전환
- 애니메이션과 이동 연동

---

## 4. 파티 시스템

탐험에서는 여러 캐릭터 중 하나를 직접 조작할 수 있습니다.

### 기능
- 파티 캐릭터 전환
- 현재 캐릭터 스폰
- 기존 캐릭터 상태 유지

### 흐름
1. 선택된 캐릭터 ID 확인
2. 기존 캐릭터 제거
3. 새 캐릭터 프리팹 생성
4. CharacterRuntime 연결

---

## 5. 상호작용 시스템

탐험에서는 다양한 오브젝트와 상호작용할 수 있습니다.

### 대상
- 몬스터 (전투 진입)
- 채집 오브젝트
- 이벤트 오브젝트

---

### Interact 구조

#### 핵심 방식
- `IInteractable` 인터페이스 사용
- 플레이어 주변 탐지 (Sensor)
- UI 선택 리스트 표시
- 선택 후 상호작용 실행

---

### Gather 시스템

채집 가능한 오브젝트는 다음과 같이 처리됩니다.

- 채집 리스트 UI 표시
- 선택된 대상 상호작용
- DropTable 기반 아이템 획득
- Respawn 시스템 연결

### 관련 스크립트
- `GatherListUIController.cs`
- `InteractSensor.cs`

---

## 6. 전투 진입

탐험 중 몬스터와 상호작용하면 전투로 전환됩니다.

### 흐름

1. 몬스터 접촉 / 상호작용
2. `BattleStarter` 실행
3. Encounter 데이터 설정
4. GameContext에 전투 정보 저장
5. Battle Scene 로드

---

### 특징
- 탐험 상태 유지
- 전투 종료 후 동일 위치 복귀 가능
- Respawn 시스템과 연결

---

## 7. Secret Art 시스템

탐험 중 특정 조건에서 Secret Art를 준비할 수 있습니다.

### 구조
- CharacterRuntime에 상태 저장
- 전투 시작 시 1회 적용
- 적용 후 초기화

### 역할
- 전투 시작 버프
- 전략적 준비 요소

---

## 8. Respawn 시스템

몬스터 및 채집 오브젝트는 Respawn 구조를 가집니다.

### 기능
- 고유 spawnId 관리
- 처치/채집 여부 기록
- 일정 시간 후 재생성

### 특징
- 월드 상태 유지
- 저장/로드와 연동 가능

---

## 9. UI 시스템

탐험 UI는 플레이어 행동을 보조합니다.

### 구성
- 상호작용 리스트 UI
- 파티 전환 UI
- 미니맵
- 핫키 시스템

---

## 10. 관련 핵심 스크립트

### Player
- `PlayerControllerHumanoid.cs`
- `ExplorationPartySwitcher.cs`

### Interaction
- `IInteractable.cs`
- `InteractSensor.cs`

### Gather
- `GatherListUIController.cs`

### Battle Entry
- `BattleStarter.cs`

### UI
- `ExplorationUIHotkeys.cs`
- `MiniMapController.cs`

---

## 11. 설계 의도

### 1) 게임 루프 시작점
탐험은 모든 시스템의 시작점이며,  
전투 및 성장 시스템과 자연스럽게 연결되도록 설계했습니다.

---

### 2) 시스템 분리
탐험과 전투를 완전히 분리하여  
각 시스템의 책임을 명확히 했습니다.

---

### 3) 확장성
새로운 상호작용 오브젝트를 추가할 때  
IInteractable만 구현하면 쉽게 확장 가능하도록 설계했습니다.

---

### 4) 상태 유지
Respawn 및 GameContext를 통해  
월드 상태가 유지되도록 구성했습니다.

## 🔍 Problem

- 퀘스트 진행 상태 초기화 버그

## 💡 Solution

- GameContext + SaveData 연동

## 🎯 Result

- 진행 상태 유지

## 💡 Why this matters

이 구조를 통해
- 데이터 안정성 확보
- 플레이 연속성 유지
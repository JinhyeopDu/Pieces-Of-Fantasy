# Core Architecture

## 1. 설계 핵심 개념 (SSOT)

이 프로젝트는 Single Source of Truth 구조를 기반으로 설계되었습니다.

모든 게임 상태는 GameContext에서 관리됩니다.

### GameContext 역할
- 파티 상태 관리 (CharacterRuntime)
- 인벤토리 관리
- 전투 SP 관리
- Secret Art 포인트 관리
- 보상 큐 관리
- 저장 / 로드 데이터

👉 모든 시스템은 GameContext를 기준으로 동작합니다.

---

## 2. 데이터 구조 분리

### CharacterData (ScriptableObject)
- 캐릭터 기본 정보
- 기본 스탯
- 스킬 데이터

### CharacterRuntime
- 현재 HP
- 레벨
- 버프 상태
- 전투 중 상태

👉 Data / Runtime 분리를 통해 유지보수성과 확장성을 확보했습니다.

---

## 3. 주요 흐름

탐험 → 전투 → 보상 → 성장 → 탐험

### 흐름 설명

1. Exploration에서 몬스터 접촉
2. BattleStarter → BattleScene 진입
3. BattleController에서 전투 진행
4. 전투 종료 후 DropTable → 보상 생성
5. GameContext에 보상 저장
6. Exploration 복귀 후 보상 UI 출력

---

## 4. 주요 스크립트 구조

### Core
- `GameContext.cs`
- `SaveManager.cs`
- `GameDataRegistry.cs`

### Battle
- `BattleController.cs`
- `SkillSelectUI.cs`

### Exploration
- `PlayerControllerHumanoid.cs`
- `BattleStarter.cs`

### UI
- `CharacterScreenController.cs`
- `InventoryUIController.cs`

---

## 5. 설계 의도

이 구조는 다음을 목표로 설계되었습니다:

- 데이터 중심 구조
- 시스템 간 결합도 최소화
- 유지보수 용이성
- 확장 가능한 구조

특히 GameContext를 중심으로 모든 상태를 관리함으로써  
데이터 흐름을 명확하게 유지하도록 했습니다.


## 🔍 Problem

- 고정 턴 방식은 전략성이 낮음

## 💡 Solution

- SPD 기반 턴 큐 도입

## 🎯 Result

- 캐릭터 속도에 따른 전략 변화
- 턴 흐름 다양성 증가

## 💡 Why this matters

이 구조를 통해
- 전투 전략성 강화
- 플레이 다양성 확보
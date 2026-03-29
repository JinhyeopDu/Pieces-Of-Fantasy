# Quest System

## 1. 개요

PoF의 퀘스트 시스템은  
플레이어의 탐험, 전투, 아이템 수집 진행을 추적하고  
보상 및 스토리 진행과 연결하는 역할을 담당합니다.

이 시스템의 목표는 다음과 같습니다.

- 진행 상태를 명확하게 저장
- 탐험 / 전투 / 수집과 자연스럽게 연결
- 이어하기(Continue)에서도 상태가 유지되도록 구성
- 퀘스트 완료/보상 수령을 명확하게 구분

---

## 2. 핵심 구조

퀘스트 시스템은 다음 요소로 구성됩니다.

### QuestData
퀘스트의 정적 데이터

- 퀘스트 ID
- 제목
- 설명
- 목표 타입
- 목표 수치
- 보상 정보

### QuestRuntimeProgress
실제 플레이 중 변하는 퀘스트 진행 상태

- 현재 진행 수치
- 완료 여부
- 보상 수령 여부

### QuestManager
퀘스트 진행의 중심 관리자

- 현재 퀘스트 관리
- 진행 수치 증가
- 완료 판정
- 보상 지급
- 자동 완료 조건 갱신

---

## 3. 진행 방식

퀘스트는 현재 활성 퀘스트를 기준으로 진행됩니다.

### 예시
- 특정 아이템 수집
- 특정 적 처치
- 특정 목표 수치 달성

### 흐름
1. 현재 활성 퀘스트 확인
2. 플레이어 행동 발생
3. QuestManager에 알림
4. 조건 만족 여부 검사
5. 완료 상태 갱신
6. 보상 수령 가능 상태로 전환

---

## 4. 수집 / 처치 / 탐험과의 연결

퀘스트는 게임의 여러 시스템과 연결됩니다.

### 수집
- 채집 또는 아이템 획득 시 진행도 증가 가능

### 전투
- 적 처치 시 퀘스트 목표 증가 가능

### 탐험
- 특정 목표 지역 진입, 상호작용 등으로도 확장 가능

---

## 5. 저장 / 불러오기 구조

퀘스트 시스템에서 가장 중요하게 본 부분은  
이어하기 시 진행 상태가 정확히 복원되는 구조였습니다.

### 저장 항목
- currentQuestId
- currentQuestValue
- currentQuestCompleted
- currentQuestRewardClaimed
- completedQuestIds

### 저장 위치
- `SaveData`
- `GameContext`

---

## 6. Continue 시 복원 흐름

이어하기를 누르면 다음 순서로 진행됩니다.

1. SaveManager가 SaveData 로드
2. GameContext가 퀘스트 관련 필드 복원
3. currentQuestId 기준으로 QuestData 조회
4. QuestRuntimeProgress 재구성
5. QuestManager가 현재 상태를 다시 사용 가능하게 연결

### 핵심 포인트
정적 데이터(QuestData)와  
동적 상태(QuestRuntimeProgress)를 분리해서 복원해야  
이어하기에서 진행이 끊기지 않습니다.

---

## 7. 구현 중 중요하게 본 문제

이 프로젝트에서는 퀘스트 관련 저장/복원 문제가 실제로 중요했습니다.

### 대표 문제
- 이어하기 후 퀘스트 수치가 초기화되는 문제
- 진행 중인 퀘스트가 처음 퀘스트로 되돌아가는 문제
- 퀘스트 데이터 연결 실패 시 현재 상태가 복구되지 않는 문제

---

## 8. 해결 방향

이 문제를 해결하기 위해 다음 구조를 강화했습니다.

### 1) Quest ID 기반 복원
현재 진행 중인 퀘스트를  
인덱스가 아니라 `questId` 기준으로 다시 찾도록 구성

### 2) 진행 수치 별도 저장
퀘스트 데이터와 별개로  
현재 진행 수치를 저장하도록 분리

### 3) 완료 / 보상 상태 분리
퀘스트 완료와 보상 수령을 별도 상태로 관리하여  
완료 후 UI/보상 흐름이 꼬이지 않도록 구성

### 4) 복원 실패 감지
QuestData를 찾지 못했을 때  
그 상태를 감지하고 잘못된 Continue를 막을 수 있게 설계

---

## 9. 관련 핵심 스크립트

### Data
- `Assets/Game/02.Scripts/Quest/QuestData.cs`
- `Assets/Game/02.Scripts/Quest/QuestObjectiveType.cs`

### Runtime / Manager
- `Assets/Game/02.Scripts/Quest/QuestRuntimeProgress.cs`
- `Assets/Game/02.Scripts/Quest/QuestManager.cs`

### Save / Load 연결
- `Assets/Game/02.Scripts/Core/GameContext.cs`
- `Assets/Game/02.Scripts/Core/SaveData.cs`
- `Assets/Game/02.Scripts/Core/SaveManager.cs`
- `Assets/Game/02.Scripts/Core/GameDataRegistry.cs`
- `Assets/Game/02.Scripts/Core/TitleNewGame.cs`

---

## 10. 설계 의도

### 1) 데이터와 상태 분리
QuestData는 설계 데이터,
QuestRuntimeProgress는 실제 진행 상태로 분리했습니다.

### 2) 저장 안정성 확보
이어하기 시 인덱스가 아니라 questId 기반으로 복원하여  
데이터 변경에도 비교적 안정적으로 대응하도록 했습니다.

### 3) 시스템 연결성 확보
전투, 아이템 획득, 탐험 상호작용과 연결해  
퀘스트가 게임 루프 전체와 맞물리도록 설계했습니다.

### 4) 문제 해결 중심 개선
실제 플레이 중 발견한 Continue 문제를 기준으로  
복원 구조를 보강하는 방향으로 개선했습니다.
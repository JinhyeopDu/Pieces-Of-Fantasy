# Battle System

## 1. 개요

PoF의 전투는 탐험 씬과 분리된 턴제 전투 구조로 설계했습니다.  
전투 진입 시 Exploration의 파티 상태를 기준으로 BattleActorRuntime을 구성하고,  
전투 종료 후 다시 GameContext에 결과를 반영하는 흐름입니다.

핵심 목표는 다음과 같습니다.

- SPD 기반 턴 순서
- Shared Skill Point 구조
- 전투 시작 시 Secret Art 적용
- 전투 종료 후 상태/보상 반영
- 탐험과 전투 사이의 데이터 일관성 유지

---

## 2. 전투 진입 흐름

### 흐름
1. 탐험 씬에서 `BattleStarter`가 전투 시작 조건을 검사
2. `GameContext`에 전투 payload 저장
3. Battle 씬으로 이동
4. `BattleController`가 전투 시작 초기화 수행
5. 아군 / 적 런타임 생성
6. 턴 큐 구성 후 전투 루프 시작

### 관련 스크립트
- `Assets/Game/02.Scripts/Exploration/BattleStarter.cs`
- `Assets/Game/02.Scripts/Battle/BattleController.cs`
- `Assets/Game/02.Scripts/Battle/TempBattlePayload.cs`
- `Assets/Game/02.Scripts/Core/GameContext.cs`

---

## 3. 턴 진행 구조

전투는 캐릭터와 적의 SPD를 기준으로 턴 순서를 구성합니다.

### 핵심 방식
- 전투 시작 시 아군과 적을 하나의 턴 큐로 합침
- `GetEffectiveSPD()` 기준으로 내림차순 정렬
- 정렬된 결과를 Queue로 사용
- 턴 종료 후 다시 큐가 비면 재구성

### 의도
단순 고정 순서가 아니라,  
속도 능력치가 실제 전투 흐름에 영향을 주도록 설계했습니다.

### 관련 스크립트
- `BattleController.cs`
- `BattleActorRuntime.cs`

---

## 4. 플레이어 턴

플레이어 턴에서는 행동 선택 UI를 통해  
기본 공격 또는 일반 스킬을 선택합니다.

### 동작 방식
- `SkillSelectUI` 표시
- 현재 선택 가능한 행동 출력
- 스킬 선택 시 SP 사용 가능 여부 확인
- 선택 완료 후 `BattleController`에서 실제 행동 실행

### 현재 정책
- 기본 공격: SP 회복
- 일반 스킬: Shared SP 사용
- 궁극기: 현재 범위에서는 제거/비활성화 상태

### 관련 스크립트
- `BattleController.cs`
- `SkillSelectUI.cs`
- `BattleHud.cs`

---

## 5. Shared Skill Point 시스템

이 프로젝트의 전투 SP는 캐릭터 개인 자원이 아니라  
파티 전체가 공유하는 자원으로 설계했습니다.

### 설계 이유
- 캐릭터 간 역할 분담 강화
- 턴 단위 선택의 전략성 강화
- 특정 캐릭터만 독점하지 않는 전투 리소스 구조

### 정책
- 전투 시작 시 초기 SP 설정
- 기본 공격 사용 시 SP 획득
- 일반 스킬 사용 시 SP 소모
- SP 상태는 HUD와 선택 UI에 반영

### 관련 스크립트
- `GameContext.cs`
- `BattleController.cs`
- `SkillSelectUI.cs`
- `SkillPointPipUI.cs`

---

## 6. Secret Art 적용

탐험 중 준비한 Secret Art는 전투 시작 시 1회 적용됩니다.

### 적용 방식
- `CharacterRuntime.secretArtReady` 상태 확인
- 전투 시작 시 `ApplySecretArtAtBattleStartOnce()` 호출
- 캐릭터별 Secret Art 타입에 따라 효과 적용
- 적용 후 ready 상태 해제

### 현재 구현된 효과 예시
- 파티 회복
- 파티 방어 버프
- 전투 시작 SP 증가

### 관련 스크립트
- `CharacterData.cs`
- `PlayerControllerHumanoid.cs`
- `BattleController.cs`
- `GameContext.cs`

---

## 7. 적 행동 처리

적 턴은 `EnemyTurnRoutine()`을 중심으로 동작합니다.

### 기본 구조
- 살아있는 아군 중 타겟 선택
- 적 타입/보스 여부에 따라 패턴 분기
- 애니메이션 이벤트 또는 fallback 타이밍에 맞춰 데미지 적용
- 턴 종료 후 다음 actor 진행

### 특징
- 일반 적 공격 처리
- 골렘 Throw 패턴
- 드래곤 Breath / Scream / Defend 패턴
- 카메라 연출과 공격 타이밍 분리

### 관련 스크립트
- `BattleController.cs`
- `EnemyAI.cs`
- `EnemyAttackEventRelay.cs`
- `GolemThrowEventRelay.cs`
- `DragonBreathEventRelay.cs`
- `BreathDamageZone.cs`

---

## 8. 전투 종료 처리

전투 종료 시 Battle 결과를 GameContext에 다시 반영합니다.

### 처리 내용
- 아군 HP / SP 동기화
- 전투 임시 버프 정리
- 드랍 생성 및 보상 큐 등록
- 퀘스트 적 처치 통보
- 탐험 씬 복귀

### 관련 스크립트
- `BattleController.cs`
- `GameContext.cs`
- `DropTable.cs`
- `QuestManager.cs`
- `SceneFader.cs`

---

## 9. 내가 신경 쓴 설계 포인트

### 1) SSOT 유지
전투 중에도 최종 결과는 GameContext를 기준으로 반영되도록 구성했습니다.

### 2) Runtime 분리
정적 데이터(CharacterData, EnemyData)와  
전투 중 상태(Runtime)를 분리해서 확장성을 확보했습니다.

### 3) 연출과 판정 분리
애니메이션과 실제 데미지 타이밍을 분리해  
전투 연출과 판정을 안정적으로 제어하도록 했습니다.

### 4) 탐험 ↔ 전투 연결
탐험 씬과 전투 씬이 완전히 분리되더라도  
파티 상태와 보상 흐름이 끊기지 않도록 설계했습니다.
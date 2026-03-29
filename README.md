# 🎮 Pieces of Fantasy (PoF)

> Unity 기반 턴제 RPG 프로젝트  
> (Honkai: Star Rail 스타일 탐험 → 전투 구조 구현)

---

## 📌 프로젝트 개요

**Pieces of Fantasy (PoF)**는  
탐험 → 전투 → 보상 → 성장 → 탐험으로 이어지는  
턴제 RPG 루프를 중심으로 설계된 프로젝트입니다.

이 프로젝트는 단순 기능 구현이 아니라  
👉 **데이터 흐름과 상태 일관성(SSOT)**을 중심으로 설계되었습니다.

---

## 🔁 핵심 게임 루프

Exploration → Battle → Reward → Growth → Exploration

- 탐험: 필드 이동, 상호작용, 몬스터 조우
- 전투: 턴제 전투 + SP 시스템
- 보상: 드랍 테이블 기반 아이템 획득
- 성장: 레벨 / 승급 / 스탯 강화

👉 이 루프가 끊기지 않도록 시스템을 설계했습니다.

---

## 🧠 설계 핵심: SSOT (Single Source of Truth)

모든 게임 상태는 하나의 중심 데이터에서 관리됩니다.

GameContext
 ├─ party (CharacterRuntime)
 ├─ inventory
 ├─ rewardQueue
 ├─ secretArtPoints
 └─ world state

👉 전투, 탐험, UI 모두 GameContext를 기준으로 동작

---

## ⚔️ 전투 시스템

PoF의 전투는 단순 턴제가 아니라  
👉 **연출 + 자원 관리 + 패턴**이 결합된 구조입니다.

### ✔ 턴 시스템
- SPD 기반 턴 큐
- 속도에 따라 행동 순서 결정

### ✔ Skill Point 시스템
- 기본 공격 → SP 획득
- 스킬 사용 → SP 소비
- 파티 전체 공유 자원

### ✔ 스킬 구조
- Basic Attack (SP 획득)
- Skill (SP 소비)
- Ultimate (현재 비활성)

### ✔ 보스 패턴 설계
- 드래곤: 방어 / 스크림 / 브레스
- 골렘: Throw (전체 공격)

👉 단순 수치 싸움이 아니라 패턴 기반 전투

---

## 📈 성장 시스템

### ✔ 레벨 구조
- 최대 레벨: 50
- 구간 캡: 10 / 20 / 30 / 40 / 50

### ✔ 승급 시스템
- stage 0~4
- 스탯 퍼센트 증가

### ✔ 스탯 계산 구조

최종 스탯 =
(기본 + 레벨 성장)
× 승급 배율
+ 영구 증가
+ 임시 버프

👉 전투 중 버프까지 포함된 최종 스탯 기반 계산

---

## 🧩 데이터 구조

PoF는 데이터와 상태를 명확히 분리했습니다.

### ✔ 정적 데이터 (설계용)
- CharacterData
- SkillData
- EnemyData
- ItemData
- EncounterData

### ✔ 런타임 데이터 (플레이 상태)
- CharacterRuntime
- BattleActorRuntime
- QuestRuntimeProgress

👉 ScriptableObject + Runtime 분리 구조

---

## 🗂️ 프로젝트 구조

Assets/Game
├── 01.Scenes
├── 02.Scripts
├── 03.ScriptableObjects
├── 04.Prefabs
├── 05.Art
├── 06.Addressables
├── 07.Audio

👉 유지보수를 고려한 구조 설계

---

## 🛠️ 기술적 특징

- Unity (URP)
- ScriptableObject 기반 설계
- Runtime / Data 분리
- 중앙 상태 관리 (GameContext)
- 전투 / 탐험 완전 분리 구조

---

## 🧪 문제 해결 경험

개발 과정에서 실제로 발생한 문제들을 구조적으로 해결했습니다.

👉 자세한 내용  
📄 [Problem Solving 문서](./Docs/08_Problem_Solving.md)

### 주요 해결 사례

- Continue 시 퀘스트 초기화 문제 해결
- 전투 ↔ 탐험 상태 동기화 문제 해결
- Secret Art 중복 적용 방지
- 애니메이션 타이밍과 데미지 동기화
- Git 대용량 업로드 문제 해결

👉 단순 구현이 아니라 “문제 해결 중심 개발”

---

## 🚀 실행 방법

1. Unity 2022.3 LTS 실행
2. 프로젝트 열기
3. Title 씬 실행

---

## 🎯 개발 의도

이 프로젝트에서 가장 중요하게 본 것은

👉 **기능의 개수가 아니라 구조의 안정성**

특히:

- 저장 / 불러오기 안정성
- 전투 → 탐험 상태 유지
- 시스템 간 데이터 일관성

을 중심으로 설계했습니다.

---

## 🔥 한 줄 요약

👉 “구조와 데이터 흐름을 중심으로 설계된 턴제 RPG 프로젝트”
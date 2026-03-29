# 10. Problem Solving

## 🎯 Overview

본 프로젝트에서는 다양한 시스템 구현 과정에서 여러 문제를 경험하였고,
이를 해결하는 과정에서 구조 설계와 디버깅 능력을 향상시켰다.

---

# ⚠️ Case 1. Quest Progress 초기화 문제

## 🔍 Problem

* 퀘스트 진행 중 게임을 종료 후 이어하기 시
* 수집 개수 및 진행 상태가 초기화됨

## 🧠 Cause

* Quest 상태가 GameContext와 SaveData에 제대로 저장되지 않음

## 💡 Solution

* Quest 상태를 SaveData에 포함
* GameContext 초기화 시 Quest 복원 로직 추가

## 🎯 Result

* 게임 재시작 후에도 퀘스트 진행 상태 유지

## 📌 Insight

* 상태 데이터는 반드시 저장 구조와 함께 설계되어야 함

---

# ⚠️ Case 2. 전투 중 턴이 스킵되는 문제

## 🔍 Problem

* 스킬 연출 중 다음 턴이 먼저 진행됨

## 🧠 Cause

* Coroutine 흐름이 연출 완료를 기다리지 않음

## 💡 Solution

* Cinematic Lock 시스템 도입

```csharp
PushCinematicLock();
PopCinematicLock();
```

## 🎯 Result

* 연출 완료 후 턴 진행

## 📌 Insight

* 게임 로직과 연출은 반드시 분리해야 한다

---

# ⚠️ Case 3. Enemy Hit Timing 문제

## 🔍 Problem

* 애니메이션 타이밍과 데미지 적용이 불일치

## 🧠 Cause

* 고정 delay 방식 사용

## 💡 Solution

* Animation Event 기반 Hit 처리
* fallback 로직 추가

## 🎯 Result

* 정확한 타격 타이밍 구현

## 📌 Insight

* 애니메이션 기반 시스템은 이벤트 중심 설계가 필요

---

# ⚠️ Case 4. Target 선택 오류

## 🔍 Problem

* 클릭 시 잘못된 적 선택됨

## 🧠 Cause

* Collider 기준 판단 오류

## 💡 Solution

* Transform → Actor 매핑 Dictionary 사용

```csharp
Dictionary<Transform, BattleActorRuntime>
```

## 🎯 Result

* 정확한 타겟 선택

## 📌 Insight

* UI와 게임 로직 연결은 명확한 매핑 구조 필요

---

# ⚠️ Case 5. 데이터 초기화 문제 (CharacterRuntime)

## 🔍 Problem

* 캐릭터 생성 시 데이터가 리셋됨

## 🧠 Cause

* 생성자에서 상태 초기화

## 💡 Solution

* InitForNewGame() 분리
* 생성자는 순수 생성만 수행

## 🎯 Result

* 데이터 유지 안정성 확보

## 📌 Insight

* 생성자 로직은 최소화해야 한다

---

# ⚠️ Case 6. 드랍 중복 지급 문제

## 🔍 Problem

* 전투 종료 시 보상이 중복 지급됨

## 🧠 Cause

* EndBattle() 중복 호출

## 💡 Solution

* _dropsGranted flag 추가

## 🎯 Result

* 보상 1회 지급 보장

## 📌 Insight

* 상태 기반 중복 방지 로직 필요

---

# 🧠 Overall Insight

이 프로젝트를 통해 다음을 경험했다:

* 상태 관리의 중요성
* 시스템 간 데이터 흐름 설계
* Coroutine 기반 흐름 제어
* 이벤트 기반 시스템 설계

---

# 💡 Why this matters

이 문제 해결 과정을 통해

* 실전 디버깅 능력 향상
* 구조 설계 이해도 증가
* 게임 시스템 안정성 확보

를 달성했다.

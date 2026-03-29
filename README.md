# 🎮 Pieces of Fantasy (PoF)

## 📌 Overview

**Pieces of Fantasy (PoF)**는
Unity 기반의 턴제 RPG 프로젝트로,
**Honkai: Star Rail 스타일 전투 시스템**을 목표로 개발되었다.

---

## 🎯 Core Loop

```plaintext
Exploration → Battle → Reward → Growth → Exploration
```

---

## 🧠 Core Architecture

### Single Source of Truth (SSOT)

```plaintext
GameContext
 ├─ party
 ├─ inventory
 ├─ rewardQueue
 ├─ battleSP
 └─ global state
```

👉 모든 시스템은 GameContext를 기준으로 동작

---

## ⚔️ Battle System

* SPD 기반 턴 큐
* Skill + SP 공유 시스템
* Cinematic Lock 기반 연출 제어
* Boss Pattern (Dragon / Golem)

---

## 🎬 Cinematic System

* 연출 중 턴 정지
* 카메라 제어 (Follow / Freeze / Fixed)
* Animation Event 기반 타이밍 처리

---

## 📈 Growth System

* LevelingPolicy 기반 구조
* 레벨 캡 / 승급 시스템
* Runtime 데이터 분리

```plaintext
CharacterData → CharacterRuntime
```

---

## 🎒 Inventory System

* ItemStack 구조
* Reward Queue 시스템
* DropTable 기반 보상

---

## 🌍 Exploration System

* GatherList 기반 상호작용
* Respawn / Unique 시스템
* MiniMap + Hotkey UI

---

## 🎨 UI / UX Design

* 정보 / 행동 / 피드백 분리
* 상태 기반 UI 반응
* 성장 프리뷰 시스템

---

## 🧩 Problem Solving Highlights

* Quest 초기화 버그 해결
* Cinematic Lock 설계
* Animation Event 타이밍 개선
* 데이터 초기화 문제 해결
* Drop 중복 방지

---

## 🛠️ Tech Stack

* Unity (URP)
* C#
* ScriptableObject
* Coroutine 기반 시스템

---

## 💡 Key Design Philosophy

* Single Source of Truth
* Data Driven Design
* Runtime Separation
* UX 중심 설계

---

## 🚀 Conclusion

이 프로젝트는 단순한 기능 구현이 아니라,

* 상태 관리 구조
* 전투 시스템 설계
* UX 중심 인터페이스

를 통합적으로 설계한 프로젝트이다.

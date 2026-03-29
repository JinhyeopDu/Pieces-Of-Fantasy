# 🎮 Pieces of Fantasy (PoF)

> Unity 기반 턴제 RPG 프로젝트
> **탐험 → 전투 → 보상 → 성장 → 탐험** 루프를 중심으로 설계한
> 구조 설계 중심의 개인 개발 프로젝트입니다.

---

## 🎬 Core Gameplay

전체 게임 플레이 영상은 아래 링크에서 확인할 수 있습니다.

👉 **[▶ YouTube Gameplay Video 보기](https://youtu.be/S-H_P9mZGVo?si=kCcr74VqkXThsIm-)**

이 영상에서는 다음 흐름을 확인할 수 있습니다.

* 탐험 → 전투 진입
* 턴제 전투 진행
* 보상 획득
* 캐릭터 성장
* 전체 게임 루프

---

## ⚡ Key Features

* ⚔️ **SPD 기반 턴 시스템**
* 🎯 **Shared Skill Point 시스템**
* 🎬 **Cinematic Lock (연출 중 턴 흐름 제어 시스템)**
* 🧠 **GameContext 기반 SSOT 구조**
* 📦 **Reward Queue 시스템**
* 📈 **정적 데이터 / 런타임 상태 분리 구조**
* 🐉 **보스 패턴 전투 구조**
* 🧩 **퀘스트 저장 / 복원 구조**

---

## 🖼️ Gameplay Preview

### ⚔️ Battle System

![Battle](Media/gifs/battle.gif)

* SPD 기반 턴 큐
* 타겟 선택 UI
* 기본 공격 / 스킬 선택
* Shared SP 소비 구조

---

### 🐉 Boss System

![Boss](Media/gifs/boss.gif)

* 보스 패턴 기반 전투
* Cinematic Lock 기반 흐름 제어
* 전투 연출 및 카메라 구성

---

### 📈 Growth System

![Growth](Media/gifs/growth.gif)

* 캐릭터 선택 및 상태 확인
* 재료 기반 성장 구조
* 성장 결과 프리뷰 UI

---

### 🎒 Inventory System

![Inventory](Media/gifs/inventory.gif)

* ItemStack 기반 구조
* 아이템 사용 흐름
* 보상 → 성장 연결 구조

---

## 🔁 Core Game Loop

```text
Exploration → Battle → Reward → Growth → Exploration
```

---

## 🧩 Data Structure

PoF는 **정적 데이터와 런타임 상태를 분리**해서 설계했습니다.

### 📦 정적 데이터 (ScriptableObject)

* CharacterData
* SkillData
* EnemyData
* ItemData
* EncounterData
* QuestData

### ⚙️ 런타임 데이터

* CharacterRuntime
* BattleActorRuntime
* QuestRuntimeProgress
* InventoryRuntime
* ItemStack

### 🎯 설계 의도

정적 데이터는 밸런스와 설계 정보에 집중하고,
플레이 중 변하는 값은 런타임 객체에서 관리하여
**유지보수성과 확장성을 확보했습니다.**

---

## ⚔️ Main Systems

### ⚔️ Battle System

* SPD 기반 턴 큐
* Shared Skill Point
* Animation Event 기반 타이밍 처리
* 보스 패턴 처리
* 전투 종료 후 GameContext 동기화

---

### 📈 Character Growth System

* LevelingPolicy 기반 성장 정책 분리
* 레벨 / 승급 구조
* 성장 결과 프리뷰 UI
* 캐릭터 상태 관리

---

### 📦 Inventory & Reward System

* ItemStack 기반 인벤토리
* DropTable 기반 보상 생성
* RewardQueue 기반 보상 표시
* 아이템 사용 → 성장 연결 구조

---

### 🌍 Exploration System

* CharacterController 기반 이동
* 상호작용 리스트 기반 UX
* 전투 진입 구조
* Respawn / Unique 처리
* MiniMap / Hotkey UI

---

### 🧩 Quest System

* questId 기반 상태 복원
* 진행도 / 완료 / 보상 분리
* Continue 안정성 확보
* 전투 / 탐험 시스템과 연동

---

## 🧪 Problem Solving

이 프로젝트에서는 단순 기능 구현이 아닌
**실제 플레이 중 발생한 문제를 구조적으로 해결하는 과정**을 중요하게 다뤘습니다.

### 주요 해결 사례

* Continue 시 퀘스트 초기화 문제 해결
* 전투 ↔ 탐험 상태 동기화 문제 해결
* Secret Art 중복 적용 문제 해결
* Animation Event 타이밍 불일치 문제 해결
* Git 대용량 업로드 문제 해결

👉 자세한 내용:
[📄 Problem Solving 문서 보기](Docs/08_Problem_Solving.md)

---

## 📚 Documentation

* [01. Project Overview](Docs/01_Project_Overview.md)
* [02. Core Architecture](Docs/02_Core_Architecture.md)
* [03. Battle System](Docs/03_Battle_System.md)
* [04. Character Growth System](Docs/04_Character_Growth.md)
* [05. Inventory & Reward System](Docs/05_Inventory_Reward_System.md)
* [06. Exploration System](Docs/06_Exploration_System.md)
* [07. Quest System](Docs/07_Quest_System.md)
* [08. Problem Solving](Docs/08_Problem_Solving.md)
* [09. UI / UX Design](Docs/09_UI_UX.md)
* [10. Additional Problem Solving](Docs/10_Problem_Solving.md)

---

## 🗂️ Project Structure

```text
Assets/Game
├── 01.Scenes
├── 02.Scripts
├── 03.ScriptableObjects
├── 04.Prefabs
├── 05.Art
├── 06.Addressables
├── 07.Audio
```

### 🎯 구조 의도

기능 단위가 아니라 역할 단위로 구조를 나누어
**유지보수성과 확장성을 확보했습니다.**

---

## 🛠️ Tech Stack

* Unity 2022.3 LTS
* C#
* URP (Universal Render Pipeline)
* ScriptableObject
* Coroutine 기반 비동기 처리
* JSON Save System
* Git / GitHub

---

## 🎯 What I Focused On

### 1. 데이터 흐름 안정성

GameContext 기반 SSOT 구조로
시스템 간 상태 불일치 문제 방지

---

### 2. 시스템 간 연결성

탐험 → 전투 → 보상 → 성장
전체 게임 루프가 자연스럽게 이어지도록 설계

---

### 3. 유지보수 가능한 구조

정적 데이터 / 런타임 분리
정책 분리 / UI-로직 분리
확장 가능한 구조 설계

---

## 🚀 Summary

**Pieces of Fantasy (PoF)**는
단순 기능 구현이 아닌

* 상태 관리 구조 설계
* 턴제 전투 시스템 구현
* 성장 / 보상 / 퀘스트 시스템 연결
* 실제 문제 해결 경험

을 포함한 **구조 설계 중심 RPG 프로젝트**입니다.

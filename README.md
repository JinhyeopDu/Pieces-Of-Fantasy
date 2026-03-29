# 🎮 Pieces of Fantasy (PoF)

> Unity 기반 턴제 RPG 프로젝트  
> **탐험 → 전투 → 보상 → 성장 → 탐험** 루프를 중심으로 설계한  
> 구조 설계 중심의 개인 개발 프로젝트입니다.

---

## 🎬 Core Gameplay

전체 게임 플레이 영상은 아래 링크에서 확인할 수 있습니다.

**[▶ YouTube Gameplay Video 보기](https://youtu.be/S-H_P9mZGVo?si=kCcr74VqkXThsIm-)**

이 영상에서는 아래 흐름을 전체적으로 확인할 수 있습니다.

- 탐험 씬 이동
- 전투 진입
- 턴제 전투
- 보상 획득
- 캐릭터 성장 UI
- 전반적인 플레이 흐름

---

## ⚡ Key Features

- ⚔️ **SPD 기반 턴 시스템**
- 🎯 **Shared Skill Point 시스템**
- 🎬 **Cinematic Lock 기반 연출 제어**
- 🧠 **GameContext 중심 SSOT 구조**
- 📦 **Reward Queue 기반 보상 처리**
- 📈 **정적 데이터 / 런타임 상태 분리**
- 🐉 **보스 패턴 전투 구조**
- 🧩 **퀘스트 저장/복원 구조**

---

## 🖼️ Gameplay Preview

아래 GIF는 전체 영상 중 핵심 기능만 빠르게 확인할 수 있도록 정리한 미리보기입니다.

### ⚔️ Battle System
![Battle](Media/gifs/battle.gif)

전투는 SPD 기반 턴 큐로 동작하며,  
플레이어는 기본 공격과 스킬 중 행동을 선택하고,  
공유 SP 자원을 관리하면서 전투를 진행합니다.

핵심 포인트:
- SPD 기반 턴 순서
- 타겟 선택 UI
- 기본 공격 / 스킬 선택
- Shared SP 소비 / 회복 구조

---

### 🐉 Boss Cinematic
![Boss](Media/gifs/boss.gif)

보스 전투는 일반 몬스터와 다르게  
연출, 패턴, 카메라 제어가 포함된 전투 흐름으로 구성되어 있습니다.

핵심 포인트:
- 드래곤 보스 패턴
- 일반 적과 구분되는 연출
- Cinematic Lock (연출 중 턴 흐름 제어 시스템)
- 카메라 포즈 및 보스 전용 전투 연출

---

### 📈 Growth System
![Growth](Media/gifs/growth.gif)

캐릭터 성장 시스템은 단순 수치 증가가 아니라  
레벨, 승급, 재료 선택, 프리뷰 UI를 포함한 구조로 설계했습니다.

핵심 포인트:
- 캐릭터 선택 UI
- 현재 스탯 확인
- 재료 기반 성장 구조
- 성장 결과 프리뷰

---

### 🎒 Inventory System
![Inventory](Media/gifs/inventory.gif)

인벤토리 시스템은 전투 보상, 아이템 보유, 사용, 성장 재료 흐름을 연결하는 역할을 담당합니다.

핵심 포인트:
- 아이템 목록 UI
- 상세 정보 확인
- 사용 가능한 아이템 선택
- 게임 루프와 연결되는 보상 처리 구조

---

## 🔁 Core Game Loop

```text
Exploration → Battle → Reward → Growth → Exploration

---

## 🧩 Data Structure

PoF는 **정적 데이터와 런타임 상태를 분리**해서 설계했습니다.

### 📦 정적 데이터 (ScriptableObject)
- CharacterData  
- SkillData  
- EnemyData  
- ItemData  
- EncounterData  
- QuestData  

### ⚙️ 런타임 데이터
- CharacterRuntime  
- BattleActorRuntime  
- QuestRuntimeProgress  
- InventoryRuntime  
- ItemStack  

### 🎯 설계 의도
정적 데이터는 밸런스와 설계 정보에 집중하고,  
플레이 중 변하는 값은 런타임 객체에서 관리하여  
**유지보수성과 확장성을 확보했습니다.**

---

## ⚔️ Main Systems

### ⚔️ Battle System
- SPD 기반 턴 큐  
- Shared Skill Point  
- 애니메이션 이벤트 기반 타이밍 처리  
- 보스 전용 패턴 처리  
- 전투 종료 후 GameContext 동기화  

---

### 📈 Character Growth System
- LevelingPolicy 기반 성장 정책 분리  
- 레벨 / 승급 구조  
- 성장 결과 프리뷰 UI  
- 캐릭터별 상태 관리  

---

### 📦 Inventory & Reward System
- ItemStack 기반 인벤토리  
- DropTable 기반 보상 생성  
- RewardQueue 기반 보상 표시  
- 아이템 사용 / 성장 재료 흐름 연결  

---

### 🌍 Exploration System
- CharacterController 기반 탐험 이동  
- 상호작용 리스트 기반 UX  
- 전투 진입 구조  
- Respawn / Unique 처리  
- Minimap / Hotkey UI  

---

### 🧩 Quest System
- questId 기반 퀘스트 복원  
- 진행도 / 완료 / 보상 수령 분리  
- Continue 안정성 강화  
- 전투 / 수집 / 탐험 시스템과 연결  

---

## 🧪 Problem Solving

이 프로젝트에서는 단순 기능 구현보다  
**실제 플레이 중 발생한 문제를 구조적으로 해결하는 과정**을 중요하게 다뤘습니다.

### 대표 사례

- Continue 시 퀘스트 진행 상태 초기화 문제  
- 전투 ↔ 탐험 상태 동기화 문제  
- Secret Art 중복 적용 문제  
- 애니메이션 이벤트와 실제 데미지 타이밍 불일치 문제  
- Git 대용량 파일 업로드 문제  

👉 자세한 내용은 아래 문서 참고  
[📄 Problem Solving 문서 보기](Docs/08_Problem_Solving.md)

---

## 📚 Documentation

프로젝트 상세 문서는 Docs 폴더에 정리했습니다.

- [01. Project Overview](Docs/01_Project_Overview.md)
- [02. Core Architecture](Docs/02_Core_Architecture.md)
- [03. Battle System](Docs/03_Battle_System.md)
- [04. Character Growth System](Docs/04_Character_Growth.md)
- [05. Inventory & Reward System](Docs/05_Inventory_Reward_System.md)
- [06. Exploration System](Docs/06_Exploration_System.md)
- [07. Quest System](Docs/07_Quest_System.md)
- [08. Problem Solving & Technical Decisions](Docs/08_Problem_Solving.md)
- [09. UI / UX Design](Docs/09_UI_UX.md)
- [10. Problem Solving](Docs/10_Problem_Solving.md)

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
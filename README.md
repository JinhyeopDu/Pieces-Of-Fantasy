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
- Cinematic Lock 기반 흐름 제어
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
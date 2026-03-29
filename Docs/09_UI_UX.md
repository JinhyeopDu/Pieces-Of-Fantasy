# 09. UI / UX Design

## 🎯 Overview

본 프로젝트의 UI/UX는 단순한 화면 구성이 아니라,
플레이어가 현재 상태를 빠르게 이해하고 행동을 선택할 수 있도록 설계되었다.

핵심 구조:

* 정보 표시 (State Awareness)
* 행동 선택 (Decision Making)
* 결과 피드백 (Feedback)

---

## 🧩 UI 구조

```plaintext
UI Layer
 ├─ Exploration UI
 ├─ Battle UI
 └─ Character / Growth UI
```

---

# ⚔️ Battle UI

## SkillSelectUI

* 행동 선택 UI
* 기본 공격 / 스킬
* SP 부족 피드백

### 특징

* 선택은 가능, 실행은 제한
* 상태 기반 UI 반응

---

## BattleHud

* HP 표시
* 턴 로그
* 현재 actor 표시

### 특징

* 최소 정보 집중
* 로그 기반 흐름 이해

---

## Target Marker

* 현재 타겟 표시
* 자동 재선택

---

## Damage Popup

* 데미지 수치 표시
* 크리티컬 강조

---

# 📈 Character UI

## Character Screen

* 캐릭터 선택
* 성장 UI

### 특징

* 성장 결과 프리뷰 제공
* 즉시 반영 구조

---

## EXP Bar

* 성장 진행도 시각화

---

# 🌍 Exploration UI

## GatherListUI

* 상호작용 리스트 제공

---

## MiniMap

* 위치 보조

---

## Hotkey UI

* 빠른 접근

---

# 🧠 UX 원칙

## 1. 정보와 행동 분리

* HUD = 정보
* UI = 행동

---

## 2. 즉시 피드백

* 데미지
* 보상
* 상태 변화

---

## 3. 상태 기반 UI

* SP 상태
* 타겟 상태
* 버프 상태

---

## 4. 게임 루프 연결

탐험 → 전투 → 성장 → 탐험

---

## 💡 Why this matters

이 구조를 통해

* 플레이어의 상태 인지 속도 향상
* 선택 과정 단순화
* 게임 몰입도 증가

를 달성했다.

## 🔍 UX Improvement Examples

- SP 부족 시 즉시 UI 피드백 제공
- 타겟 선택 상태 유지로 입력 반복 최소화
- 성장 결과 프리뷰 제공으로 사용자 의사결정 지원
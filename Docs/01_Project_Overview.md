# Project Overview

## 1. 프로젝트 목표
Pieces of Fantasy는 Unity 기반 턴제 RPG 프로젝트입니다.  
탐험과 전투를 자연스럽게 연결하고, 전투 이후 보상과 성장으로 이어지는 루프를 구현하는 것을 목표로 했습니다.

## 2. 핵심 게임 루프
탐험 → 전투 → 보상 → 성장 → 탐험

## 3. 설계 방향
이 프로젝트는 GameContext를 중심으로 한 SSOT(Single Source of Truth) 구조를 사용합니다.  
파티, 인벤토리, 보상, 포인트, 저장/로드 상태를 한 곳에서 관리하도록 설계했습니다.

## 4. 주요 시스템
- 탐험 이동 및 상호작용
- 턴제 전투
- Shared SP 시스템
- Secret Art 전투 시작 효과
- 캐릭터 성장 (레벨업 / 승급)
- 퀘스트 진행 및 보상
- 데이터 기반 설계 (ScriptableObject)

## 5. 관련 핵심 스크립트
- `Assets/Game/02.Scripts/Core/GameContext.cs`
- `Assets/Game/02.Scripts/Battle/BattleController.cs`
- `Assets/Game/02.Scripts/UI/CharacterScreenController.cs`
- `Assets/Game/02.Scripts/Exploration/PlayerControllerHumanoid.cs`
- `Assets/Game/02.Scripts/Exploration/BattleStarter.cs`
- `Assets/Game/02.Scripts/Quest/QuestManager.cs`

## 6. 현재 구현 상태
현재 탐험, 전투, 성장, 퀘스트의 핵심 루프는 구현되어 있으며,  
코드 구조와 시스템 정리를 지속적으로 진행 중입니다.
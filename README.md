# Pieces of Fantasy (PoF)

## 프로젝트 개요
Pieces of Fantasy는 Unity 기반 턴제 RPG 프로젝트입니다.  
탐험 → 전투 → 보상 → 성장 → 탐험 루프를 중심으로 설계했습니다.

## 장르
- 턴제 RPG
- Honkai: Star Rail 스타일 탐험/전투 구조

## 핵심 특징
- GameContext 기반 SSOT 구조
- ScriptableObject 기반 데이터 설계
- SPD 기반 턴제 전투
- Shared Skill Point 시스템
- CharacterData / CharacterRuntime 분리
- 캐릭터 성장 (레벨업 / 승급)
- 탐험 상호작용 및 전투 진입
- 퀘스트 진행 및 보상 시스템

## 폴더 구조
- `Assets/Game/01.Scenes`
- `Assets/Game/02.Scripts`
- `Assets/Game/03.ScriptableObjects`
- `Assets/Game/04.Prefabs`

## 주요 문서
- [프로젝트 개요](Docs/01_Project_Overview.md)

## 현재 구현 범위
- 탐험 씬 이동
- 전투 진입
- 턴제 전투
- Shared SP
- Secret Art
- 보상 획득
- 캐릭터 성장
- 퀘스트 진행
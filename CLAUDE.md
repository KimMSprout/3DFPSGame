# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity **6000.3.22f1** (URP 17.3) 기반 3D FPS 좀비 슈팅 게임이에요. 씬은 `Assets/01.Scenes/GameScene.unity` 하나뿐이에요.
빌드, 실행, 테스트는 Unity 에디터에서 해요. 자동화된 테스트는 없어요(`Assets/02.Scripts/Test/`의 스크립트는 C# 문법 연습용이에요).

## 명령어

```sh
dotnet tool restore                      # Husky.Net 설치 (dotnet-tools.json)
dotnet husky install                     # git hook 연결

# pre-commit hook과 같은 포맷 검사 (.editorconfig 기준)
dotnet format 3DFPSGame.sln --verify-no-changes --severity warn --include Assets/02.Scripts/
# 자동 수정
dotnet format 3DFPSGame.sln --include Assets/02.Scripts/
```

`.csproj`/`.sln`은 Unity가 생성하는 파일이에요. 스크립트를 추가하거나 옮긴 뒤 포맷 검사에서 파일이 빠지면 Unity 에디터에서 프로젝트 파일을 다시 생성해야 해요.

## 커밋 규칙

- `.husky/commit-msg`가 `type: 설명` 형식을 강제해요. 허용 type: `asset|build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test` (예: `feat: 미니맵 확대 및 축소`). 설명은 한국어로 써요.
- 에셋만 추가하는 커밋은 `asset:`을 써요.
- `main`에 push해서 `Assets/02.Scripts/**/*.cs`가 바뀌면 GitHub Actions(`.github/workflows/gemini-commit-review.yml`)가 Gemini로 코드 리뷰를 하고 커밋 코멘트를 남겨요.

## 코드 리뷰 기준

리뷰 기준은 `Docs/CodeReviewGuide.md`에 있어요. 코드를 리뷰하거나 작성할 때 이 기준을 따라요. 핵심은 다음과 같아요:
- 실제로 발생할 수 있는 문제를 우선하고, 과도한 리팩터링이나 패턴 적용은 피해요.
- 중요도는 Critical / High / Medium / Low로 나눠요.
- 매직 넘버는 `const` 또는 `[SerializeField]`로 분리해요(0, 1, -1 같은 관용적 값은 예외).
- Unity 관련 점검 항목: `Update`에서의 할당과 `GetComponent` 반복, 이벤트 해제, 생명주기 이후에 남는 코루틴, 문자열로 된 태그·레이어 참조, 풀링 필요성.
- 리뷰 결과 형식: 총평 → 발견된 문제(중요도/위치/설명/결과/수정 제안) → 설계 개선 제안(구조 문제가 명확할 때만) → 잘 작성된 점.

## 아키텍처

게임 코드는 모두 `Assets/02.Scripts/`에 있고 asmdef 없이 `Assembly-CSharp`로 컴파일돼요. 네임스페이스는 쓰지 않아요.

- **게임 상태:** `GameManager`(싱글턴, `GameManager.Instance`)가 `GameState`(Ready → Playing → GameOver)를 관리해요. 입력, 이동, 사격, 좀비 AI 스크립트는 모두 `Update` 첫 줄에서 `GameManager.Instance.State != GameState.Playing`이면 return하는 방식으로 상태를 확인해요. 새 게임플레이 스크립트도 같은 패턴을 따라요. 플레이어 체력이 0이 되면 `Player`가 `GameManager.Instance.GameOver()`를 호출해요.
- **데미지:** `Core/IDamageable.TakeDamage(DamageInfo)`를 `Player`, `Zombie`, `Drum` 등이 구현해요. `DamageInfo` 구조체에는 데미지 양, 맞은 위치, 맞은 면의 노멀이 들어 있어요(피 이펙트 방향에 사용). 폭발(`Bomb`, `Drum`)은 `Physics.OverlapSphere` + `LayerMask.GetMask("Drum", "Zombie")`로 대상을 찾으니 레이어 이름을 바꿀 때 주의하세요.
- **좀비 AI:** `Monster/Zombie.cs`는 enum `ZombieState`(Idle, Roaming, Trace, Comback, Attack, Hit, Death)와 `switch` 기반의 상태 머신이에요. 이동에는 NavMesh가 아니라 `CharacterController`를 써요.
- **오브젝트 풀링:** 방식이 세 가지 섞여 있어요.
  - `Player/PlayerFirePool`: `UnityEngine.Pool.ObjectPool` 기반의 자체 풀이에요(폭탄, 피격 이펙트). 첫 접근 때 `DontDestroyOnLoad` 숨김 오브젝트로 직접 생성되고, 발사 스크립트가 `Awake`에서 `Register*`로 프리팹을 등록해요. 파티클은 `stopAction = Callback`으로 자동 반환돼요.
  - Lean Pool(`Assets/Plugins/CW`): 현재 `PlayerGunFire`의 피격 이펙트는 `LeanPool.Spawn`을 써요.
  - `Assets/Modules/Submodules/Pools`: 별도의 풀 모듈이에요(`PoolManager`, `IPoolObject`).
  - 새 풀링 코드를 쓸 때는 주변 코드가 어떤 방식을 쓰는지 먼저 확인하세요.
- **입력:** Input System 패키지가 설치되어 있지만(`activeInputHandler: 2`, 즉 둘 다 사용) 실제 코드는 기존 `Input.GetAxis`/`Input.GetKeyDown` API를 써요.
- **UI:** `UI/` 폴더의 `UI_` 접두사 클래스들이에요. `UI_PlayerStats`는 `LateUpdate`에서 `Player`/`PlayerMove` 값을 읽어 슬라이더를 갱신해요(폴링 방식이고 이벤트는 쓰지 않아요). 미니맵은 `MinimapCamera` + `UI_Minimap`이에요.

## 코드 스타일

- private 필드는 `_camelCase`, Inspector에 노출하는 필드는 `[SerializeField] private`로 써요.
- 코루틴 메서드 이름에는 `_Coroutine` 접미사를 붙여요(예: `StartSequence_Coroutine`).
- 주석과 로그 메시지는 한국어로 써요.
- 폴더 구조는 번호를 붙인 에셋 폴더(`01.Scenes`, `02.Scripts`, `03.Prefabs` …)를 따라요.

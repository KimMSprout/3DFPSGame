# 조명 · 포스트 프로세싱 · 최적화 보고서

측정일: 2026-11-02 / Unity 6000.3.22f1 (URP 17.3) / 에디터 플레이 모드, Game View 1920×1080, vSync 끔

## 1. 측정 방법

- 도구: `Assets/02.Scripts/Tools/PerformanceBenchmark.cs`. 기존 컬링 최적화 보고서(`README.md`)와 같은 조건이에요.
  - 플레이어를 치우고 좀비 12마리를 고정 시드로 스폰한 뒤, 고정 시점 5곳에서 4초씩 `ProfilerRecorder`로 매 프레임 샘플링해요.
  - 이번에 `keepPlayer` 옵션을 추가했어요. 플레이어와 게임 카메라를 그대로 두고 현재 화면 하나(`00_PlayerView`)만 재요. 기존 방식은 플레이어를 지하로 치워서 **플레이어 캐릭터의 렌더링 비용이 측정에서 빠져 있었어요.**
- Stats 창, Profiler(CPU/Rendering/Memory) 캡처: 에디터 창의 백버퍼를 직접 읽어 저장했어요(`Screenshots/Evidence/`).
- 메모리: 씬 렌더러가 참조하는 텍스처의 **빌드 기준 용량**(임포트 설정 반영), 라이트맵 용량, 메시 메모리를 `*_memory.json`에 기록했어요. 에디터 전체 메모리(Total Used 등)는 에디터 자체 사용량이 섞여 있어서 참고만 해요.

| 단계 라벨 | 상태 |
|---|---|
| `lp_before` | 작업 전(낮 조명 + 밤하늘, Directional Light 2개) |
| `lp_lightonly` | 새 조명 적용, 포스트 프로세싱·SSAO는 런타임에서 끔 |
| `lp_after` | 새 조명 + 포스트 프로세싱 + SSAO(최적화 전) |
| `opt_after` | 최적화 적용 후 |
| `opt_before_player` / `opt_after_player` | 3인칭 플레이어 시점, 최적화 전/후 |
| `ab_player_lod0` / `ab_player_lod5` | 다른 최적화는 모두 켠 상태에서 플레이어 Mesh LOD만 0 ↔ 5 |

## 2. 조명 (Lighting)

### 발견한 문제
- 하늘은 별이 뜬 밤(`StarSkybox04`)인데, 흰색 Directional Light가 **2개**(강도 2 + 1.5) 켜져 있어 낮처럼 밝았어요.
- Fog가 켜져 있었지만 Density가 0이라 효과가 없었어요.
- Light Probe가 없어서 좀비, 플레이어처럼 움직이는 물체가 구운 간접광을 받지 못했어요.

### 적용한 내용
| 기능 | 설정 | 역할 |
|---|---|---|
| **Mixed** – 달빛 | Directional 1개만 유지, 색 `#9EB8F2`, 강도 1.2, 높이 35°, Soft Shadow | 간접광은 굽고 그림자는 실시간(최적화 후에는 Shadowmask)으로 처리해요 |
| **Baked** – 나트륨등 17개 | 격납고 14곳(NavMesh 위, 맵 중심을 향한 벽면) + 게이트 3곳, 주황 `#FF9E52`, 강도 8/6, 범위 18/14m | 라이트맵에 구워져 실행 중 비용이 0이에요. 차가운 달빛과 대비되는 따뜻한 빛 웅덩이를 만들어요 |
| **Realtime** – 총구 화염 | 기존 Point Light 5개 유지 | 순간적으로 켜지는 빛이라 실시간으로 둬요 |
| **Lightmap** | Progressive GPU, Directional 모드 | 정적 오브젝트 416개에 간접광과 램프 빛을 구워요 |
| **Light Probe** | NavMesh 위 4m 간격, 높이 0.6m/2.6m 2층, **1,696개** | 좀비와 플레이어가 위치에 맞는 밝기와 색을 받아요 |
| 환경광 | Trilight(하늘 남색 → 지면 거의 검정), Reflection 0.5 | 그늘의 기본 밝기를 밤 색감으로 맞춰요 |
| 안개 | Exponential, `#12161F`, 0.015 | 먼 곳을 어둡게 가려서 긴장감을 주고 시야를 제한해요 |

첫 베이크(달빛 0.6, 램프 3.5)는 화면이 거의 보이지 않을 만큼 어두워서 광원 강도를 올려 다시 구웠어요.

### 결과 화면 (01 시점)
| Before | 조명만 | 조명 + 포스트 | 최적화 후 |
|---|---|---|---|
| ![](Screenshots/lp_before_01_PlayerStart_Spawns.png) | ![](Screenshots/lp_lightonly_01_PlayerStart_Spawns.png) | ![](Screenshots/lp_after_01_PlayerStart_Spawns.png) | ![](Screenshots/opt_after_01_PlayerStart_Spawns.png) |

전경 사진(04 시점): `Screenshots/lp_before_04_Overview_High.png` → `Screenshots/lp_after_04_Overview_High.png`

## 3. 포스트 프로세싱 (Post Processing)

Global Volume(`Assets/Settings/SampleSceneProfile.asset`)과 Renderer Feature(`PC_Renderer`)를 수정했어요.

| 효과 | 설정 | 역할 |
|---|---|---|
| Tonemapping | Neutral → **ACES** | 대비가 강한 영화 같은 톤 |
| Bloom | Threshold 0.9, Intensity 0.7, Scatter 0.65, 따뜻한 Tint | 나트륨등과 총구 화염만 번져 보이게 해요 |
| Color Adjustments | Post Exposure 0.5, Contrast 12, Saturation −20, 약한 청색 Color Filter | 채도를 낮춰 음산한 분위기를 만들어요 |
| White Balance | Temperature −8 | 차가운 밤 색온도 |
| Vignette | 0.2 → 0.32, Smoothness 0.4 | 가장자리를 어둡게 해서 시선을 조준점으로 모아요 |
| Film Grain | Thin2, 0.18 | 거친 공포 영화 질감 |
| Motion Blur | 끔 유지 | FPS에서 멀미를 유발해요 |
| **SSAO** (Renderer Feature) | Intensity 0.4 → 1.2, Radius 0.35 | 벽 모서리, 컨테이너 틈 등 접촉면의 입체감을 살려요 |

### 조명과 포스트 프로세싱의 비용 (5개 시점 평균)
| 지표 | Before | 조명만 | 조명 + 포스트 |
|---|---|---|---|
| GPU (ms) | 1.40 | 1.51 | **1.87 (+34%)** |
| Batches | 544 | 410 | 559 |
| Shadow Casters | 388 | 437 | 444 |
| CPU Render Thread (ms) | 1.36 | 1.58 | 1.85 |

- 품질을 올린 대가로 GPU 시간이 34% 늘었어요. 포스트 프로세싱과 SSAO만 따지면 +0.36ms예요.
- SSAO의 Source가 `DepthNormals`라서 깊이+노멀 프리패스가 추가돼 Batches가 약 150개 늘어요(조명만 410 → 559).
- 달빛 각도를 35°로 낮춰 그림자가 길어지면서 Shadow Casters도 늘었어요.

## 4. 최적화

### AI가 제안한 후보와 선정 결과
| 분류 | 후보 | 선정 | 이유 |
|---|---|---|---|
| GPU | 플레이어 캐릭터 50만 tris → Mesh LOD | ✅ | 3인칭 프레임당 삼각형 568만 개. 가장 큰 병목 |
| GPU | Mixed Lighting을 Shadowmask로 전환 + 그림자 거리 50→40m, 캐스케이드 4→2 | ✅ | Shadow Casters가 조명 적용 후 444개로 늘었음 |
| GPU | SSAO 다운샘플 | ✅ | 품질 차이가 작고 픽셀 비용이 1/4로 줄어요 |
| RAM | Lightmap 해상도 40 → 16 texel/unit | ✅ | 라이트맵 12장(128MB)이 씬 텍스처 전체(86MB)보다 커요 |
| RAM | 작게 보이는 소품/무기 텍스처 2048 → 1024 | ✅ | 수류탄, 칼, 드럼통, 플레이어 텍스처 14장 |
| CPU | 매 프레임 / 매 상태 전환마다 호출되는 코드 정리 | ✅ | 규모는 작지만 `CodeReviewGuide`에서 지적하는 패턴 |
| CPU | 좀비 Animator 컬링, 풀링 | ❌ | 이미 `CullUpdateTransforms`와 LeanPool을 쓰고 있어요 |
| CPU | 좀비 SkinnedMeshRenderer 14개 → 1개로 병합 | ❌ | 효과는 크지만 모델 작업이 필요해 범위 밖이에요 |

### 적용한 변경
1. **플레이어 Mesh LOD** (`Assets/05.Models/Purple Teletubby.fbx`)
   - Generate Mesh LODs를 켜서 16단계(500,000 → 64 tris)를 생성했어요.
   - 씬의 플레이어 `SkinnedMeshRenderer.forceMeshLod = 5`(21,322 tris, −96%). 플레이어는 늘 카메라 가까이 있어서 자동 선택을 쓰면 LOD0이 선택돼요. 그래서 고정했어요.
   - 단계별 비교(`Screenshots/Evidence/player_meshlod_compare.png`, 왼쪽부터 LOD 0/3/4/5/6/8): LOD5까지는 원본과 구분되지 않고 LOD6부터 얼굴 UV가 깨져요.
2. **Shadowmask + 그림자 설정**
   - Lighting Mode: Baked Indirect → **Shadowmask**, Quality: Distance Shadowmask → **Shadowmask**
   - 정적 오브젝트의 달빛 그림자는 Shadowmask 텍스처로 굽고, 실시간 그림자 맵에는 움직이는 물체만 그려요.
   - URP: Shadow Distance 50 → 40m, Cascade 4 → 2
3. **SSAO** Downsample 켬(반 해상도)
4. **Lightmap 해상도** 40 → 16 texel/unit
5. **텍스처 Max Size** 2048 → 1024: 수류탄 4장, Magic Sword 5장, 드럼통 3장, 플레이어 2장
6. **스크립트(CPU)**
   - `PlayerZoom.Update`: 매 프레임 `GetComponent<Image>()`, `SetActive` 2번, FOV 대입을 반복 → Image를 `Awake`에서 캐시하고 **줌 상태가 바뀐 프레임에만** 반영
   - `PlayerGunFire.MuzzelFlash_Coroutine`: 발사할 때마다 찍던 디버그 잔여 로그 `Debug.Log(randomIndex)` 제거
   - `Zombie.ChangeState`: `Debug.Log` → 프로젝트의 `GameLogger.Log`(`[Conditional("ENABLE_LOG")]`). 측정 중 좀비 12마리가 30초에 약 65회 상태 전환 로그를 남겼어요.

## 5. 결과

### 5개 시점 평균 (조명 + 포스트 적용 후 → 최적화 후)
| 지표 | Before (작업 전) | 조명 + 포스트 | **최적화 후** | 최적화 효과 |
|---|---|---|---|---|
| Shadow Casters | 388 | 444 | **106** | **−76%** |
| Batches | 544 | 559 | **461** | −18% |
| SetPass Calls | 81 | 79 | **68** | −15% |
| Triangles | 174k | 185k | **132k** | −28% |
| GPU (ms) | 1.40 | 1.87 | **1.39** | **−26%** (작업 전 수준으로 복귀) |
| CPU Render Thread (ms) | 1.36 | 1.85 | **1.40** | −24% |
| CPU Main (ms) | 2.47 | 3.38 | 2.87 | 노이즈 범위 |

시점별 Shadow Casters: 01 520→141, 02 651→212, 03 275→16, 04 90→4, 05 682→158

### 플레이어 시점 (3인칭, `00_PlayerView`)
| 지표 | 최적화 전 | 최적화 후 |
|---|---|---|
| Triangles | **5,678k** | **308k (−95%)** |
| Vertices | 3,290k | 654k (−80%) |
| Shadow Casters | 449 | 146 |
| GPU (ms) | 2.11 | 1.55 (−27%) |

Mesh LOD만 분리한 A/B(다른 최적화는 모두 켠 상태): Triangles 4,138k → 284k(−93%), GPU 1.66 → 1.44ms(−13%), CPU Main 5.19 → 5.18ms(변화 없음)

### 메모리 (빌드 기준 용량)
| 항목 | Before | 조명 + 포스트 | **최적화 후** |
|---|---|---|---|
| 라이트맵 | 12장 / 256MB | 12장 / 128MB | **2장 / 42.7MB** (Shadowmask 포함) |
| 씬 텍스처(57장) | 86.5MB | 86.5MB | **44.5MB (−49%)** |
| 씬 메시 | 68.8MB | 68.8MB | 81.7MB (+12.9MB) |
| Light Probe | 0 | 1,696 | 1,696 |
| 라이트맵 굽는 시간 | – | 5분 15초 | **1분 9초 (−78%)** |

- Before의 256MB는 이전에 구워 둔 라이트맵이에요. 같은 12장을 현재 설정(압축 High Quality)으로 다시 구우니 128MB였어요. 이전 베이크 때 압축 설정이 달랐던 것으로 추정해요.
- 메시는 Mesh LOD 인덱스 데이터가 추가되면서 **12.9MB 늘었어요.** 정점은 공유하고 인덱스만 단계별로 저장하는 구조예요. GPU 부하를 줄이는 대신 메모리를 조금 더 쓰는 맞교환이에요.

### 증거 자료
| 구분 | Before | 조명 + 포스트 | 최적화 후 |
|---|---|---|---|
| Stats 창 | `Evidence/lp_before_stats_01.png` | `Evidence/lp_after_stats_01.png` | `Evidence/opt_after_stats_01.png` |
| Profiler CPU | `Evidence/lp_before_profiler_cpu.png` | `Evidence/lp_after_profiler_cpu.png` | `Evidence/opt_after_profiler_cpu.png` |
| Profiler Rendering | `Evidence/lp_before_profiler_rendering.png` | `Evidence/lp_after_profiler_rendering.png` | `Evidence/opt_after_profiler_rendering.png` |
| Profiler Memory | `Evidence/lp_before_profiler_memory.png` | `Evidence/lp_after_profiler_memory.png` | `Evidence/opt_after_profiler_memory.png` |
| 측정 JSON | `lp_before.json`, `lp_before_memory.json` | `lp_after.json`, `lp_lightonly.json`, `lp_after_memory.json` | `opt_after.json`, `opt_after_memory.json`, `*_player.json`, `ab_player_*.json` |
| 시점별 화면 | `Screenshots/lp_before_0*.png` | `Screenshots/lp_after_0*.png` | `Screenshots/opt_after_0*.png` |

## 6. 분석과 한계

- **가장 큰 효과는 측정 방식을 바꾼 데서 나왔어요.** 기존 벤치마크는 플레이어를 치워서 50만 tris 캐릭터가 보이지 않았어요. 플레이어 유지 모드를 추가하자 한 프레임 삼각형의 약 95%가 캐릭터 하나에서 나온다는 사실이 드러났어요.
- Shadowmask는 정적 그림자를 굽기 때문에 Shadow Casters를 크게 줄였어요. 대신 정적 그림자 경계가 Lightmap 해상도(16 texel/unit)만큼 부드러워져요. 밤 장면에서는 달빛 그림자가 원래 흐릿해서 차이가 거의 보이지 않았어요(01 시점 비교).
- **CPU 시간은 에디터에서 의미 있게 비교할 수 없었어요.** 같은 설정에서도 회차마다 ±1ms씩 흔들리고, Profiler상 프레임의 약 73%가 `EditorLoop`예요. 스크립트 수정의 효과(PlayerZoom 매 프레임 호출 4개 제거, 발사·상태 전환 로그 제거)는 이 노이즈보다 작아요. 실제 효과를 확인하려면 Development Build + Profiler로 측정해야 해요.
- GPU 수치(1~2ms)가 낮은 건 측정 PC의 GPU가 강력해서예요. 목표 기기가 저사양이라면 삼각형 −95%, Shadow Casters −76%의 효과가 시간으로 더 크게 나타날 거예요.
- 03, 05 시점은 원래 바닥 메시가 없는 맵 가장자리라서 하늘이 보여요(이번 작업과 무관).

### 추가로 해볼 만한 것
- SSAO Source를 `DepthNormals` → `Depth`로 바꾸면 노멀 프리패스(Batches 약 +150)가 사라져요. 품질 비교가 필요해요.
- 릴리스 빌드에서 `ENABLE_LOG` define을 빼면 `GameLogger.Log` 호출이 컴파일 단계에서 사라져요.
- 좀비 SkinnedMeshRenderer 14개를 하나로 합치면 좀비 수만큼 Draw Call과 스키닝 비용이 줄어요.

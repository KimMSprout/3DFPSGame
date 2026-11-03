# 렌더링 최적화 결과 (오클루전 컬링 · LOD Group · Mesh LOD)

측정일: 2026-10-30 / Unity 6000.3.22f1 / 에디터 플레이 모드, Game View 1920×1080, vSync 끔

## 측정 방법

- 도구: `Assets/02.Scripts/Tools/PerformanceBenchmark.cs`(플레이 모드에서 런타임에 붙여 실행하고, 씬에는 저장하지 않음)
- 조건: 플레이어는 조작 스크립트를 끄고 지하로 이동(좀비가 `Player` 태그를 찾아야 해서 `SetActive(false)` 대신 이렇게 처리), 좀비 12마리를 고정 시드로 스폰
- 고정 시점 5곳에서 워밍업 1초 후 4초 동안 `ProfilerRecorder`로 매 프레임 샘플링
- 최종 비교는 같은 세션에서 **최적화 OFF(런타임에서 끔) ↔ ON**을 번갈아 2회씩 측정한 평균

| 시점 | 설명 |
|---|---|
| 01_PlayerStart_Spawns | 시작 지점에서 스폰 포인트 쪽 |
| 02_BehindHangar | 격납고 바로 뒤(가려지는 곳) |
| 03_EastIndustrial | 동쪽 탱크·격납고 밀집 지역 |
| 04_Overview_High | 맵 모서리 높은 곳에서 전체 |
| 05_NorthSpawn | 북쪽 스폰 근처, 벽을 마주봄 |

## 적용한 변경

### 1. 오클루전 컬링
- 오클루전 데이터가 **한 번도 베이크되지 않은 상태**였음(카메라 옵션만 켜져 있어 효과 없음)
- Occluder Static 정리: 크고 속이 찬 것만 유지(격납고, 탱크, 컨테이너, 콘크리트 담, 벽, 덤프스터, 다리) → 163개 유지, 259개 해제
  - 해제 대상: 바닥·도로(가리는 것 없음), 얇은 파이프·가로등, 작은 소품, **격자 펜스(알파 머티리얼, 펜스 너머가 잘못 사라지는 원인)**
- Occludee Static은 정적 오브젝트 전체 유지
- 베이크 파라미터: Smallest Occluder 5 → **2**(높이 2.5~3m 콘크리트 담도 가림막으로 쓰이도록), Smallest Hole 0.25, Backface 100
- 결과물: `Assets/01.Scenes/GameScene/OcclusionCullingData.asset`(약 460KB)

### 2. LOD Group (136개)
- 에셋 팩의 `LD` 번호는 숫자가 클수록 고품질. 박스(12 tris)와 배럴(32 tris)은 씬에 이미 최저 단계가 배치되어 있었음
- `Cargo_container_v1` 14개: LOD0 `LD2close`(132 tris) → LOD1 `LD1close`(12 tris, 화면 높이 15% 미만) → Culled(2% 미만)
- 3m 이하 소품 122개(박스, 배럴, 팔레트, 에어컨, 전기함, 환풍구, 도로 블록): LOD0 → Culled(2.5% 미만). `lodBias = 2`라서 1m 소품은 약 70m 밖에서 사라짐

### 3. Mesh LOD
- 정적 배칭된 렌더러는 Mesh LOD가 동작하지 않고 항상 LOD0을 씀([문서](https://docs.unity3d.com/6000.3/Documentation/Manual/lod/mesh-lod-introduction.html)). 또 원본이 256 tris 이상이어야 생성됨
- 그래서 **정적 배칭이 아닌 메시**에만 적용함
  - `Assets/Zombie/FBXs/Zombie1.FBX`: 파츠 7개에 4~6단계 생성(예: 머리 1712 → 896 → 447 → 256 → 128 → 78 tris)
  - `Post-Apocalyptic Barrels.fbx`(Drum): 2038 → 1024 → 512 → 256 → 128 → 64 tris
- 격납고·탱크 등은 Batching Static이라 제외함(정적 배칭을 끄면 Draw Call이 늘어나는 맞교환 + 메시가 550~1100 tris로 이미 가벼움)

### 같이 고친 문제
- **Drum(`PRE_Barrel_Biohazard`) 3개가 씬에서 Static(Batching 포함)으로 덮어써져 있었음.** 폭발 시 `AddForce`로 날아가는데, 정적 배칭되면 화면상 위치가 고정되고 원래 위치 기준으로 컬링됨 → 인스턴스 오버라이드를 되돌려 프리팹 값(Static 없음)으로 맞춤

## 결과 (최적화 OFF 평균 → ON 평균)

OFF 상태는 원래 baseline의 렌더 카운트를 잘 재현함(보이는 렌더러 수 100% 일치, Draw Call ±8% 이내).

| 시점 | 보이는 정적 렌더러 | Draw Calls | Triangles | Shadow Casters |
|---|---|---|---|---|
| 01_PlayerStart_Spawns | 86 → 45 (**−47%**) | 842 → 732 (−13%) | 429k → 360k (−16%) | 539 → 407 (−25%) |
| 02_BehindHangar | 169 → 124 (**−27%**) | 889 → 814 (−8%) | 429k → 385k (−10%) | 565 → 512 (−9%) |
| 03_EastIndustrial | 151 → 111 (**−27%**) | 543 → 458 (−16%) | 322k → 294k (−9%) | 332 → 239 (−28%) |
| 04_Overview_High | 406 → 338 (**−17%**) | 1447 → 1335 (−8%) | 535k → 436k (−19%) | 133 → 68 (−49%) |
| 05_NorthSpawn | 361 → 281 (**−22%**) | 1551 → 894 (**−42%**) | 579k → 416k (−28%) | 718 → 569 (−21%) |

기법별로 효과가 나타난 곳:
- 오클루전 컬링: 지상 시점 4곳의 렌더러, Draw Call, 그림자 감소 대부분을 담당함. 고공 시점(04)은 가려지는 게 없어 효과 없음
- LOD Group: 고공 시점에서 보이는 렌더러 −17%(멀리 있는 소품 컬링)
- Mesh LOD: 좀비를 멀리서 보는 시점(04, 05)의 삼각형 −10~16%

### 프레임 시간: 에디터에서는 차이를 측정할 수 없음

| 회차(실행 순서) | CPU Main | GPU | Frame |
|---|---|---|---|
| OFF #1 | 15.0ms | 3.9ms | 16.7ms |
| ON #1 | 14.0ms | 3.5ms | 16.3ms |
| OFF #2 | 11.9ms | 3.6ms | 14.6ms |
| ON #2 | 14.3ms | 4.0ms | 17.0ms |

- 회차마다 ±2ms(약 15%)씩 흔들리고 ON/OFF 사이에 일관된 차이가 없음
- GPU는 3.5~4ms로 여유가 있어서, 그리는 양을 줄여도 프레임 시간이 줄지 않음. **병목은 메인 스레드(스크립트 + 에디터 오버헤드)**
- 실제 효과를 시간으로 확인하려면 Development Build + Profiler로 측정해야 함

## 다시 측정하기

플레이 모드에서 아래 코드를 실행하면 `Docs/Optimization/<label>.json`과 스크린샷이 저장됨.

```csharp
new GameObject("Benchmark").AddComponent<PerformanceBenchmark>().Begin("label");        // 현재 상태
new GameObject("Benchmark").AddComponent<PerformanceBenchmark>().Begin("label", true);  // 최적화를 런타임에서 끈 상태
```

에디터가 포커스를 잃으면 플레이 모드가 스로틀링되므로 벤치마크가 `Application.runInBackground`를 켬. 씬 뷰가 함께 그려지지 않도록 Game View를 최대화한 상태에서 측정함.

## 파일

| 파일 | 내용 |
|---|---|
| `baseline.json`, `baseline_2.json` | 최적화 전(노이즈 확인용 2회) |
| `step1_occlusion.json` | 오클루전만 적용 |
| `step2_lodgroup*.json`, `ab_lodgroup_off.json` | LOD Group 추가 + A/B 확인 |
| `step3_meshlod.json` | Mesh LOD 추가 |
| `final_off_*.json`, `final_on_*.json` | 최종 A/B(번갈아 2회씩) |
| `final_summary.json` | 최종 A/B 합산 |
| `Screenshots/` | 각 측정·시점별 화면 |

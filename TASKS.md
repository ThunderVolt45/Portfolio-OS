# TASKS — Portfolio-OS (Epic / Task 보드)

> **Epic**(큰 목표·딜리버러블) 아래 **Task**(실행 단위)로 작업을 관리한다. 날짜 로그가 아니라 영역별 보드.
> 새 세션은 `CLAUDE.md`(§0~§5: 온보딩·API·git·MCP 레퍼런스)를 먼저 읽고, 현황은 이 문서에서 파악·갱신한다.
>
> **상태**: ✅ Done · 🔄 In Progress · ⬜ Todo · 🔒 Blocked(외부 의존)
> **ID 규칙**: Epic = `E<n>`, Task = `E<n>-T<m>`. 완료 Task도 이력 참조용으로 남겨둔다.
> **갱신 규칙**: 작업 후 해당 Task 상태 + (필요 시) 구현 노트만 고친다. 새 작업은 Task를 추가한다.

## 🎯 목표 시나리오 (North Star)

방문자가 경험하는 최종 흐름. 모든 Epic/Task는 이 시나리오를 실현하기 위한 것.

1. **접속** — GitHub Pages 랜딩 페이지(모니터 셸 `monitor.html` 안에 Unity WebGL 캔버스).
2. **부팅 연출** — 기본 Unity WebGL 로딩 화면을 **부팅 연출로 대체**(커스텀 WebGL 템플릿).
   - 부팅 진행은 `createUnityInstance(..., onProgress)`의 **실제 로드 progress**로 구동 → ~30MB 다운로드/초기화 시간을 부팅 연출이 그대로 가린다(진행 막대 노출 없음).
   - **부팅 연출은 템플릿에서 완결**되고, Unity ready 시 데스크톱으로 **매끄럽게 핸드오프**(플래시/점프 없이). Unity 쪽 연장 연출은 없음.
3. **부팅 완료 → 3창 자동 오픈**: 자기소개(About) · Contact · **Projects 런처**. (겹침 방지 cascade 배치.)
4. **데스크톱 사용법 학습** — 바탕화면의 사용 방법 텍스트를 읽고 조작 학습(아이콘 실행, 창 드래그/최소화/닫기, 작업표시줄 복원 등).
5. **데스크톱 아이콘으로 콘텐츠 접근** — 아이콘 **4개**로 집약:
   - **About**(자기소개) · **Contact** · **Resume**(이력서 PDF)
   - **Projects 런처 창** — 프로젝트 **7종**을 리스트+상세로 집약, 항목 선택 시 라우팅:
     - PDF형: BrawlStarsTPS · Nova-Revolution · ProjectBlackout → PDF 뷰어(E3)로 오픈
     - UGUI형: UGUI-Window-Sample → 전용 UGUI 소개 창(PDF 미사용)
     - 그 외: 고아미 캠프 · Colyseus-Server-Sample · Gyeongseong97 → 런처 내 상세(SSOT 기반)
6. **모바일 / WebGL 미지원 폴백** — 정적 안내 + 핵심 링크(이력서·연락처·GitHub) 페이지 제공. 단, **WebGL 구동 가능한 모바일은 "그래도 입장" 허용**.

## Epic 개요

| Epic | 제목 | 상태 | 다음 액션 |
|---|---|---|---|
| **E1** | 창 프레임워크 유지 (upstream 동기화) | ✅ 최신 | `UGUIWindowSwitcher` 실사용 점검 (E1-T2) |
| **E2** | 포트폴리오 앱 창 & 콘텐츠 | 🔄 진행 | Projects 런처 창 (E2-T4) |
| **E3** | PDF 문서 뷰어 (PDF.js 오버레이) | 🔄 핵심완결 | monitor.html 셸 좌표 정합 (E3-T3) |
| **E4** | WebGL 빌드 & 배포 | 🔄 빌드완료 | `ThunderVolt45.github.io` 배포 (E4-T4) |
| **E5** | 폴리시 & 위생 | 🔄 진행 | 폰트 SDF 노이즈 방침 (E5-T4) |
| **E6** | 부팅 & 데스크톱 셸 연출 | ⬜ 미착수 | 커스텀 WebGL 템플릿 부팅 (E6-T1) |

> **프로젝트 최종 목표 = E4-T4**(WebGL 빌드를 모니터 셸에 임베드해 GitHub Pages 발행).

---

## E1 — 창 프레임워크 유지 (upstream 동기화) · ✅ 최신

> 목표: `upstream`(ThunderVolt45/UGUI-Window-Sample)의 프레임워크 개선을 주기적으로 병합해 최신 유지.

| ID | Task | 상태 |
|---|---|---|
| E1-T1 | upstream 1~3차 동기화 반영 (본문 스크롤·작업표시줄 복구·창 전환 오버레이·스케일 드래그 보정) | ✅ |
| E1-T2 | 신규 `UGUIWindowSwitcher`(창 전환 오버레이) 포트폴리오 실사용 여부 + PDF 오버레이(E3)와 상호작용 점검 | ⬜ |

**들어온 upstream 기능** — 창 본문 스크롤(ScrollRect) · 작업표시줄/최대화 영역 복구 fix · 창 전환 오버레이 `UGUIWindowSwitcher.cs`(`a1bf9f5`,`edb7939`) · 스케일 환경 드래그 보정 `UGUIWindowManager.GetPointerDeltaInRect(eventData, relativeTo)`(`a64fde2`).

**구현 노트 — 병합 절차(중요)**
- 씬 충돌은 **손 병합 금지**, Unity Smart Merge 사용: `UnityYAMLMerge.exe merge -p <base> <theirs> <ours> <out>` (git 인덱스 `:1`=base / `:3`=theirs / `:2`=ours 추출). fileID 기준 병합이라 내 아이콘 + upstream 요소 모두 보존. 스크립트/프리팹은 auto-merge.
- 병합 전 폰트 SDF 노이즈 7개(E5-T4)는 `git checkout --`로 되돌린 뒤 merge.
- 내가 구독하는 이벤트 `OnManagedWindowOpened/Focused/Minimized/Closed` + `CreateWindow/CreateWindowEx` 시그니처는 3차까지 **불변** 확인.

---

## E2 — 포트폴리오 앱 창 & 콘텐츠 · 🔄 진행

> 목표: About·Contact·Resume + Projects 런처(7종)를 앱 창으로 구현. 모두 `Assets/Scripts/Portfolio/`(namespace `UGUIWindow`), 프리팹 `Assets/Resources/Windows/<클래스명>.prefab`. 콘텐츠 SSOT는 CLAUDE.md §1 `index.md`.

| ID | Task | 상태 |
|---|---|---|
| E2-T1 | `AboutWindow` (정체성 + 핵심역량 + 연락처) | ✅ |
| E2-T2 | `DocumentViewerWindow` + PDF별 서브클래스 3 (`BrawlStarsTPSDocWindow`/`NovaRevolutionDocWindow`/`ProjectBlackoutDocWindow`) | ✅ |
| E2-T3 | 구 `ProjectBlackoutWindow`(전용 창) 제거 → PDF 뷰어로 대체 | ✅ |
| E2-T4 | **`ProjectsWindow`(런처)** — 7종 리스트+상세, 항목 선택 시 라우팅(PDF 뷰어 / UGUI 창 / 런처 내 상세) | ⬜ |
| E2-T5 | `ContactWindow` — 이메일·GitHub·전화 | ⬜ |
| E2-T6 | 창 본문 스크롤(E1 프레임워크 반영분) 포트폴리오 창에 실제 적용/동작 확인 | 🔄 |
| E2-T7 | `UGUISampleWindow` — UGUI-Window-Sample 소개 전용 창(PDF 미사용, UGUI만). GitHub repo 링크 포함 | ⬜ |
| E2-T8 | 데스크톱 아이콘 집약 → **About·Contact·Resume·Projects 4개**. 개별 PDF 아이콘 3개는 Projects 런처로 이동(제거) | ⬜ |

> **E2-T4 프로젝트 7종 라우팅**
> - PDF형(뷰어): ProjectBlackout · Nova-Revolution · BrawlStarsTPS
> - UGUI형(전용 창, E2-T7): UGUI-Window-Sample
> - 런처 내 상세(SSOT 텍스트): 고아미 캠프 · Colyseus-Server-Sample · Gyeongseong97

**구현 노트 — 프리팹 = Prefab Variant (필수 패턴)**
- 창 프리팹은 base `UGUIWindow.prefab`(guid `23a3495e…`)의 **Variant**로 만든다(GUID 교체 독립복제 금지: upstream 창 개선을 못 받음).
- Unity는 Variant에서 컴포넌트 `m_Script` 교체 불가 → 에디터툴 `Assets/Editor/PortfolioPrefabTools.cs`로 우회:
  - 메뉴 **Portfolio → Rebuild Window Prefab Variants** (About/Doc)
  - 메뉴 **Portfolio → Build PDF Doc Windows + Icons** (PDF 서브클래스 3 + 씬 아이콘)
  - 동작: base 인스턴스화 → root `UGUIWindow` 컴포넌트 제거 + subclass 추가 → `SerializedObject`로 필드 복사 → `SaveAsPrefabAsset`. 검증: `isVariant=True`, root 컴포넌트 = `UGUIWindowView` + subclass.
- base body는 `Content/Viewport/ScrollContent`(ScrollRect). 단일 `ContentText`(TMP, stretch)를 ScrollContent에 배선 → About은 `contentText` 필드, Doc은 `documentRelativePath`.
- `DocumentViewerWindow` base에 `protected virtual string DocumentPath/DocumentTitle` override 지점(서브클래스는 경로·제목만). 매니저가 **타입명으로 프리팹 로드**(`Resources.Load("Windows/"+typeName)`)라 PDF별 별도 타입 필수.

**구현 노트 — 함정**
- ⚠️ 창 초기화(콘텐츠·크기)는 `Start()`가 아니라 **`OnEnable()`**에서. `execute_code`로 만든 오브젝트는 Start가 안 뜨고, 매니저가 Instantiate 직후 제목을 클래스명으로 덮어씀.
- E2-T8 데스크톱 아이콘: 씬 `UGUI_Desktop/IconGrid`에 `Icon_About` 복제로 추가, `targetClassName`+라벨+`anchoredPosition`만 변경. (현재 개별 PDF 아이콘 3개가 배치돼 있으나 런처 집약으로 제거 예정.)

---

## E3 — PDF 문서 뷰어 (PDF.js 오버레이) · 🔄 핵심 완결

> 목표: PDF를 창 안에서 native 수준(선택·검색·폼·하이퍼링크)으로 보여주고, 다중 창/포커스 전환에서 상태 소실 없이 동작. Projects 런처(E2-T4)의 PDF형 항목이 이 뷰어를 띄운다.

| ID | Task | 상태 |
|---|---|---|
| E3-T1 | 포커스-스왑 오버레이 (창별 iframe 유지, 다중창, 좌표 동기) | ✅ |
| E3-T2 | URL 해시 `#open=클래스명` 딥링크 (`PortfolioBootstrap.cs`) | ✅ |
| E3-T3 | monitor.html 셸(스케일/오프셋 캔버스) 좌표 정합 검증 — docs 세션 조율 | 🔒 |
| E3-T4 | 복귀(백그라운드→라이브) 경로 실브라우저 눈확인 + 포커스아웃 스냅샷 플래시 refinement | ⬜ |
| E3-T5 | 고도화: 다페이지 스크롤 + HTTP Range + 텍스처 가상화 + "PDF 원본 다운로드" 버튼 | ⬜ |
| E3-T6 | release 빌드에서 각 PDF 항목 → 해당 PDF 눈확인 (실렌더는 WebGL 전용) | ⬜ |

**구현 노트 — 오버레이 구조**
- jslib `Assets/Plugins/WebGL/PdfOverlay.jslib`. **창마다 자기 전용 iframe 1개**를 만들어 살려 둠(파괴 안 함, `id`=`GetType().Name` 키잉). `window.__pdfOverlays[id] = {wrap,iframe,url}`, 모든 함수 첫 인자 `id`.
- 포커스/최상단 창만 자기 iframe 표시(`PdfOverlayShow(id)`), 백그라운드로 밀리면 자기 iframe을 스냅샷으로 굳히고 숨김(`PdfOverlayHide(id)`) → 다시 라이브면 숨김만 해제 = **리로드/상태소실 없음**. 각 창이 자기 iframe만 스냅샷하므로 교차오염 불가.
- 스왑 트리거: `UGUIWindowManager.OnManagedWindowFocused/Minimized/Closed` **+ `OnManagedWindowOpened`**(Open은 포커스 이벤트 미발생 → Opened도 구독해야 새 창이 기존 라이브를 백그라운드로 밀어냄). `OnAnyWindowActivated`가 Opened+Focused 공용 핸들러.
- 좌표매핑: 콘텐츠 RectTransform world corners → `RectTransformUtility.WorldToScreenPoint`(Unity px) → jslib에서 `canvas.getBoundingClientRect()`+버퍼크기로 CSS 변환·Y뒤집기·경계 클리핑.
- 자원: pdf.js 정식 viewer **3.11.174** = `Assets/StreamingAssets/pdfjs/{build,web}`(~9MB). PDF 실파일 = `Assets/StreamingAssets/docs/*.pdf`(ASCII명 `brawlstarstps`/`novarevolution`/`projectblackout`; SSOT는 docs 프로젝트 `portfolio/projects/*.pdf`). `DocumentPath`가 `docs/<name>.pdf` 반환(viewerUrl `../../docs/...`).
- 딥링크(E3-T2)는 `Type.GetType` 방식이라 IL2CPP 스트리핑 안전.

**검증됨** — 다중 PDF 열기 + 문서 간 포커스 전환 리로드 없음(사용자 수동 WebGL 빌드). 딥링크 오픈→좌표 정합, 리사이즈 재계산·클리핑, 스냅샷 스왑.

---

## E4 — WebGL 빌드 & 배포 · 🔄 빌드 완료, 배포 대기

> 목표: GitHub Pages 제약(싱글스레드·파일당 100MB·Content-Encoding 못 넣음) 안에서 재현 가능한 배포 빌드를 만들고, 모니터 셸에 임베드해 발행.

| ID | Task | 상태 |
|---|---|---|
| E4-T1 | 재현 빌드 스크립트 `PortfolioBuild.cs` (메뉴 + 배치모드) | ✅ |
| E4-T2 | Brotli 압축 + Decompression Fallback 채택 (~30MB) — 스크립트에 반영 완료, 재빌드 눈확인 남음 | 🔄 |
| E4-T3 | 로컬 서버 `Tools/serve.py`(`.br` 서빙 + HTTPS) + preview `launch.json` | ✅ |
| E4-T4 | **`ThunderVolt45.github.io` repo 생성 + 배포** (모니터 셸 HTML + WebGL 임베드) — 최종 목표 | ⬜ |
| E4-T5 | 모바일/WebGL 미지원 폴백: 정적 안내+링크 페이지 + capable 모바일 "그래도 입장" 허용 — monitor.html/docs 세션 조율 | ⬜ |

**구현 노트 — 빌드 설정**
- `Assets/Editor/PortfolioBuild.cs`(`PortfolioOS.EditorTools.PortfolioBuild`) — 배포 설정을 코드로 고정(에디터 UI 상태에 의존 X). 메뉴 **Portfolio → Build WebGL (Release/Development)** + 배치모드(`-executeMethod …BuildWebGLRelease`).
- release = **Brotli + Decompression Fallback=Enabled** + High 매니지드 스트리핑 + IL2CPP `Release` + 싱글스레드 + linker Wasm. dev = Minimal/Debug.
- 배포 크기: 압축 Disabled 52MB → **Brotli ~30MB**. GH Pages가 Content-Encoding을 못 넣지만 Unity 내장 JS 디컴프레서(fallback)가 클라이언트에서 `.br`을 풀어 배포 가능.
- 로컬 `serve.py`: wasm MIME `application/wasm`, `.br`/`.gz` 사전압축 서빙 + `Content-Encoding` 헤더, HTTPS(self-signed; Brotli는 보안 컨텍스트 필요), `Cache-Control: no-store`. preview는 `.claude/launch.json`의 `webgl`(port 8000).

> **E4-T2가 아직 🔄인 이유**: `PortfolioBuild.cs`를 Brotli로 바꾼 건 코드뿐. 실제 재빌드로 산출물이 `.br`로 나오고 브라우저에서 폴백 로드되는지 눈확인해야 ✅.
> **E4-T5 참고**: capable 모바일 진입 강행은 monitor.html이 WebGL/디바이스 판정 후 "그래도 입장" 경로를 노출하는 방식(docs 세션과 셸 스펙 합의 필요).

**구현 노트 — 빌드 함정**
- 빌드는 메인스레드 동기라 **빌드 내내 MCP 브리지 끊김**(정상). 완료는 `Build/WebGL/Build/WebGL.wasm` mtime 폴링/콘솔로 확인.
- preview 브라우저는 SwiftShader라 셰이더 에러 로그·`preview_screenshot` 타임아웃 정상 → 검증은 `preview_console_logs`로. `Build/`은 .gitignore.

---

## E5 — 폴리시 & 위생 · 🔄 진행

> 목표: 폰트/아이콘/리포 노이즈 등 품질·유지보수 항목 정리.

| ID | Task | 상태 |
|---|---|---|
| E5-T1 | 한글 폰트 폴백 (WantedSans 동적 SDF를 TMP 전역 폴백에 추가) | ✅ |
| E5-T2 | 일부 TMP 텍스트 한글 글리프 미스 경고(□) 재점검 | ⬜ |
| E5-T3 | 아이콘 프리팹 `windowIcon` 스프라이트 할당 (About/Contact/Resume/Projects + 런처 항목) | ⬜ |
| E5-T4 | 폰트 SDF 노이즈 처리 방침 확정 | 🔄 |
| E5-T5 | `ProjectSettings.preloadedAssets`(InputSystem) 노이즈 관리 | 🔄 |
| E5-T6 | 외부 유료 에셋(MPUIKit) gitignore 제외 | ✅ |

**구현 노트**
- E5-T1: 기본 TMP(LiberationSans)에 한글 없어 □로 깨짐 → WantedSans 동적 SDF를 TMP Settings `m_fallbackFontAssets`(전역 폴백)에 추가. 창 텍스트 한글 정상 렌더 확인.
- E5-T3: `UGUIIcon.ApplyTargetWindowIcon`은 `windowIcon`이 있으면 데스크톱+작업표시줄에 **자동 적용**, null이면 스킵(현재 빈 아이콘 = 시각적 일관). 각 프리팹 `windowIcon`에 스프라이트를 넣으면 자동 반영.
- E5-T4: `Assets/UGUIWindowSample/Fonts/WantedSans-*.asset` 7개가 동적 아틀라스에 한글 글리프가 구워지며 매번 "수정됨"으로 뜸(`_typelessdata`, ~1392줄). 지금은 작업/병합 때마다 `git checkout --`로 되돌리는 중 → 근본 처리(정적 pre-bake 또는 커밋/ignore 정책) 필요.
- E5-T6: **MPUIKit** = 에셋스토어 **유료** 에셋(재배포 불가) → 저장소에서 **제외**. `.gitignore`에 `/[Aa]ssets/MPUIKit/`·`/[Aa]ssets/MPUIKit.meta` 추가. 빌드는 이 에셋이 설치된 로컬에서 수행(타 환경/CI에는 미설치 → UI 깨질 수 있음, 필요 시 설치 안내 메모로 대체).
  - ⚠️ WantedSans 폰트를 `Assets/Fonts/`에 별도 반입하려다 철회 — **이미 `Assets/UGUIWindowSample/Fonts/`에 동일 7종이 존재**(중복). 폰트는 그쪽을 SSOT로 사용.

---

## E6 — 부팅 & 데스크톱 셸 연출 · ⬜ 미착수

> 목표: 접속 → 부팅 → 자동 오픈 → 데스크톱 학습의 첫인상 연출(North Star 2~4단계). 부팅은 **기본 Unity WebGL 로딩 화면을 대체**하는 커스텀 WebGL 템플릿에서 재생.

| ID | Task | 상태 |
|---|---|---|
| E6-T1 | 커스텀 Unity WebGL 템플릿 — 기본 로딩 화면을 부팅 연출로 대체. `createUnityInstance(..., onProgress)` 실제 progress로 부팅 진행 구동(다운로드/초기화 시간 마스킹). **부팅은 템플릿에서 완결** | ⬜ |
| E6-T2 | Unity ready → 데스크톱 등장 + 3창 자동 오픈 (**About·Contact·Projects 런처**, cascade 배치). 템플릿 부팅 → Unity 첫 프레임 시각적 연속성(플래시/점프 없이) | ⬜ |
| E6-T3 | 데스크톱 사용법 텍스트 (아이콘 실행·창 드래그/최소화/닫기·작업표시줄 복원 안내) | ⬜ |

**설계 메모**
- **부팅은 WebGL 템플릿(HTML/JS/CSS)에 산다** — Unity C# 런타임은 다운로드/초기화 중엔 아직 안 뜨므로, 그 시간을 가리려면 템플릿 레벨이어야 함. 템플릿은 `Assets/WebGLTemplates/<이름>/`에 두고 Player Settings의 `PlayerSettings.WebGL.template = "PROJECT:<이름>"`로 지정 → **E4(빌드) 연동**: `PortfolioBuild.cs`가 템플릿을 코드로 고정해야 재현 빌드에 반영됨.
- E6-T2 핸드오프: 템플릿 부팅 UI가 Unity ready 시점에 페이드/전환되며 데스크톱으로 이어짐. 자동 오픈은 `PortfolioBootstrap`/부팅 완료 콜백에서 `CreateWindow`. 딥링크(E3-T2) `#open=`가 있으면 자동 오픈 대신 해당 앱 우선.
- **monitor.html 셸과의 관계**: 셸은 "모니터 그릇"만 제공하고 부팅 연출은 WebGL 템플릿이 담당(진짜 progress 접근이 여기 있음). 셸 좌표/스케일(E3-T3, E4-T5) 가정과 어긋나지 않게 docs 세션과 조율.
- ⚠️ 커스텀 템플릿은 Unity 기본 로더 스크립트(`{{{ LOADER_FILENAME }}}` 등 플레이스홀더) 구조를 유지해야 빌드가 깨지지 않음.

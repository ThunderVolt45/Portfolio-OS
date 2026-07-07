# CLAUDE.md — Portfolio-OS (김민영 포트폴리오 데스크톱 OS)

이 저장소는 **김민영님의 취업 포트폴리오를 "데스크톱 OS" 형태로 구현**하는 Unity 프로젝트입니다.
본인이 만든 창(Window) 프레임워크 **UGUI-Window-Sample** 위에 About·Projects·Resume·Contact 등을 "앱 창"으로 얹어,
**Unity WebGL로 빌드 → HTML '모니터' 셸 안에 임베드**하여 랜딩 페이지로 발행하는 것이 목표입니다.
(자기 프레임워크를 dogfooding해 포트폴리오 UI 자체로 쓰는 것이 핵심 어필 포인트)

> 이 문서는 **다른 세션에서 넘어온 핸드오프 온보딩**입니다. 새 세션은 이 문서를 먼저 통독하세요.

---

## 0. 시작 시 먼저 할 일 (콜드 스타트)

1. 이 문서를 통독한다.
2. **Unity MCP 연결 확인**: `claude mcp list` → `UnityMCP: http://127.0.0.1:8080/mcp — ✔ Connected` 이어야 함.
   - 안 뜨면 Unity 에디터가 열려있는지, **Window → MCP for Unity**가 Connected인지 확인.
3. 프레임워크 구조 파악: `Assets/UGUIWindowSample/Scripts/`(§3), 샘플 씬 `Assets/UGUIWindowSample/Scenes/UGUIWindowSampleScene.unity`.
4. **콘텐츠·인적사항의 단일 진실 공급원(SSOT)은 별도 docs 프로젝트에 있음**(§1) — 절대경로로 읽어 참조.

---

## 1. 외부 SSOT (docs 프로젝트 — 절대경로로 참조)

포트폴리오 텍스트/인적사항은 **이 repo가 아니라** 아래 docs 프로젝트가 원천이다. 지어내지 말고 여기서 가져올 것.

- **인적사항·경력·프로젝트 사실관계 (마스터)**:
  `C:\Users\zxc98\Claude\Projects\취업 포트폴리오 프로젝트\CLAUDE.md`
- **랜딩/About/프로젝트 카드 확정 문구**:
  `C:\Users\zxc98\Claude\Projects\취업 포트폴리오 프로젝트\portfolio\index.md`
- **프로젝트별 상세(기술문서/케이스스터디)**:
  `C:\Users\zxc98\Claude\Projects\취업 포트폴리오 프로젝트\portfolio\projects\*.md`
- **HTML 모니터 셸(이 OS를 담을 그릇)**:
  `C:\Users\zxc98\Claude\Projects\취업 포트폴리오 프로젝트\portfolio\preview\monitor.html`
  (docs 프로젝트 세션이 관리. 화면 안 `.desktop` 영역이 이 프로젝트의 Unity WebGL 캔버스로 교체됨)

> 표기 원칙: 한국어 작성, 기술 용어(영문)는 그대로. 과장·허위 금지, 정량 수치 없으면 지어내지 말 것.

---

## 2. Git 구조 (fork + upstream)

- `origin` = **ThunderVolt45/Portfolio-OS** (Private) — 이 포트폴리오 OS
- `upstream` = **ThunderVolt45/UGUI-Window-Sample** — 창 프레임워크 원본
- **프레임워크 개선 가져오기 (CLI 전용!)**:
  ```bash
  git fetch upstream && git merge upstream/main
  ```
  ⚠️ GitHub Desktop은 `upstream`을 못 봄(이 repo는 GitHub상 fork가 아님) → **반드시 CLI**로 동기화.
  ⚠️ Windows LF/CRLF 노이즈로 merge가 막히면(내용 변화 없이 "수정됨"), 해당 파일을 `git checkout -- <경로>`로 되돌린 뒤 merge.
- MCP 개발 툴(`Packages/com.coplaydev.unity-mcp`)은 의도적으로 **vendoring**됨(원본 repo에서도 MCP 개발 예정) — 유지.

---

## 3. 창 프레임워크 API 요약 (재분석 불필요)

**창 생성** (`UGUIWindowManager`, namespace `UGUIWindow`):
```csharp
UGUIWindowManager.CreateWindow<T>(string name = null);
UGUIWindowManager.CreateWindowEx<T>(string name, int x, int y, int w, int h);
```
- **새 앱 창 = C# 클래스(`: UGUIWindow`) + 동명 프리팹** `Assets/Resources/Windows/<클래스명>.prefab`
  (매니저가 타입명으로 `Resources.Load("Windows/" + 타입명)` 하여 로드)
- 오브젝트 풀링: `useObjectPooling`, `allowMultipleInstance` 플래그로 제어. z-순서는 `DoublyLinkedList`로 관리.
- **데스크톱/아이콘/작업표시줄**: `UGUIDesktop.cs`(아이콘 재귀 수집 + `OnIconClicked`), `UGUIIcon`, `UGUIMenu`,
  **`UGUITaskBar`/`UGUITaskIcon`**(upstream 추가). `UGUIDesktop`은 `createDemoWindowsOnStart`(기본 false)로 데모 창 게이팅 → 빈 데스크톱 시작.
- **아이콘 스프라이트 = `UGUIWindow.windowIcon`**: 각 창 프리팹의 `windowIcon`을 지정하면 `UGUIIcon.ApplyTargetWindowIcon()`이
  데스크톱 아이콘 + 작업표시줄 아이콘에 **자동 적용**(아이콘에 스프라이트 수동 지정 불필요). 아이콘엔 `targetClassName`+라벨만 설정.
- 주요 스크립트: `Scripts/Base/`(UGUIWindow, UGUIWindowManager, Header/Border/Edge/Content/State/View, Cursor/Log),
  `Scripts/Sample/`, `Scripts/Utilities/DoublyLinkedList.cs`.
- **Unity 버전**: `main` 기준 `6000.3.19f1`(Unity 6.3)로 열 것 (README 뱃지는 stale).

---

## 4. 작업 계획 (Track B)

> **완료·진행 상황은 §6(진행 상황 로그)에서 최신 상태 확인.** 아래는 원래 계획.

**A. 부족한 기본 기능부터 추가** (샘플에 없음):
- 창 내부 콘텐츠 **스크롤**(ScrollRect 기반)
- **작업표시줄(taskbar)** — 열린/최소화된 창 관리, 클릭으로 복원

**B. 포트폴리오 앱 창 제작** (콘텐츠는 §1 index.md에서):
- `AboutWindow` — 한 줄 정체성 + 핵심역량 + 연락처
- `ProjectsWindow` — 프로젝트 7종(ProjectBlackout / Nova-Revolution / BrawlStarsTPS / 고아미 캠프 / Colyseus-Server-Sample / UGUI-Window-Sample / Gyeongseong97). 개별 창 또는 리스트+상세.
- `ResumeWindow`, `ContactWindow`(이메일·GitHub·전화)
- 데스크톱 아이콘 + 작업표시줄에서 열리도록 연결

**C. WebGL 빌드 & 배포** (GitHub Pages):
- Player Settings → **Compression Format = Disabled** *또는* **Decompression Fallback = Enabled** (GitHub Pages가 Content-Encoding 헤더를 못 넣으므로 필수)
- **싱글스레드 빌드**(SharedArrayBuffer/COOP·COEP 불가)
- 파일 1개 **100MB 하드 제한** — 빌드 경량 유지
- 배포 대상: `ThunderVolt45.github.io`(정적) — 모니터 셸 HTML + 이 WebGL 빌드. (해당 repo 아직 미생성)

---

## 5. Unity MCP 사용 메모

- 이미 설치·연결됨: 서버 `http://127.0.0.1:8080/mcp`, 상태 Connected. **작업 내내 Unity 에디터 열어둘 것.**
- MCP 툴로 GameObject/컴포넌트/스크립트/프리팹 생성, 콘솔 로그 읽기, 메뉴 실행, 빌드 가능.
- 픽셀 단위 UGUI 레이아웃 미세조정은 에디터 손이 더 편할 수 있음 — 스캐폴딩은 MCP, 마감은 에디터 병행.

---

## 6. 진행 상황 로그 (현재 컨텍스트)

> 세션 간 인계용. 새 세션은 §0~§5 통독 후 **여기서 최신 상태 파악**하고, 작업 후 이 섹션을 갱신할 것.

### 2026-07-06 기준 — 최근 작업

**upstream 3차 동기화 (`git merge upstream/main`, `edb7939..a64fde2`, 1커밋 → merge `d01dbac`)**:
`Fix: 스케일 환경에서 창 드래그 거리 보정`(a64fde2). `UGUIWindowManager`에 `GetPointerDeltaInRect(eventData, relativeTo)` 헬퍼 추가(+35, 순수 추가) + Border/Edge/Header.cs 드래그 델타 계산을 이 헬퍼로 교체, docs 갱신.
- **충돌 0**(ort auto-merge) — 씬·프리팹 미변경이라 이번엔 Smart Merge 불필요. 병합 전 미커밋 폰트 SDF 7개만 `git checkout --`로 되돌림.
- 내가 구독하는 이벤트 4종(`OnManagedWindowOpened/Focused/Minimized/Closed`) + `CreateWindow/Ex` 시그니처 불변 확인 → PDF 오버레이 코드 영향 없음. (에디터 미기동으로 MCP 컴파일 검증은 생략, 정적 확인.)

**포트폴리오 창 프리팹을 Prefab Variant로 재생성 + ProjectBlackout 제거 (미커밋)**:
- **AboutWindow / DocumentViewerWindow 프리팹을 base `UGUIWindow.prefab`(guid `23a3495e…`)의 Prefab Variant로 재생성.** 기존엔 GUID 교체 독립 복제본이라 upstream 창 구조 개선(스크롤 등)을 못 받았음 → 변형으로 전환해 자동 상속.
  - **핵심 난점**: Unity는 변형에서 컴포넌트 m_Script 교체 불가 → **에디터 스크립트**(`Assets/Editor/PortfolioPrefabTools.cs`, 메뉴 **Portfolio → Rebuild Window Prefab Variants**)로 우회: base 인스턴스화 → root의 `UGUIWindow` 컴포넌트 제거 + subclass(About/Doc) 추가 + `SerializedObject`로 설정필드 복사(RequireComponent은 `UGUIWindowView`만이라 안전, base 컴포넌트 역참조 0 확인) → `SaveAsPrefabAsset`으로 Variant 저장.
  - base 창 body는 `Content/Viewport/ScrollContent` 구조(ScrollRect). 데모 텍스트 5개 제거하고 단일 `ContentText`(TMP, stretch)를 ScrollContent에 배선 → About은 `contentText` 필드 연결, Doc은 `documentRelativePath="docs/resume.pdf"` + 런타임 status text. 아이콘은 About.png/Resume.png.
  - **검증**: `isVariant=True`, `m_SourcePrefab`=base guid, root 컴포넌트=`UGUIWindowView`+subclass(base UGUIWindow 잔존 0). 플레이모드 스모크: `CreateWindow(typeof(AboutWindow/DocumentViewerWindow))` → 각각 정확한 타입 반환, 콘솔 에러/경고 0.
- **ProjectBlackout(전용 창) 완전 제거**: `ProjectBlackoutWindow.prefab`(+meta), `Assets/Scripts/Portfolio/ProjectBlackoutWindow.cs`(+meta) `git rm`, 씬의 `Icon_ProjectBlackout` GameObject 삭제 후 씬 저장. 프리팹 GUID/클래스 외부 참조 0 확인 → 컴파일 에러 없음. (ProjectBlackout은 아래 PDF 문서 뷰어로 대체됨.)

**PDF별 전용 문서 창 + 데스크톱 아이콘 3종 (미커밋)**:
- 매니저가 **타입명으로 프리팹을 로드**(`Resources.Load("Windows/"+typeName)`)하므로 PDF별 구분엔 별도 타입 필수 → **`DocumentViewerWindow`를 상속한 얇은 서브클래스 3개**: `BrawlStarsTPSDocWindow`/`NovaRevolutionDocWindow`/`ProjectBlackoutDocWindow`(모두 namespace `UGUIWindow`, `Assets/Scripts/Portfolio/`). base를 리팩터링해 `protected virtual string DocumentPath/DocumentTitle` override 지점 신설(서브클래스는 경로·제목만 지정).
- 각 서브클래스용 **Variant 프리팹**을 base `UGUIWindow.prefab`에서 생성(에디터툴 메뉴 **Portfolio → Build PDF Doc Windows + Icons**). **스프라이트(windowIcon)는 미지정**(iconPath=null → windowIcon=null) — 사용자가 추후 직접 할당. UGUIIcon.ApplyTargetWindowIcon은 windowIcon null이면 스킵하므로 아이콘 이미지는 빈 상태(기존 About/Resume 아이콘도 sprite null이라 시각적 일관).
- **씬 아이콘 3개**를 `UGUI_Desktop/IconGrid`에 `Icon_About` 복제로 추가(targetClassName+라벨+anchoredPosition만 변경). 세로 열: About(50,-60)/Resume(50,-170) 뒤로 (50,-280)/(50,-390)/(50,-500). 라벨: "BrawlStars/TPS", "Nova/Revolution", "Project/Blackout".
- **PDF 실파일 3종을 `Assets/StreamingAssets/docs/`로 ASCII명 복사**: `brawlstarstps.pdf`(3.3MB)/`novarevolution.pdf`(3.2MB)/`projectblackout.pdf`(2.6MB) — SSOT는 docs 프로젝트 `portfolio/projects/*.pdf`. `DocumentPath`가 각각 `docs/<name>.pdf` 반환(viewerUrl `../../docs/...` 규칙 유지).
- **검증**: 5개 변형 모두 `isVariant=True`. 플레이모드 스모크에서 `CreateWindow(typeof(각 서브클래스))` → 정확한 타입 반환, 콘솔 에러/경고 0. 씬 아이콘 5개 targetClassName 확인(About/DocumentViewer/BrawlStarsTPS/Nova/ProjectBlackout).
- **⚠️ 남은 것**: ①아이콘 스프라이트는 미지정(사용자 할당 예정) — 프리팹 windowIcon에 넣으면 데스크톱+작업표시줄 자동 적용. ②PDF 실제 렌더는 WebGL 전용(에디터는 안내 텍스트) → release 빌드에서 각 아이콘→해당 PDF 눈확인 필요. ③배포 크기 +~9MB(여전히 <100MB 예상, 재빌드 후 확인).
- **⚠️ 미커밋**: 위 전부(About/Doc 변형 2 + ProjectBlackout 4 삭제 + 서브클래스 3 + 변형프리팹 3 + PDF 3 + 씬 + DocumentViewerWindow.cs + 에디터툴) 아직 커밋 안 함. 폰트 SDF 노이즈는 매번 `git checkout --`로 되돌림.

**다중 PDF 창 오버레이 — 2단계 수정 (커밋됨/미커밋)**:
- **1차(같은 문서만 보임) — 커밋 `2494c1e`**: 원인은 `PdfOverlayInit`이 `if (window.__pdfOverlay) return;`로 첫 문서 URL 고정한 단일 iframe만 생성. 1차 수정은 단일 오버레이 src를 포커스 창 문서로 전환(`PdfOverlaySetSrc`)+`static s_live`로 전환 직전 이전 라이브 창 동기 스냅샷. → **그러나 문서 전환마다 pdf.js가 리로드(스크롤·검색 상태 소실)되는 후속 문제 발생.**
- **2차(포커스 전환 시 리로드) — 미커밋, 최종**: **창마다 자기 전용 iframe을 1개씩 만들어 살려 둔다**(파괴 안 함, `id`=`GetType().Name`로 키잉). 포커스/최상단 창만 자기 iframe 표시(`PdfOverlayShow(id)`), 백그라운드로 밀리면 자기 iframe을 스냅샷으로 굳히고 숨김(`PdfOverlayHide(id)`). 다시 라이브가 되면 숨김만 해제 → **리로드/상태소실 없음**. `PdfOverlaySetSrc`·`s_live` 제거.
  - **핵심**: 각 창이 **자기** iframe만 스냅샷하므로 1차의 리스너-순서 경합이 사라짐(교차오염 불가).
  - **Open은 포커스 이벤트 미발생**(`UGUIWindow.Open()`은 `OnOpenWindow`만) → `OnManagedWindowOpened`**도** 구독해야 새 PDF 창이 열릴 때 기존 라이브 창이 백그라운드로 내려감. `OnAnyWindowActivated`가 Opened+Focused 공용 핸들러.
  - jslib는 `window.__pdfOverlays[id] = {wrap,iframe,url}` 딕셔너리, 모든 함수 첫 인자 `id`.
- **검증**: 사용자 수동 WebGL 빌드에서 다중 PDF 열기 + 문서 간 포커스 전환 정상(리로드 없음) 확인. (에디터 MCP 컴파일 검증은 에디터 미기동으로 생략 — 정적으로 심볼 정합만 확인.)

**upstream 2차 동기화 (`git merge upstream/main`, `4c36133..edb7939`, 2커밋 → merge `cc5485c`)** — 창 전환 오버레이 반영:
`Feat: 창 전환 오버레이 추가`(a1bf9f5) + `Feat: 창 전환 오버레이 프리팹 관리 개선`(edb7939). 신규 `UGUIWindowSwitcher.cs`/프리팹 추가, `UGUIWindowManager.cs`(+197)·`UGUIWindow.cs`(+3) 수정, docs 갱신.
- **씬만 충돌 → 1차와 동일하게 Unity Smart Merge**(`UnityYAMLMerge.exe merge -p <base> <theirs> <ours> <out>`, 인덱스 `:1/:3/:2`)로 fileID 병합 → **내 아이콘 3개(About/ProjectBlackout/DocumentViewer) 전부 보존**, upstream 요소 유지, 충돌 마커 0. 스크립트/프리팹은 auto-merge.
- 병합 전 미커밋 폰트 SDF 7개는 `git checkout --`로 되돌림. `ProjectSettings.preloadedAssets`(InputSystem) 노이즈는 이번에도 스테이징 제외(병합이 안 건드림). productName 영향 없음.
- **컴파일 검증됨(MCP)**: `refresh_unity(force/compile)` 후 `read_console` 에러 0건. 내가 구독하는 `UGUIWindowManager.OnManagedWindowFocused/Minimized/Closed` 이벤트 + `CreateWindow/CreateWindowEx` 시그니처 불변 확인.
- ⚠️ 남은 것: 신규 `UGUIWindowSwitcher`(창 전환 오버레이)를 포트폴리오에서 실제로 쓸지/동작 확인은 미검증. 내 PDF 포커스-스왑 오버레이와 상호작용 여부도 점검 후보.

**upstream 1차 동기화 (`git merge upstream/main`, → 4c36133, 7커밋)** — 프레임워크 개선 반영:
`Feat: 창 본문 스크롤 처리 추가`(§4-A 본문 스크롤!), 작업표시줄/최대화 영역 복구 fix, 샘플 디자인 수정 등.
- **씬 충돌은 Unity Smart Merge로 해결**: `UnityYAMLMerge.exe merge -p <base> <theirs> <ours> <out>`(git 인덱스 `:1/:3/:2` 추출) → fileID 기준 병합으로 **내 아이콘 3개 + upstream InputSystem_Actions 블록 모두 보존**(손 병합 금지, 이 도구 쓸 것). 프리팹/스크립트는 auto-merge.
- 병합 전 미커밋 폰트 SDF 7개(`_typelessdata` 아틀라스 노이즈)는 `git checkout --`로 되돌린 뒤 merge(§2). productName=Portfolio-OS 유지 확인.
- ⚠️ **병합 후 Unity 에디터에서 컴파일 검증 필요**(MCP 다운 상태로 정적 확인만 함 — 내가 쓰는 `UGUIWindow` API 시그니처는 불변 확인).

**빌드 파이프라인 + PDF 오버레이 하니스 커밋됨** (f70d278, 10379d3) — 아래 상세.

**PDF 포커스-스왑 오버레이 Unity 통합 — 완료 & WebGL 실빌드 검증** (c0efc8c):
- `DocumentViewerWindow` 재작성: 포커스 시 실제 pdf.js viewer(iframe)를 창 위에 좌표동기 오버레이(`Assets/Plugins/WebGL/PdfOverlay.jslib`)로 띄워 **선택·검색·폼·하이퍼링크 native**, 백그라운드 시 page canvas 스냅샷을 텍스처로 굳혀 RawImage 표시(z-order 정상). `UGUIWindowManager.OnManagedWindowFocused/Minimized/Closed` 구독으로 스왑.
- 좌표매핑: 콘텐츠 RectTransform world corners → `RectTransformUtility.WorldToScreenPoint`(Unity px) → jslib에서 `canvas.getBoundingClientRect()`+버퍼크기로 CSS 변환·Y뒤집기·캔버스 경계 클리핑.
- pdf.js **정식 viewer(3.11.174)를 `Assets/StreamingAssets/pdfjs/{build,web}`에 추가**(dev 잔여 제거, ~9MB). release 빌드 60MB(여전히 <100MB).
- `PortfolioBootstrap.cs`: URL 해시 `#open=클래스명` 딥링크로 특정 앱 자동 오픈(테스트+monitor.html 딥링크). `Type.GetType` 방식이라 스트리핑 안전(아이콘과 동일).
- 검증(release 브라우저): 딥링크 오픈→좌표동기 정합, resume 렌더, 뷰포트 리사이즈 재계산·클리핑, 타 창 포커스 시 스냅샷 스왑(overlay display:none) 확인. JS 에러 0.
- ⚠️ **남은 것**: ①복귀(백그라운드→라이브) 경로는 대칭 로직이나 헤드리스 preview에서 클릭검증 못 함 — 실브라우저 눈확인 권장. ②포커스아웃 시 스냅샷이 async라 도착 전 짧은 플래시 가능. ③**monitor.html 셸**(스케일/오프셋 캔버스) 좌표 정합 검증 필요(docs 세션 조율). ④다중 DocumentViewerWindow는 단일 오버레이 PoC.
- ⚠️ **미커밋 곁가지**: 폰트 SDF 7개(동적 아틀라스에 한글 글리프 구워짐, 1392줄) + `ProjectSettings.preloadedAssets`(InputSystem 자동추가) — PDF와 무관해 제외. 별도 처리 필요. 또한 일부 TMP 텍스트에서 한글 글리프 미스 경고(□) 관측 — 폰트 폴백 재점검 후보.

### 2026-07-03 기준 — 완료 & 검증됨

**포트폴리오 앱 창 (§4-B) — 부분 완료** (모두 `Assets/Scripts/Portfolio/`, 프리팹은 `Assets/Resources/Windows/`)
- `AboutWindow`, `ProjectBlackoutWindow` — `UGUIWindow` 상속, 콘텐츠는 index.md SSOT 인용.
- `DocumentViewerWindow` + `PdfJsBridge`(.cs + `Assets/Plugins/WebGL/PdfJsBridge.jslib`) — PDF.js로 PDF를 창 안에 렌더(아래 PoC).
- 프리팹은 샘플 프리팹을 **스크립트 GUID 교체**로 복제해 생성. 아이콘 이미지: `Assets/Portfolio/Icons/{About,ProjectBlackout,Resume}.png`(System.Drawing 생성).
- ⚠️ **창 초기화(콘텐츠·크기)는 `Start()`가 아니라 `OnEnable()`에서.** `execute_code`로 만든 오브젝트는 Start가 호출 안 되고, 매니저가 Instantiate 직후 제목을 클래스명으로 덮어씀.

**데스크톱 / 아이콘 / 작업표시줄** — §3 참조. 씬에 About/ProjectBlackout/Resume 아이콘 배치 완료, 샘플 아이콘 제거. 작업표시줄에 창 아이콘 자동 표시 확인.

**한글 폰트** — 기본 TMP(LiberationSans)에 한글 없어 □로 깨졌음 → upstream **WantedSans 동적 SDF**를 **TMP Settings 전역 폴백**(`m_fallbackFontAssets`)에 추가해 해결. 창 텍스트 한글 정상 렌더 확인.

**WebGL 빌드 & PDF.js PoC — 실빌드+브라우저 검증 완료**
- WebGL Build Support 모듈 설치됨. `PlayerSettings.WebGL.compressionFormat = Disabled`, `productName = Portfolio-OS`.
- 파이프라인: PDF.js(v3.11.174, `Assets/StreamingAssets/pdfjs/`) 브라우저 로드 → 페이지 렌더 → base64 PNG → `SendMessage` → C# `Texture2D` → 창 RawImage. 테스트 문서 `Assets/StreamingAssets/docs/resume.pdf`.
- ⚠️ **development 빌드는 wasm ~101MB로 GH Pages 100MB 초과** → 배포는 **release 빌드 + IL2CPP 코드 스트리핑** 필요. `Build/`은 .gitignore 대상.

**WebGL 빌드 파이프라인 정비 — 완료 & 검증됨 (반복 검증용)**
- **재현 빌드 스크립트** `Assets/Editor/PortfolioBuild.cs`(`PortfolioOS.EditorTools.PortfolioBuild`): 배포 설정을 코드로 고정(압축 Disabled·싱글스레드·linker Wasm). 메뉴 **Portfolio → Build WebGL (Release/Development)** + 배치모드(`-executeMethod …BuildWebGLRelease`).
  - release = **High 매니지드 스트리핑 + IL2CPP `Release`**(Master는 빌드 너무 느려 제외). dev = Minimal/Debug.
- **결과**: release wasm **37MB**, 전체 배포 **52MB**(모든 파일 <100MB) → **GH Pages 배포 가능**. 빌드 ~4.3분. 브라우저 런타임 정상 초기화(스트리핑 파손 없음) 확인.
- **로컬 서버** `Tools/serve.py`(wasm MIME=application/wasm, no-store) + **preview MCP** `.claude/launch.json`의 `webgl` 컨피그(port 8000). 브라우저 실구동 검증됨.
- ⚠️ 빌드는 메인스레드 동기라 **빌드 내내 MCP 브리지 끊김**(정상) → 완료는 `Build/WebGL/Build/WebGL.wasm` mtime 폴링/콘솔 로그로 확인. preview 브라우저는 SwiftShader라 셰이더 에러 로그·`preview_screenshot` 타임아웃 정상 → 검증은 `preview_console_logs`로.

### 남은 작업 (다음 세션 후보)
- [ ] `ProjectsWindow`(7종 리스트+상세), `ContactWindow` 등 나머지 앱 창
- [ ] `DocumentViewerWindow` 다페이지 스크롤 + HTTP Range 요청 + 텍스처 가상화 + "PDF 원본 다운로드" 버튼
- [~] 창 내부 콘텐츠 스크롤(ScrollRect) — §4-A: **upstream `Feat: 창 본문 스크롤 처리 추가`로 프레임워크에 들어옴**(2026-07-06 병합). 포트폴리오 창에 실제 적용/동작 확인 필요.
- [x] **release WebGL 빌드**로 배포 크기 최적화 (52MB, 위 참조) — 남은 건 `ThunderVolt45.github.io` 배포(모니터 셸에 임베드)
- [x] **PDF 하이퍼링크 대응** 단계 1(포커스-스왑 오버레이) — WebGL 통합·검증 완료(c0efc8c, 위 상세). 남은 refinement는 §6 ⚠️ 참조(복귀경로 눈확인·플래시·셸 정합·다중창).

### 알려진 함정
- Unity MCP 브리지가 도메인 리로드/플랫폼 전환/서버 재시작 때 자주 끊김 → `manage_editor(telemetry_status)`로 재확인 후 **`set_active_instance`로 인스턴스 재고정**(외부 git 변경 후 AssetDatabase가 stale하면 프리팹이 "없다"고 나올 수 있음 → 강제 refresh).
- `execute_code`는 **C# 6(codedom)** — 튜플/문자열 보간/`using` 지시문 불가. 완전한 네임스페이스로 작성.
- upstream 병합 시 `ProjectSettings.asset`이 productName 등을 덮어쓸 수 있음 → 병합 후 확인·복구.

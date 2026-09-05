# TASKS — Portfolio-OS (Epic / Task 보드)

> **Epic**(큰 목표·딜리버러블) 아래 **Task**(실행 단위)로 작업을 관리한다. 날짜 로그가 아니라 영역별 보드.
> 새 세션은 `AGENTS.md`(프로젝트 규칙·아키텍처·검증 레퍼런스)를 먼저 읽고, 현황은 이 문서에서 파악·갱신한다.
>
> **상태**: ✅ Done · 🔄 In Progress · ⬜ Todo · 🔒 Blocked(외부 의존)
> **ID 규칙**: Epic = `E<n>`, Task = `E<n>-T<m>`. 완료 Task도 이력 참조용으로 남겨둔다.
> **갱신 규칙**: 작업 후 해당 Task 상태 + (필요 시) 구현 노트만 고친다. 새 작업은 Task를 추가한다.

## 🎯 목표 시나리오 (North Star)

방문자가 경험하는 최종 흐름. 모든 Epic/Task는 이 시나리오를 실현하기 위한 것.

1. **접속** — GitHub Pages 랜딩 페이지에서 Unity WebGL 캔버스가 **페이지 전체(뷰포트)를 차지**한다.
   - ⚠️ **모니터 셸(`monitor.html`) 구상은 폐기**(2026-07-16). 셸 없이 Unity가 페이지 전체를 쓰는 방식으로 확정 → 커스텀 템플릿 `Assets/WebGLTemplates/PortfolioFull`.
2. **부팅 연출** — 기본 Unity WebGL 로딩 화면을 **부팅 연출로 대체**(커스텀 WebGL 템플릿).
   - 부팅 진행은 `createUnityInstance(..., onProgress)`의 **실제 로드 progress**로 구동 → ~30MB 다운로드/초기화 시간을 부팅 연출이 그대로 가린다(진행 막대 노출 없음).
   - **부팅 연출은 템플릿에서 완결**되고, Unity ready 시 데스크톱으로 **매끄럽게 핸드오프**(플래시/점프 없이). Unity 쪽 연장 연출은 없음.
3. **부팅 완료 → 3창 자동 오픈**: 자기소개(About) · Contact · **Projects 런처**. (겹침 방지 cascade 배치.)
4. **데스크톱 사용법 학습** — 바탕화면의 사용 방법 텍스트를 읽고 조작 학습(아이콘 실행, 창 드래그/최소화/닫기, 작업표시줄 복원 등).
5. **데스크톱 아이콘으로 콘텐츠 접근** — 아이콘 **4개**로 집약:
   - **About**(자기소개) · **Contact** · **Resume**(안내 창 → 브라우저 PDF)
   - **Projects 런처 창** — 프로젝트 **7종**을 리스트+상세로 집약, 항목 선택 시 라우팅:
     - 문서형: BrawlStarsTPS · Nova-Revolution · ProjectBlackout → 프로젝트별 UGUI 소개 창 → 자세히 보기로 브라우저 PDF(E3)
     - UGUI형: UGUI-Window-Sample → 전용 UGUI 소개 창(PDF 미사용)
     - 그 외: 고아미 캠프 · Colyseus-Server-Sample · Gyeongseong97 → 런처 내 상세(SSOT 기반)
6. **모바일 / WebGL 미지원 폴백** — 정적 안내 + 핵심 링크(이력서·연락처·GitHub) 페이지 제공. 단, **WebGL 구동 가능한 모바일은 "그래도 입장" 허용**.

## Epic 개요

| Epic | 제목 | 상태 | 다음 액션 |
|---|---|---|---|
| **E1** | 창 프레임워크 유지 (upstream 동기화) | ✅ 최신 | `UGUIWindowSwitcher` 실사용 점검 (E1-T2) |
| **E2** | 포트폴리오 앱 창 & 콘텐츠 | 🔄 진행 | Projects 런처 창 (E2-T4) |
| **E3** | PDF 문서 열기 (브라우저 기본 뷰어) | 🔄 핵심완결 | release WebGL에서 새 탭 동작 눈확인 (E3-T8) |
| **E4** | WebGL 빌드 & 배포 | ✅ **배포됨** | 모바일/미지원 폴백 재설계 (E4-T5) |
| **E5** | 폴리시 & 위생 | 🔄 진행 | 폰트 SDF 노이즈 방침 (E5-T4) |
| **E6** | 부팅 & 데스크톱 셸 연출 | ⬜ 미착수 | 커스텀 WebGL 템플릿 부팅 (E6-T1) |

> **프로젝트 최종 목표 = E4-T4 → 달성**(2026-07-16). 공개 발행: **https://thundervolt45.github.io/Portfolio-OS/**
> 모니터 셸 임베드 대신 **Unity가 페이지 전체를 쓰는 방식**으로 확정했고, 별도 `ThunderVolt45.github.io` repo 없이
> **이 저장소의 `gh-pages` 브랜치**로 배포한다(저장소는 public 전환됨). 재배포는 `python Tools/deploy_ghpages.py`.

---

## E1 — 창 프레임워크 유지 (upstream 동기화) · ✅ 최신

> 목표: `upstream`(ThunderVolt45/UGUI-Window-Sample)의 프레임워크 개선을 주기적으로 병합해 최신 유지.

| ID | Task | 상태 |
|---|---|---|
| E1-T1 | upstream 1~3차 동기화 반영 (본문 스크롤·작업표시줄 복구·창 전환 오버레이·스케일 드래그 보정) | ✅ |
| E1-T2 | 신규 `UGUIWindowSwitcher`(창 전환 오버레이) 포트폴리오 실사용 점검 | ⬜ |

**들어온 upstream 기능** — 창 본문 스크롤(ScrollRect) · 작업표시줄/최대화 영역 복구 fix · 창 전환 오버레이 `UGUIWindowSwitcher.cs`(`a1bf9f5`,`edb7939`) · 스케일 환경 드래그 보정 `UGUIWindowManager.GetPointerDeltaInRect(eventData, relativeTo)`(`a64fde2`).

**구현 노트 — 병합 절차(중요)**
- 씬 충돌은 **손 병합 금지**, Unity Smart Merge 사용: `UnityYAMLMerge.exe merge -p <base> <theirs> <ours> <out>` (git 인덱스 `:1`=base / `:3`=theirs / `:2`=ours 추출). fileID 기준 병합이라 내 아이콘 + upstream 요소 모두 보존. 스크립트/프리팹은 auto-merge.
- 병합 전 폰트 SDF 노이즈 7개(E5-T4)는 `git checkout --`로 되돌린 뒤 merge.
- 내가 구독하는 이벤트 `OnManagedWindowOpened/Focused/Minimized/Closed` + `CreateWindow/CreateWindowEx` 시그니처는 3차까지 **불변** 확인.

---

## E2 — 포트폴리오 앱 창 & 콘텐츠 · 🔄 진행

> 목표: About·Contact·Resume + Projects 런처(7종)를 앱 창으로 구현. 모두 `Assets/Scripts/Portfolio/`(namespace `UGUIWindow`), 프리팹 `Assets/Resources/Windows/<클래스명>.prefab`. 콘텐츠 SSOT는 `AGENTS.md` §1의 외부 `portfolio/index.md`.

| ID | Task | 상태 |
|---|---|---|
| E2-T1 | `AboutWindow` — 이력서 1페이지 전체(소개·핵심역량·경력·학력/자격/교육·프로필·슬로건)를 구조화 UGUI 레이아웃으로 구현 | ✅ |
| E2-T2 | 초기 `DocumentViewerWindow` + PDF별 서브클래스 3의 PDF.js 내장 뷰어 구현 (E2-T9/E3-T7에서 폐기) | ✅ |
| E2-T3 | 구 `ProjectBlackoutWindow`(전용 창) 제거 → 초기 PDF 뷰어로 대체 (E2-T9에서 소개 창으로 재전환) | ✅ |
| E2-T4 | **`ProjectsWindow`(런처)** — 7종 리스트+상세, 항목 선택 시 라우팅(프로젝트 소개 창 / UGUI 창 / 런처 내 상세) | ⬜ |
| E2-T5 | `ContactWindow` — 이메일·GitHub(공개 개인정보 정책 준수) | ⬜ |
| E2-T6 | 창 본문 스크롤(E1 프레임워크 반영분) 포트폴리오 창에 실제 적용/동작 확인 | 🔄 |
| E2-T7 | `UGUISampleWindow` — UGUI-Window-Sample 소개 전용 창(PDF 미사용, UGUI만). GitHub repo 링크 포함 | ✅ |
| E2-T8 | 데스크톱 아이콘 집약 → **About·Contact·Resume·Projects 4개**. 개별 PDF 아이콘 3개는 Projects 런처로 이동(제거) | ⬜ |
| E2-T9 | 문서형 프로젝트 3종을 `UGUISampleWindow` 스타일의 소개 창으로 전환 + 프로젝트별 팔레트 + 브라우저 PDF CTA | ✅ |

> **E2-T4 프로젝트 7종 라우팅**
> - 문서형(소개 창 → 브라우저 PDF): ProjectBlackout · Nova-Revolution · BrawlStarsTPS
> - UGUI형(전용 창, E2-T7): UGUI-Window-Sample
> - 런처 내 상세(SSOT 텍스트): 고아미 캠프 · Colyseus-Server-Sample · Gyeongseong97

**구현 노트 — 프리팹 = Prefab Variant (필수 패턴)**
- 창 프리팹은 base `UGUIWindow.prefab`(guid `23a3495e…`)의 **Variant**로 만든다(GUID 교체 독립복제 금지: upstream 창 개선을 못 받음).
- Unity는 Variant에서 컴포넌트 `m_Script` 교체 불가 → 에디터툴 `Assets/Editor/PortfolioPrefabTools.cs`로 우회:
  - 메뉴 **Portfolio → Rebuild Window Prefab Variants** (전체 포트폴리오 창)
  - 메뉴 **Portfolio → Build Project Introduction Windows + Icons** (문서형 프로젝트 소개 창 3 + 씬 아이콘)
  - 동작: base 인스턴스화 → root `UGUIWindow` 컴포넌트 제거 + subclass 추가 → `SerializedObject`로 필드 복사 → `SaveAsPrefabAsset`. 검증: `isVariant=True`, root 컴포넌트 = `UGUIWindowView` + subclass.
- base body는 `Content/Viewport/ScrollContent`(ScrollRect). About·UGUISample·프로젝트 소개·Resume 안내 창은 모두 에디터 빌더가 구조화 레이아웃을 굽는다.

**구현 노트 — AboutWindow 콘텐츠(이력서 1페이지)**
- 디자인 기반: HTML/CSS 시안 `scratchpad/about-mockup.html`(macOS 프로필 카드 톤, 라이트 테마) → UGUI로 이식. 콘텐츠 SSOT는 `C:\Users\zxc98\Documents\GitHub\-\김민영_이력서_2026.pptx`의 **1페이지**(markitdown으로 추출).
- 빌드 툴: `Assets/Editor/PortfolioAboutContent.cs`, 메뉴 **Portfolio → Build About Content**. `PrefabUtility.LoadPrefabContents`로 변형 유지한 채 `ScrollContent` 하위를 재구축(Vertical/HorizontalLayoutGroup + `ContentSizeFitter`로 세로 성장). `RebuildVariants`도 마지막에 이 Build를 호출(스켈레톤 재생성 시 콘텐츠 자동 복원).
- 폰트: **WantedSans SDF** 가중치별(ExtraBold 이름 / Bold 회사 / SemiBold 헤더·라벨 / Medium 값 / Regular 본문). 카드 라운드/원형 아바타·dot는 **MPUIKit `MPImage`**(SerializedObject로 `m_DrawShape`=Rectangle/Circle, 사각형 `m_Rectangle.m_CornerRadius`, 원형 `m_Circle.m_FitRadius`=bool). ⚠️ `m_FitRadius`는 float 아님(bool) — floatValue 쓰면 "type is not a supported float value" 경고.
- 창 크기: `AboutWindow.OnEnable`이 `Resize(480,580)`(가로 스크롤 없이 넉넉, 세로만 스크롤). `contentText` 필드/텍스트 주입 제거.
- 검증(play mode 스크린샷): 7개 섹션 전부 렌더 + 한글 글리프 정상 + 무경고 확인. 폰트 동적 SDF 노이즈(5종)는 E5-T4 방침대로 `git checkout --`로 되돌림(런타임 재베이크).
- `ExternalPdfWindow`가 `OnEnable()`에서 크기 초기화·`DetailsButton` 배선·`Application.OpenURL` 호출을 공통 처리한다. `DocumentViewerWindow`는 Resume 아이콘·`#open` 호환을 위해 역사적 타입명을 유지한 이력서 안내 창이며, 프로젝트 3종의 기존 `*DocWindow` 타입명도 씬 아이콘/딥링크 호환을 위해 유지한다.

**구현 노트 — 프로젝트 소개 창(E2-T9)**
- 콘텐츠 빌더 `Assets/Editor/PortfolioProjectContent.cs`: Hero · 핵심 포커스 · 소개 · 첫 화면 CTA · 기본 정보 · 대표 구현 3선을 `UGUISampleWindow`와 같은 macOS 라이트 톤/MPUIKit 카드/WantedSans 계층으로 구성한다.
- 팔레트: BrawlStarsTPS = warm amber, Nova-Revolution = orbital blue, Project Blackout = raid red. 각 프로젝트의 기존 스프라이트를 Hero·데스크톱·Dock에 공통 사용하며, Project Blackout은 최신 금속성 `B` 로고(`T_Blackout_Icon_B_Transparent.png`)를 사용한다.
- `DetailsButton`은 `Assets/StreamingAssets/docs/*.pdf`를 브라우저 새 탭으로 연다. PDF 원본은 유지하되 Unity 내부 PDF 렌더러·iframe·스냅샷은 사용하지 않는다.
- Unity CLI `eval`로 4개 프리팹이 모두 Variant이며 루트 타입과 `DetailsButton`이 정상임을 검증. Play Mode에서 프로젝트 3종·Resume 렌더, 스크롤 하단, 버튼 활성, 콘솔 오류 0건 확인.

**구현 노트 — UGUISampleWindow 콘텐츠(E2-T7, 프레임워크 자기소개)**
- 창 프레임워크(UGUI-Window-Sample) **자체를 소개하는 메타 창** — "지금 이 창이 곧 그 프레임워크"라는 dogfooding 콜아웃을 상단에 배치(핵심 어필 포인트). PDF 미사용, 순수 UGUI.
- 디자인 기반: HTML/CSS 시안 `scratchpad/ugui-sample-mockup.html`(About과 동일 macOS 라이트 톤 + 다크 코드블록·앱 아이콘·번호 배지). 콘텐츠 SSOT: `C:\Users\zxc98\Documents\GitHub\-\portfolio\projects\ugui-window-sample.md`(+ `-deck.md`). 이전 SSOT 위치는 더 이상 사용하지 않으며 `Documents\GitHub\-\portfolio\`가 현 원천이다.
- 파일: 창 클래스 `Assets/Scripts/Portfolio/UGUISampleWindow.cs`(`OnEnable`에서 `Resize(500,620)`, `Start`에서 제목 "UGUI-Window-Sample"). 콘텐츠 빌더 `Assets/Editor/PortfolioUGUISampleContent.cs`(메뉴 **Portfolio → Build UGUISample Content**). 프리팹 스켈레톤 = `PortfolioPrefabTools`에 등록: 단독 메뉴 **Portfolio → Build UGUISample Window**(변형+콘텐츠) + `RebuildVariants`에도 편입(전체 재빌드 시 자동 복원).
- 섹션 10종: Hero(앱 아이콘) · dogfood 콜아웃 · 소개+코드블록 · 기본정보 · 핵심특징(6) · 아키텍처 3선 · 강조구현 3 · 엔지니어링 디테일 · 링크(GitHub repo URL 포함) · 슬로건. 폰트 전부 **WantedSans SDF** 가중치별, 카드는 MPUIKit `MPImage`.
- **아이콘 일관성(수정)**: (1) Content **Hero 아이콘 = 데스크톱/작업표시줄과 동일한 스프라이트**(`Icons/UGUISample.png`)를 `Image`로 그대로 사용 — 예전엔 UGUI 도형으로 따로 그려 달라 보였음. (2) **핵심특징 아이콘 = HTML 시안 글리프**(⌘◱♻⿴⤢⚙)를 스프라이트로 재현. ⚠️ 이 심볼들은 **WantedSans/폴백에 없어 TMP 텍스트로는 □**(play mode로 확인) → 심볼폰트(Segoe UI Symbol / ⿴만 SimSun)로 **흰색 모노크롬 PNG를 구워** `Assets/Portfolio/Icons/Features/feat_*.png`(Sprite)로 두고 `Image.color`로 액센트 틴트. 생성 스크립트 `scratchpad/gen_feature_glyphs.py`(PIL+fontTools). 폰트 파일은 미배포(픽셀만).
- ⚠️ 함정(해결): `Hlg` 헬퍼는 `childControlHeight`를 항상 true로 강제(controlH 인자 무시) → 아이콘 타일 행에서 이를 false로 **덮어쓰면** LayoutElement 고정 높이가 무시되어 아이콘이 세로로 늘어남. 콜아웃/피처 행은 override 제거(true 유지)+`childForceExpandHeight=false`+상단정렬로 해결.
- 검증(play mode 스크린샷): 10개 섹션 전부 렌더 + 한글/기호(↔) 글리프 정상 + 무경고 확인. Projects 런처 라우팅(E2-T4/T8)은 미결(별도 Task).
- **아이콘(완료)**: `Assets/Portfolio/Icons/UGUISample.png`(256², 파란→인디고 그라디언트 타일 + 겹친 창 글리프 — About/ProjectBlackout와 동일 플랫 스타일, Sprite import) 생성 → PrefabTools에서 `windowIcon`으로 배선(iconPath 지정). 시안 스크립트 `scratchpad/gen_icon.py`(PIL). 데스크톱 아이콘 `UGUI_Desktop/IconGrid/Icon_UGUISample`(targetClassName=`UGUISampleWindow`, 라벨 "UGUI-Window\nSample", anchoredPosition (50,-610)) 씬에 추가·저장. `UGUIIcon.ApplyTargetWindowIcon`이 데스크톱+작업표시줄(Dock)에 스프라이트 자동 적용 확인. 아이콘 더블클릭(→`OpenWindow`) 창 오픈 end-to-end 검증.

**구현 노트 — 함정**
- ⚠️ 창 초기화(콘텐츠·크기)는 `Start()`가 아니라 **`OnEnable()`**에서. `execute_code`로 만든 오브젝트는 Start가 안 뜨고, 매니저가 Instantiate 직후 제목을 클래스명으로 덮어씀.
- E2-T8 데스크톱 아이콘: 씬 `UGUI_Desktop/IconGrid`에 `Icon_About` 복제로 추가, `targetClassName`+라벨+`anchoredPosition`만 변경. (현재 개별 PDF 아이콘 3개가 배치돼 있으나 런처 집약으로 제거 예정.)

---

## E3 — PDF 문서 열기 (브라우저 기본 뷰어) · 🔄 핵심 완결

> 목표: Unity에서는 문서 소개와 맥락만 제공하고, 실제 PDF 읽기·확대·검색·다운로드는 브라우저 기본 PDF 뷰어에 맡긴다.

| ID | Task | 상태 |
|---|---|---|
| E3-T1 | 초기 포커스-스왑 PDF.js 오버레이 구현 (E3-T7에서 폐기) | ✅ |
| E3-T2 | URL 해시 `#open=클래스명` 딥링크 (`PortfolioBootstrap.cs`) | ✅ |
| E3-T3 | ~~monitor.html 셸 좌표 정합 검증~~ — **폐기**(모니터 셸 미사용, 캔버스가 뷰포트 전체라 스케일/오프셋 없음) | ❌ |
| E3-T4 | ~~복귀 경로/스냅샷 플래시 refinement~~ — 브라우저 기본 뷰어 전환으로 불필요 | ❌ |
| E3-T5 | ~~다페이지 스크롤·Range·텍스처 가상화~~ — 브라우저 기본 뷰어에 위임 | ❌ |
| E3-T6 | 초기 내장 뷰어 release 빌드 눈확인 — E3-T7 방향 변경으로 대체 | ❌ |
| E3-T7 | 내장 pdf.js/iframe/스냅샷 제거 + UGUI 소개/안내 창의 외부 PDF 버튼으로 전환 | ✅ |
| E3-T8 | release WebGL에서 프로젝트 3종 + Resume CTA가 새 탭의 브라우저 PDF 뷰어를 여는지 눈확인 | ⬜ |

**구현 노트 — 외부 열기 구조**
- `ExternalPdfWindow.OpenDocument()`가 `Application.streamingAssetsPath + "/docs/<name>.pdf"`를 `Application.OpenURL`로 연다. 호출은 `DetailsButton`의 직접 클릭 이벤트 안에서 실행한다.
- PDF 실파일은 `Assets/StreamingAssets/docs/`에 유지한다. 내장 뷰어 코드(`PdfOverlay.jslib`, `PdfJsBridge.*`)와 `Assets/StreamingAssets/pdfjs/` 번들은 제거했다.
- Projects 런처(E2-T4)는 문서형 항목을 각 UGUI 소개 창으로 라우팅하고, 사용자가 맥락을 읽은 뒤 CTA로 브라우저 PDF를 연다.
- 딥링크(E3-T2)는 기존 타입명을 유지하므로 계속 동작하며, 이제 내장 PDF 대신 소개/안내 창을 연다.

---

## E4 — WebGL 빌드 & 배포 · ✅ 배포 완료

> 목표: GitHub Pages 제약(싱글스레드·파일당 100MB·Content-Encoding 못 넣음) 안에서 재현 가능한 배포 빌드를 만들어 발행.
> **발행됨 → https://thundervolt45.github.io/Portfolio-OS/** (2026-07-16)

| ID | Task | 상태 |
|---|---|---|
| E4-T1 | 재현 빌드 스크립트 `PortfolioBuild.cs` (메뉴 + 배치모드) | ✅ |
| E4-T2 | Brotli 압축 + Decompression Fallback 채택 — 공개 URL에서 폴백 로드 확인 완료 | ✅ |
| E4-T3 | 로컬 서버 `Tools/serve.py`(`.br` 서빙 + HTTPS) + preview `launch.json` | ✅ |
| E4-T4 | **GitHub Pages 배포** — 이 저장소 `gh-pages` 루트 + 저장소 public 전환 (최종 목표) | ✅ |
| E4-T5 | 모바일/WebGL 미지원 폴백: 정적 안내+링크 페이지 + capable 모바일 "그래도 입장" 허용 | ⬜ |
| E4-T6 | 배포 스크립트 `Tools/deploy_ghpages.py` (payload 검증 + orphan force-push + URL 확인) | ✅ |
| E4-T7 | 전체화면 템플릿 `Assets/WebGLTemplates/PortfolioFull` + 기본 UI 배율(`PortfolioUIScale`) | ✅ |

**구현 노트 — 빌드 설정**
- `Assets/Editor/PortfolioBuild.cs`(`PortfolioOS.EditorTools.PortfolioBuild`) — 배포 설정을 코드로 고정(에디터 UI 상태에 의존 X). 메뉴 **Portfolio → Build WebGL (Release/Development)** + 배치모드(`-executeMethod …BuildWebGLRelease`).
- release = **Brotli + Decompression Fallback=Enabled** + High 매니지드 스트리핑 + IL2CPP `Release` + 싱글스레드 + linker Wasm. dev = Minimal/Debug.
- 배포 크기: 압축 Disabled 52MB → **Brotli ~30MB**. GH Pages가 Content-Encoding을 못 넣지만 Unity 내장 JS 디컴프레서(fallback)가 클라이언트에서 `.br`을 풀어 배포 가능.
- 로컬 `serve.py`: wasm MIME `application/wasm`, `.br`/`.gz` 사전압축 서빙 + `Content-Encoding` 헤더, HTTPS(self-signed; Brotli는 보안 컨텍스트 필요), `Cache-Control: no-store`. 미리보기는 `python Tools/serve.py 8000 Build/WebGL`로 실행한다.

**구현 노트 — 배포 (E4-T4/T6/T7)**
- **배포 경로**: 별도 `ThunderVolt45.github.io` repo를 만들지 않고 **이 저장소의 `gh-pages` 브랜치 루트**로 발행한다.
  저장소는 **public**. Pages 설정 = `gh-pages` / `/`.
- **왜 로컬 빌드를 푸시하나**: MPUIKit(유료 에셋)이 gitignore라 **CI 빌드가 불가** → 로컬 `Build/WebGL`을 그대로 배포.
- **재배포**: `python Tools/deploy_ghpages.py [--yes] [--verify]`. payload 검증(필수 파일·100MB 하드리밋·빌드 시각) →
  `.nojekyll` 추가 → **매번 커밋 1개짜리 orphan 히스토리로 force-push**(빌드 산출물이 히스토리에 누적돼 저장소가
  비대해지는 것을 방지) → `--verify`면 공개 URL 200 확인. 작업 저장소는 건드리지 않고 임시 디렉터리에서 수행.
- **공개 URL에서 검증됨**: `.unityweb` 응답에 `Content-Encoding` 없음 + `Content-Type: application/vnd.unity` →
  Unity 내장 JS 디컴프레서가 클라이언트에서 `.br`을 풀어 정상 로드(= §4-C 설계대로 동작).
- **전체화면 템플릿**: 기본 Unity 플레이어(960x600 고정 캔버스)가 작아 `Assets/WebGLTemplates/PortfolioFull`로 교체.
  캔버스에 고정 크기를 주지 않고 CSS로 뷰포트를 채우며, `matchWebGLToCanvasSize`(기본 true)가 렌더 타깃을 맞춘다.
  `PlayerSettings.WebGL.template = "PROJECT:PortfolioFull"`을 `PortfolioBuild.cs`에 고정.
- **기본 UI 배율**: 프레임워크는 시작 시 `PlayerPrefs.GetFloat("DPI Settings", 1f)`를 읽어
  `referenceResolution = screen / dpi`로 세팅한다(= dpi가 곧 UI 배율). 이 키를 **쓰는 곳이 없어 항상 100%**였으므로,
  `Assets/Scripts/PortfolioUIScale.cs`가 `BeforeSceneLoad`에 기본 배율을 주입한다. 배율 조정은 `DefaultScale` 한 곳.

> **E4-T5 참고**: 모니터 셸 폐기로 **셸 기반 디바이스 판정 경로가 사라졌다**. 모바일/미지원 폴백은
> 템플릿(`PortfolioFull/index.html`)에서 WebGL 지원 여부를 판정해 정적 안내로 분기하는 방식으로 **재설계 필요**.

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
| E5-T7 | WantedSans SDF **Font Weights 테이블** 설정 (faux bold 제거, 진짜 굵기 폰트 사용) | ✅ |
| E5-T8 | 공식 Unity CLI + `com.unity.pipeline` 로컬 Editor 연동 | ✅ |

**구현 노트**
- E5-T1: 기본 TMP(LiberationSans)에 한글 없어 □로 깨짐 → WantedSans 동적 SDF를 TMP Settings `m_fallbackFontAssets`(전역 폴백)에 추가. 창 텍스트 한글 정상 렌더 확인.
- E5-T3: `UGUIIcon.ApplyTargetWindowIcon`은 `windowIcon`이 있으면 데스크톱+작업표시줄에 **자동 적용**, null이면 스킵(현재 빈 아이콘 = 시각적 일관). 각 프리팹 `windowIcon`에 스프라이트를 넣으면 자동 반영. **완료: `UGUISampleWindow`**(스프라이트 `Icons/UGUISample.png` 생성 + `windowIcon` 배선, E2-T7 노트). 남은 것: About/Resume는 스프라이트 존재하나 배선 확인 필요, PDF 3종·Contact·Projects는 스프라이트/배선 미결.
- E5-T4: `Assets/UGUIWindowSample/Fonts/WantedSans-*.asset` 7개가 동적 아틀라스에 한글 글리프가 구워지며 매번 "수정됨"으로 뜸(`_typelessdata`, ~1392줄). 지금은 작업/병합 때마다 `git checkout --`로 되돌리는 중 → 근본 처리(정적 pre-bake 또는 커밋/ignore 정책) 필요.
- **E5-T7 (Font Weights, 완료)**: WantedSans SDF는 굵기별 애셋(Regular/Medium/SemiBold/Bold/ExtraBold)이 준비돼 있어, TMP 기본 `<b>`의 **faux(합성) bold 대신 진짜 굵기 폰트**를 쓰도록 각 베이스 애셋의 `m_FontWeightTable`을 설정. 인덱스=가중치/100 → **[6]=SemiBold, [7]=Bold, [8]=ExtraBold** 매핑(5개 베이스 애셋 전부). [4]/[5](400/500)은 **비워 둠** — 빈 칸은 베이스 폰트로 폴백하므로 일반 텍스트가 베이스 굵기를 유지(안 그러면 `</b>` 뒤 400 조회로 베이스가 Regular로 바뀌는 버그). 효과: `<b>`→진짜 Bold, `<font-weight=600/800>`→SemiBold/ExtraBold. **렌더타임 적용이라 콘텐츠 재빌드 불필요**, About·UGUISample 등 모든 창의 기존 `<b>`가 자동으로 진짜 굵기로 바뀜. 설정은 `execute_code`+`SerializedObject`로 기록(play mode 대조 테스트로 4단계 굵기 구분 확인).
  - ⚠️ **E5-T4 노이즈 정책과 충돌 주의**: 이 5개 애셋에 이제 **의미 있는 변경(weight table)** 이 들어 있음 → SDF 글리프 노이즈라고 `git checkout --`로 통째 되돌리면 **weight table 설정도 날아감**. 반드시 **weight table 변경을 먼저 커밋**해 새 baseline으로 만든 뒤, 이후엔 그 baseline 대비 노이즈만 되돌릴 것.
  - **Header 프리팹 적용(font-weight 방식 + macOS 타이틀 스타일)**: `Assets/UGUIWindowSample/Resources/BaseComponents/Header.prefab`의 제목 TMP(`TitleText`, 헤더 높이 40)를 **Bold 애셋 직접 지정 → 베이스 `WantedSans-Regular SDF` + `m_fontWeight`(weight table 경유)** 방식으로 전환. 최종 스타일은 **macOS 타이틀바**: `m_fontWeight=600`(SemiBold) · `fontSize=15` · 색 `#6E6E73`(ink-2 회색) · 중앙정렬(기존 유지). 시안(`*-mockup.html`)의 `.titlebar b{font-weight:600;color:ink-2}`와 정합. 한글+라틴 진짜 SemiBold 렌더 확인(□ 없음). 모든 창이 이 프리팹을 공유하므로 전역 반영. ⚠️ Header.prefab은 upstream 프레임워크 파일 → 이미 로컬 커스터마이즈 상태이며, upstream 병합 시 충돌 가능(병합 후 확인).
- **E5-T4 잠정 방침(2026-07-13)**: 콘텐츠(등장 문자)가 아직 미확정이라 static pre-bake는 시기상조 → **일단 dynamic 유지 + git 노이즈 감수**, 최종 단계(문자셋 확정/E4 배포 직전)에서 static 전환 재결정.
  - 설정 목표: `WantedSans-Regular SDF`(전역 폴백, E5-T1)의 **Multi Atlas Textures = ON**(현재 `m_IsMultiAtlasTexturesEnabled: 0`). Population=Dynamic·1024²는 이미 맞음. 1024² 멀티는 총 바이트 동일(면적 불변)이라 100MB 우려와 무관, 오버플로 시 □ 방지용.
  - ⚠️ **다음 세션 처리**: Unity 에디터/MCP 도구가 제공되는 Codex 세션에서 브리지와 활성 인스턴스를 확인한 뒤 `manage_asset` 등 에디터 API로 플래그를 변경한다. 도구가 없으면 Unity Inspector에서 변경하고 serialized diff를 검증한다. 설정 변경은 동적 SDF 노이즈와 분리해 적용하거나 별도 커밋으로 관리한다.
- E5-T6: **MPUIKit** = 에셋스토어 **유료** 에셋(재배포 불가) → 저장소에서 **제외**. `.gitignore`에 `/[Aa]ssets/MPUIKit/`·`/[Aa]ssets/MPUIKit.meta` 추가. 빌드는 이 에셋이 설치된 로컬에서 수행(타 환경/CI에는 미설치 → UI 깨질 수 있음, 필요 시 설치 안내 메모로 대체).
  - ⚠️ WantedSans 폰트를 `Assets/Fonts/`에 별도 반입하려다 철회 — **이미 `Assets/UGUIWindowSample/Fonts/`에 동일 7종이 존재**(중복). 폰트는 그쪽을 SSOT로 사용.
- **E5-T8 (공식 Unity CLI, 완료)**: Unity Hub가 설치한 `unity` 1.0.0-beta.8 확인(`%LOCALAPPDATA%/Unity/bin/unity.exe`). `unity pipeline install --project-path <repo>`로 실험적 패키지 `com.unity.pipeline` 0.6.0-exp.1을 설치했다. 실행 중인 Editor의 로컬 Pipeline 서버 `127.0.0.1:7800`이 reachable, 상태 `ready`임을 확인했고, `unity command eval`로 Unity 6000.6.0f1과 활성 씬 `Assets/Scenes/PortfolioOS.unity`를 live 조회했다. 기존 CoplayDev `com.coplaydev.unity-mcp`는 별개 도구로 유지한다.

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
- **셸 없음(2026-07-16 확정)**: monitor.html 셸은 폐기됐고 부팅 연출은 **전적으로 WebGL 템플릿**이 담당한다(진짜 progress 접근이 여기 있음). 조율할 외부 셸이 없으므로 docs 세션 의존도 사라짐.
- **부팅 연출은 `PortfolioFull` 템플릿 위에 얹는다** — 이미 배포에 쓰이는 템플릿이므로 새로 만들지 말 것. 현재는 중앙 로딩 오버레이(`#unity-loading-bar` + `createUnityInstance(..., onProgress)`)만 있고, 이 자리를 부팅 연출로 대체하면 된다(E6-T1).
- ⚠️ 커스텀 템플릿은 Unity 기본 로더 스크립트(`{{{ LOADER_FILENAME }}}` 등 플레이스홀더) 구조를 유지해야 빌드가 깨지지 않음.

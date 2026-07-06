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
- [ ] 창 내부 콘텐츠 스크롤(ScrollRect) — §4-A
- [x] **release WebGL 빌드**로 배포 크기 최적화 (52MB, 위 참조) — 남은 건 `ThunderVolt45.github.io` 배포(모니터 셸에 임베드)
- [ ] **PDF 하이퍼링크 대응**(설계 확정): 포커스 시 실제 PDF.js viewer DOM을 창 위에 좌표동기 오버레이, 백그라운드 시 텍스처 스냅샷으로 스왑 → 선택·검색·폼 native 지원. 단계 1 = 라이브 viewer + 좌표동기 jslib.

### 알려진 함정
- Unity MCP 브리지가 도메인 리로드/플랫폼 전환/서버 재시작 때 자주 끊김 → `manage_editor(telemetry_status)`로 재확인 후 **`set_active_instance`로 인스턴스 재고정**(외부 git 변경 후 AssetDatabase가 stale하면 프리팹이 "없다"고 나올 수 있음 → 강제 refresh).
- `execute_code`는 **C# 6(codedom)** — 튜플/문자열 보간/`using` 지시문 불가. 완전한 네임스페이스로 작성.
- upstream 병합 시 `ProjectSettings.asset`이 productName 등을 덮어쓸 수 있음 → 병합 후 확인·복구.

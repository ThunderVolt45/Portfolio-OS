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
- **데스크톱/아이콘**: `Assets/UGUIWindowSample/Scripts/Sample/UGUIDesktop.cs`(아이콘 재귀 수집 + `OnIconClicked`),
  `UGUIIcon`, `UGUIMenu`. 샘플에서 `UGUIDesktop.Start()`가 초기 창들을 생성.
- 주요 스크립트: `Scripts/Base/`(UGUIWindow, UGUIWindowManager, Header/Border/Edge/Content/State/View, Cursor/Log),
  `Scripts/Sample/`, `Scripts/Utilities/DoublyLinkedList.cs`.
- **Unity 버전**: `main` 기준 `6000.3.19f1`(Unity 6.3)로 열 것 (README 뱃지는 stale).

---

## 4. 작업 계획 (Track B)

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

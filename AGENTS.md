# AGENTS.md — Portfolio-OS

이 저장소는 김민영의 취업 포트폴리오를 데스크톱 OS 형태로 구현하는 Unity 프로젝트다.
직접 만든 `UGUI-Window-Sample` 창 프레임워크 위에 About, Projects, Resume, Contact 등을 앱 창으로 구성하고 Unity WebGL로 배포한다. 프레임워크를 포트폴리오 UI에 직접 사용하는 dogfooding이 핵심이다.

- 공개 사이트: https://thundervolt45.github.io/Portfolio-OS/
- Unity 캔버스가 브라우저 뷰포트 전체를 사용한다.
- 과거의 HTML 모니터 셸 구상은 폐기되었다. 현재 랜딩과 부팅 UI는 `Assets/WebGLTemplates/PortfolioFull`이 담당한다.

## 0. 작업 시작 절차

1. 이 문서와 `TASKS.md`를 읽는다. 이 문서는 지속적인 작업 규칙, `TASKS.md`는 최신 진행 상태와 구현 이력의 원천이다.
2. 변경 전 `git status --short`로 사용자 변경을 확인하고 보존한다.
3. Unity 버전은 `ProjectSettings/ProjectVersion.txt`에서 확인한다. 현재 기준 버전은 `6000.6.0f1`이다.
4. 실제 포트폴리오 작업은 `Assets/Scenes/PortfolioOS.unity`에서 한다. `Assets/UGUIWindowSample/Scenes/UGUIWindowSampleScene.unity`는 프레임워크 원본 샘플 씬이다.
5. Unity MCP 또는 에디터 도구가 현재 세션에 제공되면 연결 상태와 활성 Unity 인스턴스를 확인한 뒤 사용한다. 도구가 보이지 않는다는 이유만으로 설치나 연결 상태를 추측하지 않는다.

## 1. 콘텐츠 원본(외부 SSOT)

포트폴리오 문구와 사실관계의 원본은 이 저장소 밖의 docs 프로젝트에 있다. 내용을 지어내지 말고 다음 파일을 확인한다.

- 인적사항·경력: `C:\Users\zxc98\Documents\GitHub\-\김민영_이력서_2026.md`
- 랜딩·About·프로젝트 카드 확정 문구: `C:\Users\zxc98\Documents\GitHub\-\portfolio\index.md`
- 프로젝트별 기술문서·케이스스터디: `C:\Users\zxc98\Documents\GitHub\-\portfolio\projects\*.md`

콘텐츠 작성 원칙:

- 한국어로 작성하고 기술 용어는 필요한 경우 영문 표기를 유지한다.
- 과장하거나 사실을 만들지 않는다. 원본에 없는 정량 수치를 추정해서 추가하지 않는다.
- 공개 저장소, Unity 에셋, PDF, WebGL 빌드에 전화번호나 주소를 넣지 않는다. 공개 연락 수단은 이메일 `zxc9876zxc@gmail.com`과 GitHub `https://github.com/ThunderVolt45`만 사용한다.
- 외부 원본에 민감한 개인정보가 있더라도 공개 프로젝트로 복사하지 않는다.

## 2. 저장소와 주요 경로

- `origin`: `ThunderVolt45/Portfolio-OS` — 공개 포트폴리오 저장소
- `upstream`: `ThunderVolt45/UGUI-Window-Sample` — 창 프레임워크 원본
- 프레임워크 코드: `Assets/UGUIWindowSample/Scripts/`
- 포트폴리오 앱 코드: `Assets/Scripts/Portfolio/`
- 창 프리팹: `Assets/Resources/Windows/`
- WebGL 템플릿: `Assets/WebGLTemplates/PortfolioFull/`
- WebGL 빌드 스크립트: `Assets/Editor/PortfolioBuild.cs`
- 배포 스크립트: `Tools/deploy_ghpages.py`
- 로컬 WebGL 서버: `Tools/serve.py`

프레임워크 동기화가 요청된 경우 CLI에서 다음 순서로 가져온다.

```bash
git fetch upstream
git merge upstream/main
```

병합 전에는 워킹 트리를 검사한다. 줄바꿈이나 동적 폰트 아틀라스 노이즈처럼 보여도 사용자 변경을 임의로 되돌리지 않는다. 씬 충돌은 손으로 YAML을 합치기보다 Unity Smart Merge를 우선 사용한다.

Package Manager로 관리할 수 있는 플러그인은 저장소에 vendoring하지 않는다. Unity MCP 패키지 `com.coplaydev.unity-mcp`는 `Packages/manifest.json`의 Git 의존성으로 관리한다. MPUIKit은 유료 에셋이므로 gitignore 상태를 유지하며 CI 환경에 존재한다고 가정하지 않는다.

## 3. 먼저 읽을 문서

창 시스템 동작을 바꾸기 전에 관련 문서를 읽는다.

- `docs/manual/02-concepts.md`: 아키텍처, 풀링, z-order, DPI
- `docs/manual/03-creating-windows.md`: 커스텀 창과 프리팹 명명 규칙
- `docs/manual/06-events-lifecycle.md`: open, close, focus, minimize 이벤트
- `docs/manual/07-samples.md`: 데스크톱 샘플
- `docs/manual/08-api-reference.md`: 공개 API
- `docs/ClassDiagram.md`, `docs/class-diagram/*.md`: 클래스 관계
- `README.md`, `docs/Manual.md`: 공개 문서 진입점

## 4. 창 프레임워크 구조

창 생성 API는 `UGUIWindow` 네임스페이스의 `UGUIWindowManager`에 있다.

```csharp
UGUIWindowManager.CreateWindow<T>(string name = null);
UGUIWindowManager.CreateWindowEx<T>(string name, int x, int y, int w, int h);
```

- 새 앱 창은 `UGUIWindow`를 상속한 C# 클래스와 동명 프리팹 한 쌍이다.
- 매니저는 타입명으로 `Resources.Load("Windows/" + typeName)`을 호출한다. 클래스와 프리팹 이름을 일치시킨다.
- `UGUIWindowManager`는 창 생성, 오브젝트 풀링, z-order, DPI 스케일, ESC 처리를 담당하는 싱글턴 진입점이다.
- `UGUIWindow`는 창 모드와 `OnOpenWindow`, `OnCloseWindow`, `OnFocusWindow`, `OnMinimizeWindow` 이벤트를 소유한다.
- `UGUIWindowView`는 헤더, 테두리, 버튼, 페이드, 최대화·복원 레이아웃 등 시각 상태를 관리한다.
- `UGUIWindowState`는 위치, 크기, 앵커, 복원 플래그를 저장한다.
- 오브젝트 풀링은 `useObjectPooling`, 다중 인스턴스는 `allowMultipleInstance`로 제어한다. z-order는 `DoublyLinkedList`로 관리한다.
- 포트폴리오 앱 클래스는 `Assets/Scripts/Portfolio/`에 두고 `UGUIWindow` 네임스페이스를 따른다.

데스크톱 계층:

- `UGUIDesktop`은 자식 `UGUIIcon`을 재귀 수집하고 `createDemoWindowsOnStart`로 데모 창 자동 생성을 제어한다. 포트폴리오에서는 기본값 `false`로 빈 데스크톱에서 시작한다.
- `UGUIIcon`은 더블 클릭 시 `targetClassName`으로 `UGUIWindow.{targetClassName}` 타입을 찾아 창을 연다.
- `UGUIMenu`는 설정 창과 종료 동작을 제공한다.
- `UGUITaskBar`와 `UGUITaskIcon`은 열린 창과 최소화된 창을 관리한다.
- `UGUIApplicationSetting`은 해상도, 전체화면 모드, 프레임레이트, DPI 변경을 적용한다.
- 창 프리팹의 `UGUIWindow.windowIcon`을 지정하면 `UGUIIcon.ApplyTargetWindowIcon()`이 데스크톱과 작업표시줄 아이콘에 자동 반영한다. 씬 아이콘에는 `targetClassName`과 라벨만 지정한다.

## 5. 구현 규칙

- 기존 패턴을 우선하고 새 추상화는 필요할 때만 추가한다.
- 공용 창 시스템 변경은 작고 재사용 가능하게 유지한다. 포트폴리오 전용 동작은 `Assets/Scripts/Portfolio/`에 둔다.
- `UGUIWindow.Awake` 또는 `UGUIWindow.OnEnable`을 오버라이드하면 `base`를 먼저 호출한다.
- 풀링으로 다시 활성화되는 창의 콘텐츠와 크기 초기화는 보통 `Start()`가 아니라 `OnEnable()`에서 수행한다.
- 프레임워크 로그에는 `Debug.Log` 대신 `UGUIWindowLog`를 사용한다.
- 창 상태 관찰은 폴링보다 기존 이벤트 구독을 우선한다.
- 명시적인 요청 없이 오브젝트 풀링 동작을 바꾸지 않는다.
- serialized field를 변경하면 기존 프리팹에 참조 할당이 필요한지 확인한다.
- 픽셀 단위 UGUI 배치는 Unity 에디터에서 시각적으로 검증한다.

Unity 에셋 규칙:

- 에셋과 `.meta` 파일을 함께 유지한다.
- Unity 에셋 경로에는 `/`를 사용한다.
- 클래스명 기반 프리팹 로딩 때문에 창 클래스·프리팹 쌍의 이름을 가볍게 변경하지 않는다.
- 프리팹이나 씬 YAML 직접 편집은 작고 의도적이며 diff를 확실히 검증할 수 있을 때만 한다.
- 동적 WantedSans SDF 에셋은 실행 중 글리프 아틀라스 때문에 변경 노이즈가 생긴다. Font Weight Table에는 의미 있는 설정도 있으므로 폰트 에셋 전체를 무심코 되돌리지 않는다.

## 6. WebGL 빌드와 배포

배포 설정:

- Brotli + Decompression Fallback 활성화
- 싱글스레드 빌드
- 파일 하나당 GitHub 100MB 제한 준수
- `Assets/WebGLTemplates/PortfolioFull`을 사용해 캔버스가 뷰포트를 채우도록 구성
- MPUIKit이 저장소에 없으므로 CI 빌드 대신 필요한 에셋이 설치된 로컬 Unity에서 빌드

로컬 빌드는 `PortfolioBuild.cs`의 메뉴 또는 배치 메서드를 사용한다. 빌드 미리보기는 저장소 루트에서 다음 명령으로 실행한다.

```bash
python Tools/serve.py 8000 Build/WebGL
```

재배포 명령은 다음과 같다.

```bash
python Tools/deploy_ghpages.py [--yes] [--verify]
```

배포 스크립트는 `gh-pages`를 orphan 커밋 하나로 force-push한다. 사용자가 배포를 명시적으로 요청한 경우에만 실행하고, 먼저 payload와 대상 저장소를 확인한다.

## 7. Unity 에디터와 MCP

- Unity 에디터 작업이 필요한 경우 가능하면 에디터/MCP 도구로 GameObject, 컴포넌트, 프리팹, 씬, 콘솔, 컴파일 상태를 확인한다.
- 브리지는 도메인 리로드, 플랫폼 전환, 서버 재시작 뒤 연결이 끊길 수 있다. 도구가 제공하는 상태 확인과 활성 인스턴스 선택 기능으로 다시 고정한다.
- 외부 파일 변경 뒤 AssetDatabase가 오래된 상태면 먼저 refresh한 후 누락 에셋을 판단한다.
- Unity MCP의 동적 C# 실행기가 CodeDOM/C# 6 제약을 보이면 튜플, 문자열 보간, 파일 수준 `using` 지시문을 피하고 완전한 네임스페이스를 사용한다.
- 에디터 도구가 없는 세션에서는 C#과 문서의 정적 검증을 진행할 수 있다. 에디터 전용 결과를 검증했다고 표현하지 않는다.

## 8. 검증

C# 또는 Unity 에셋 변경 시:

- Unity 에디터 도구가 있으면 컴파일 오류와 콘솔을 확인한다.
- 최소한 변경한 C# 파일을 다시 읽고 `rg`로 깨진 타입명, 프리팹명, 이벤트 참조를 찾는다.
- UI 변경은 포트폴리오 씬에서 create/open, focus/z-order, minimize/restore, close/pooling, DPI 변화와 관련된 경로를 영향 범위에 맞게 확인한다.
- PDF 외부 열기 변경은 실제 WebGL 빌드에서 버튼 클릭 → 새 탭 → 브라우저 기본 PDF 뷰어까지 검증한다.
- 문서만 수정한 경우 Unity 컴파일은 필요하지 않다. 링크, 경로, 상호 참조와 남은 레거시 에이전트 전용 표현을 검색한다.

## 9. 진행 상태 관리

- 현재 Epic, Task 상태, 다음 액션, 상세 구현 이력은 `TASKS.md`에서 관리한다.
- 작업을 마치면 관련 Task의 상태와 필요한 구현 노트를 갱신한다.
- 완료된 Task도 이력 참조를 위해 삭제하지 않는다.
- `AGENTS.md`에는 오래 유지될 규칙과 구조를 두고, 날짜별 상태나 세부 구현 로그를 중복 기록하지 않는다.

추가로 주의할 점:

- upstream 병합 후 `ProjectSettings.asset`의 productName과 포트폴리오 전용 설정이 덮어쓰이지 않았는지 확인한다.
- PDF는 Unity 안에 임베드하지 않는다. UGUI 소개/안내 창의 `DetailsButton`이 `StreamingAssets/docs/*.pdf`를 브라우저 새 탭으로 열며, 구조와 미완료 WebGL 검증은 `TASKS.md` E3를 따른다.
- 공개 배포물에 전화번호나 주소가 다시 들어가지 않았는지 확인한다.

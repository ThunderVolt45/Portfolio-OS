using System.Collections.Generic;
using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PortfolioOS.EditorTools
{
    /// <summary>
    /// UGUISampleWindow.prefab의 Content/Viewport/ScrollContent 하위에
    /// UGUI-Window-Sample(창 프레임워크 자체) 소개 콘텐츠를
    /// 구조화된 UGUI 레이아웃으로 구축한다.
    ///
    /// 디자인 기반: scratchpad/ugui-sample-mockup.html (HTML/CSS 시안, About과 동일 톤).
    /// 폰트: WantedSans SDF(가중치별). 카드 라운드/원형: MPUIKit MPImage.
    /// 콘텐츠 SSOT: portfolio/projects/ugui-window-sample.md.
    ///
    /// 메뉴: Portfolio → Build UGUISample Content
    /// (스켈레톤 프리팹이 없으면 먼저 Portfolio → Build UGUISample Window 실행)
    /// </summary>
    public static class PortfolioUGUISampleContent
    {
        const string Prefab = "Assets/Resources/Windows/UGUISampleWindow.prefab";
        const string FontDir = "Assets/UGUIWindowSample/Fonts/";
        const string HandCursorPath = "Assets/UGUIWindowSample/Posys-Cursors-Improved-by-wrinkdater/BandiView_Posy hand.png";

        // 링크 hover 커서(손가락). 없어도 링크 클릭은 동작.
        static Texture2D FHandCursor;
        static readonly Vector2 HandHotspot = new Vector2(8, 4);

        // ---- 팔레트 (ugui-sample-mockup.html 라이트 테마) ----
        static readonly Color Ink        = Hex("1D1D1F");
        static readonly Color Ink2       = Hex("6E6E73");
        static readonly Color Ink3       = Hex("8E8E93");
        static readonly Color Panel      = Hex("F5F5F7");
        static readonly Color Divider    = Hex("E5E5EA");
        static readonly Color Accent      = Hex("0A84FF");
        static readonly Color AccentSoft  = new Color(10f/255f, 132f/255f, 1f, 0.10f);
        static readonly Color CodeBg      = Hex("1E2130");
        static readonly Color White       = Color.white;

        // ---- 폰트 (Build()에서 로드) ----
        static TMP_FontAsset FRegular, FMedium, FSemiBold, FBold, FExtraBold;

        [MenuItem("Portfolio/Build UGUISample Content")]
        public static void Build()
        {
            FRegular   = LoadFont("WantedSans-Regular SDF");
            FMedium    = LoadFont("WantedSans-Medium SDF");
            FSemiBold  = LoadFont("WantedSans-SemiBold SDF");
            FBold      = LoadFont("WantedSans-Bold SDF");
            FExtraBold = LoadFont("WantedSans-ExtraBold SDF");
            if (FRegular == null || FMedium == null || FSemiBold == null || FBold == null || FExtraBold == null)
            {
                Debug.LogError("[UGUISample] WantedSans SDF 폰트 로드 실패. " + FontDir + " 확인.");
                return;
            }

            FHandCursor = AssetDatabase.LoadAssetAtPath<Texture2D>(HandCursorPath);
            if (FHandCursor == null)
            {
                Debug.LogWarning("[UGUISample] 손가락 커서 텍스처 없음(" + HandCursorPath + "). 링크 hover 커서 없이 진행.");
            }

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var scroll = root.transform.Find("Content/Viewport/ScrollContent") as RectTransform;
                if (scroll == null)
                {
                    Debug.LogError("[UGUISample] Content/Viewport/ScrollContent 없음. 먼저 Build UGUISample Window 실행.");
                    return;
                }

                // ScrollRect: 세로 스크롤만.
                var content = root.transform.Find("Content");
                var scrollRect = content != null ? content.GetComponent<ScrollRect>() : null;
                if (scrollRect != null) { scrollRect.horizontal = false; scrollRect.vertical = true; }

                // 기존 자식 정리.
                var kill = new List<GameObject>();
                foreach (Transform c in scroll) kill.Add(c.gameObject);
                foreach (var g in kill) Object.DestroyImmediate(g);

                // ScrollContent: 상단-가로stretch, 세로는 ContentSizeFitter로 성장.
                scroll.anchorMin = new Vector2(0, 1);
                scroll.anchorMax = new Vector2(1, 1);
                scroll.pivot = new Vector2(0.5f, 1);
                scroll.anchoredPosition = Vector2.zero;
                scroll.sizeDelta = new Vector2(0, scroll.sizeDelta.y);
                var rootVlg = Vlg(scroll.gameObject, 22, 22, 24, 26, 24, true);
                rootVlg.childAlignment = TextAnchor.UpperCenter;
                Fitter(scroll.gameObject);

                BuildHero(scroll);
                BuildCallout(scroll);
                BuildIntro(scroll);
                BuildBasicInfo(scroll);
                BuildFeatures(scroll);
                BuildArchitecture(scroll);
                BuildHighlights(scroll);
                BuildEngDetail(scroll);
                BuildLinks(scroll);
                BuildSlogan(scroll);

                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll);

                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                Debug.Log("[UGUISample] Content built. height=" + scroll.rect.height.ToString("0"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
        }

        // ============================ Sections ============================

        static void BuildHero(Transform parent)
        {
            var hero = Row("Hero", parent, 16, TextAnchor.MiddleLeft, false);

            // 앱 아이콘 = 데스크톱/작업표시줄과 '동일한' 스프라이트(UGUISample.png)를 그대로 사용.
            // (예전엔 UGUI 도형으로 별도로 그렸으나 데스크톱 아이콘과 달라 보여 스프라이트로 통일.)
            var tileGo = NewChild("AppIcon", hero.transform);
            var tileImg = tileGo.AddComponent<Image>();
            tileImg.sprite = LoadSprite("Assets/Portfolio/Icons/UGUISample.png");
            tileImg.preserveAspect = true;
            tileImg.raycastTarget = false;
            var tle = tileGo.AddComponent<LayoutElement>();
            tle.minWidth = tle.preferredWidth = 66; tle.minHeight = tle.preferredHeight = 66; tle.flexibleWidth = 0;

            // Who (이름 + 역할)
            var who = new GameObject("Who", typeof(RectTransform));
            who.transform.SetParent(hero.transform, false);
            var whoV = Vlg(who, 0, 0, 0, 0, 5, true);
            whoV.childAlignment = TextAnchor.MiddleLeft;
            who.AddComponent<LayoutElement>().flexibleWidth = 1;
            AddText(who.transform, "Name", "UGUI-Window-Sample", FExtraBold, 21, Ink, TextAlignmentOptions.Left, false);
            AddText(who.transform, "Role",
                "Unity UGUI만으로 만든\n데스크톱 OS 스타일 창(Window) UI 프레임워크",
                FMedium, 12.5f, Ink2, TextAlignmentOptions.Left, true, 3f);
        }

        static void BuildCallout(Transform parent)
        {
            var card = Card("Callout", parent, AccentSoft, 14);
            var h = Hlg(card, 16, 16, 14, 14, 11, false);
            // childControlHeight는 Hlg에서 true → LayoutElement 고정 높이(22)가 존중되어
            // 아이콘 타일이 행 높이만큼 늘어나지 않는다. childForceExpandHeight만 끄고 상단 정렬.
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.UpperLeft;

            var ic = Card("Ic", card.transform, Accent, 7);
            var ile = ic.AddComponent<LayoutElement>();
            ile.minWidth = ile.preferredWidth = 22; ile.minHeight = ile.preferredHeight = 22; ile.flexibleWidth = 0;
            var ict = AddText(ic.transform, "T", "↔", FBold, 13, White, TextAlignmentOptions.Center, false);
            Stretch((RectTransform)ict.transform);

            var t = AddText(card.transform, "T",
                "<b><color=#0A84FF>지금 이 창이 바로 그 프레임워크입니다.</color></b> 이 포트폴리오의 모든 창은 " +
                "UGUI-Window-Sample 위에서 동작합니다. 헤더를 끌어 옮기고, 모서리를 잡아 크기를 바꾸고, " +
                "최대화·최소화해 보세요.",
                FMedium, 12.5f, Ink, TextAlignmentOptions.TopLeft, true, 5f);
            t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        static void BuildIntro(Transform parent)
        {
            var s = Section("Intro", parent, "소개");
            AddText(s, "Lead",
                "별도 UI 에셋 없이 <b>Unity 빌트인 UGUI만으로</b> 데스크톱 OS와 같은 창 기반 UI를 구현한 " +
                "프레임워크 샘플입니다. 창의 생성·이동·리사이즈·최대화/최소화/복원은 물론, 오브젝트 풀링·" +
                "z-순서 관리·DPI 스케일링·에디터 도구·CI/CD까지 갖춰 <b>재사용 가능한 라이브러리</b> 수준을 " +
                "목표로 했습니다. 코드 한 줄로 창을 띄웁니다.",
                FRegular, 14, Ink, TextAlignmentOptions.TopLeft, true, 8f);

            // 코드 블록 (다크 카드)
            var code = Card("Code", s, CodeBg, 10);
            Vlg(code, 14, 14, 12, 12, 0, true);
            AddText(code.transform, "Src",
                "<color=#79B8FF>using</color> UGUIWindow;\n" +
                "UGUIWindowManager.CreateWindow<<color=#B392F0>UGUIWindow</color>>();\n" +
                "UGUIWindowManager.CreateWindowEx<<color=#B392F0>UGUIMenu</color>>(" +
                "<color=#9ECBFF>\"메뉴\"</color>, x:0, y:0, width:300, height:400);",
                FMedium, 11.5f, Hex("E6E8F0"), TextAlignmentOptions.TopLeft, true, 6f);
        }

        static void BuildBasicInfo(Transform parent)
        {
            var s = Section("BasicInfo", parent, "기본 정보");
            var card = Card("Card", s, Panel, 14);
            Vlg(card, 0, 0, 0, 0, 0, true);

            InfoRow(card.transform, "분류", "데스크톱 OS 스타일 윈도우 UI 프레임워크 (Non-Game)", 96, true);
            InfoRow(card.transform, "플랫폼 · 엔진", "Windows Standalone · Unity 6 (6000.2.6f2)", 96, true);
            InfoRow(card.transform, "언어", "C# (+ ShaderLab)", 96, true);
            InfoRow(card.transform, "기간 · 인원", "2025.08 ~ · 1인 단독 설계·개발", 96, true);
            InfoRow(card.transform, "기여도", "<b><color=#0A84FF>100%</color></b> — 전 코드 단독 작성 (52커밋)", 96, false);
        }

        static void BuildFeatures(Transform parent)
        {
            var s = Section("Features", parent, "핵심 특징");
            var list = Vlg(NewChild("List", s), 0, 0, 0, 0, 9, true).transform;

            // 아이콘 = HTML 시안 글리프(⌘ ◱ ♻ ⿴ ⤢ ⚙)를 구운 스프라이트. WantedSans엔 없는 글리프라
            // 텍스트 대신 스프라이트로 재현하고 Image.color로 액센트 틴트.
            Feature(list, "feat_cmd",     "타입 기반 창 생성", "CreateWindow<T>() 한 줄, 이름 규약으로 프리팹 자동 로드");
            Feature(list, "feat_resize",  "완전한 창 상호작용", "헤더 드래그 이동, 4변·4모서리 리사이즈, 최대화/복원/최소화");
            Feature(list, "feat_recycle", "오브젝트 풀링", "닫은 창을 파괴하지 않고 재사용, 저메모리 시 자동 정리");
            Feature(list, "feat_stack",   "z-순서 관리", "이중 연결 리스트로 포커스 창을 O(1)로 최상단 이동");
            Feature(list, "feat_scale",   "DPI · 해상도 스케일링", "캔버스 일괄 조정 + 설정 영구 저장 + 드래그 속도 보정");
            Feature(list, "feat_gear",    "에디터 도구 · CI/CD", "창 템플릿 생성기 + GitHub Actions 빌드·릴리즈 자동화");
        }

        static void Feature(Transform parent, string iconName, string title, string desc)
        {
            var card = Card("Feat", parent, Panel, 12);
            var h = Hlg(card, 14, 14, 12, 12, 11, false);
            // 아이콘 타일(26)이 늘어나지 않도록 childControlHeight(=true)는 유지, 상단 정렬.
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.UpperLeft;

            // 아이콘 타일: 액센트-소프트 라운드 + 중앙 글리프 스프라이트(액센트 틴트)
            var tile = Card("Ic", card.transform, AccentSoft, 8);
            var tle = tile.AddComponent<LayoutElement>();
            tle.minWidth = tle.preferredWidth = 26; tle.minHeight = tle.preferredHeight = 26; tle.flexibleWidth = 0;
            var glyph = NewChild("Glyph", tile.transform);
            var gi = glyph.AddComponent<Image>();
            gi.sprite = LoadSprite("Assets/Portfolio/Icons/Features/" + iconName + ".png");
            gi.color = Accent;
            gi.preserveAspect = true;
            gi.raycastTarget = false;
            var grt = (RectTransform)glyph.transform;
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f); grt.pivot = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(16, 16); grt.anchoredPosition = Vector2.zero;

            var col = NewChild("Col", card.transform);
            Vlg(col, 0, 0, 0, 0, 2, true);
            col.AddComponent<LayoutElement>().flexibleWidth = 1;
            AddText(col.transform, "T", title, FSemiBold, 12.5f, Ink, TextAlignmentOptions.TopLeft, false);
            AddText(col.transform, "D", desc, FMedium, 12, Ink2, TextAlignmentOptions.TopLeft, true, 4f);
        }

        static void BuildArchitecture(Transform parent)
        {
            var s = Section("Architecture", parent, "아키텍처 — 핵심 구조 3선");
            var list = Vlg(NewChild("List", s), 0, 0, 0, 0, 10, true).transform;

            ArchCard(list, "01", "단일 진입점 매니저",
                "생성·풀링·z-순서·DPI를 총괄하는 <b>스레드 세이프 싱글톤</b>. 개별 창은 자기 동작만 " +
                "책임지고 전역 정책은 매니저가 담당 — 관심사 분리. (UGUIWindowManager)");
            ArchCard(list, "02", "창 1개의 3분할",
                "창 하나를 <b>Controller(동작) · View(시각) · State(스냅샷)</b>로 분리. 외형을 바꿔도 " +
                "컨트롤러 무관, 최대화 직전 상태 저장·복원이 단순해집니다.");
            ArchCard(list, "03", "상호작용 컴포넌트 합성",
                "<b>Header · Border×4 · Edge×4 · Content</b>를 부위별로 합성. 각 부위가 자기 입력만 " +
                "처리하고 창에 위임 — 끄고 켜기·확장이 쉬움.");
        }

        static void ArchCard(Transform parent, string no, string title, string desc)
        {
            var card = Card("Arch", parent, Panel, 12);
            Vlg(card, 15, 15, 14, 14, 7, true);

            var top = Row("Top", card.transform, 8, TextAnchor.MiddleLeft, false);
            var badge = Chip("Badge", top.transform, AccentSoft, no, Accent);
            badge.GetComponent<LayoutElement>();
            AddText(top.transform, "T", title, FBold, 13, Ink, TextAlignmentOptions.Left, false)
                .gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            AddText(card.transform, "D", desc, FMedium, 12, Ink2, TextAlignmentOptions.TopLeft, true, 5f);
        }

        static void BuildHighlights(Transform parent)
        {
            var s = Section("Highlights", parent, "강조 구현");
            var list = Vlg(NewChild("List", s), 0, 0, 0, 0, 16, true).transform;

            Highlight(list, "1", "z-순서용 이중 연결 리스트 직접 구현",
                "z-순서 변경 = \"임의 창을 빼서 맨 앞으로\" → <b>List로는 O(n)</b>. 제네릭 DoublyLinkedList<T>를 " +
                "직접 구현해, 포커스 시 노드 참조로 <b>말단 이동 O(1)</b> + SetAsLastSibling().",
                "문제에 맞는 자료구조 직접 구현");
            Highlight(list, "2", "오브젝트 풀링 + 저메모리 자동 정리",
                "창 생성 비용이 크므로 닫아도 <b>파괴하지 않고 풀에 보관</b>(키 = Type.Name), 재요청 시 재사용. " +
                "Application.lowMemory 시 TrimWindow()로 비활성 창을 실제 파괴해 회수.",
                "예방적 최적화 설계");
            Highlight(list, "3", "DPI 스케일링 + 입력 보정",
                "SetDPI()로 모든 캔버스 referenceResolution 일괄 조정 + PlayerPrefs 저장. 드래그가 DPI와 " +
                "무관하게 일정 속도가 되도록 <b>ScreenMultiplier로 포인터 델타를 보정</b>.",
                "상호작용 체감까지 해상도 독립");
        }

        static void Highlight(Transform parent, string num, string title, string desc, string tag)
        {
            var row = Row("HL", parent, 12, TextAnchor.UpperLeft, false);

            var numGo = Card("Num", row.transform, Accent, 13);
            var nle = numGo.AddComponent<LayoutElement>();
            nle.minWidth = nle.preferredWidth = 26; nle.minHeight = nle.preferredHeight = 26; nle.flexibleWidth = 0;
            var nt = AddText(numGo.transform, "N", num, FExtraBold, 13, White, TextAlignmentOptions.Center, false);
            Stretch((RectTransform)nt.transform);

            var col = NewChild("Col", row.transform);
            Vlg(col, 0, 0, 0, 0, 5, true);
            col.AddComponent<LayoutElement>().flexibleWidth = 1;
            AddText(col.transform, "T", title, FSemiBold, 13, Ink, TextAlignmentOptions.TopLeft, true);
            AddText(col.transform, "D", desc, FMedium, 12, Ink2, TextAlignmentOptions.TopLeft, true, 5f);

            // 태그 칩 (좌측 정렬)
            var tagWrap = Row("TagWrap", col.transform, 0, TextAnchor.MiddleLeft, false);
            Chip("Tag", tagWrap.transform, AccentSoft, tag, Accent);
        }

        static void BuildEngDetail(Transform parent)
        {
            var s = Section("EngDetail", parent, "엔지니어링 디테일");
            var list = Vlg(NewChild("List", s), 0, 0, 0, 0, 0, true).transform;

            EngRow(list, "에디터 도구", "메뉴에서 창 템플릿 생성 + 컴포넌트 자동 부착/할당, 커스텀 인스펙터", true);
            EngRow(list, "CI/CD", "GitHub Actions: Windows 빌드 → 시맨틱 버전 태그 → Release 자동 생성", true);
            EngRow(list, "빌드별 로깅", "에디터 = Info / 개발 = Warning / 릴리즈 = Error", true);
            EngRow(list, "문서화", "README + 매뉴얼 8장 + 클래스 다이어그램 8종", false);
        }

        static void EngRow(Transform parent, string k, string v, bool divider)
        {
            var row = Row("Row", parent, 12, TextAnchor.UpperLeft, false);
            row.padding = new RectOffset(0, 0, 11, 11);
            var kt = AddText(row.transform, "K", k, FSemiBold, 12, Ink, TextAlignmentOptions.TopLeft, true);
            var kle = kt.gameObject.AddComponent<LayoutElement>();
            kle.minWidth = kle.preferredWidth = 104; kle.flexibleWidth = 0;
            AddText(row.transform, "V", v, FMedium, 12, Ink2, TextAlignmentOptions.TopLeft, true, 4f)
                .gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            if (divider)
            {
                var dv = NewChild("Divider", parent);
                var img = dv.AddComponent<Image>(); img.color = Divider; img.raycastTarget = false;
                var le = dv.AddComponent<LayoutElement>(); le.minHeight = le.preferredHeight = 1;
            }
        }

        static void BuildLinks(Transform parent)
        {
            var s = Section("Links", parent, "링크");
            var card = Card("Card", s, Panel, 14);
            Vlg(card, 0, 0, 0, 0, 0, true);

            LinkRow(card.transform, "GitHub", "github.com/ThunderVolt45/UGUI-Window-Sample", true,
                "https://github.com/ThunderVolt45/UGUI-Window-Sample");
            LinkRow(card.transform, "Releases", "github.com/ThunderVolt45/UGUI-Window-Sample/releases", true,
                "https://github.com/ThunderVolt45/UGUI-Window-Sample/releases");
            LinkRow(card.transform, "API 매뉴얼", "docs/Manual.md · 클래스 다이어그램 8종", false);
        }

        // href != null 이면 표시 텍스트를 <link>로 감싸 실제 하이퍼링크로 만든다.
        static void LinkRow(Transform parent, string label, string display, bool divider, string href = null)
        {
            var row = Row("Row", parent, 12, TextAnchor.MiddleLeft, false);
            row.padding = new RectOffset(15, 15, 12, 12);
            var lk = AddText(row.transform, "K", label, FSemiBold, 12, Ink2, TextAlignmentOptions.Left, false);
            var kle = lk.gameObject.AddComponent<LayoutElement>();
            kle.minWidth = kle.preferredWidth = 92; kle.flexibleWidth = 0;
            var v = AddText(row.transform, "V",
                href != null ? "<link=\"" + href + "\">" + display + "</link>" : display,
                FMedium, 11.5f, Accent, TextAlignmentOptions.TopLeft, true);
            v.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            if (href != null)
            {
                MakeLink(v);
            }

            if (divider)
            {
                var dv = NewChild("Divider", parent);
                var img = dv.AddComponent<Image>(); img.color = Divider; img.raycastTarget = false;
                var le = dv.AddComponent<LayoutElement>(); le.minHeight = le.preferredHeight = 1;
            }
        }

        // 링크 TMP 라벨을 실제 하이퍼링크로: raycast 켜고 런타임 클릭/hover 핸들러 부착.
        static void MakeLink(TextMeshProUGUI t)
        {
            t.raycastTarget = true;
            var h = t.gameObject.AddComponent<PortfolioOS.TMPLinkHandler>();
            h.linkCursor = FHandCursor;
            h.linkCursorHotspot = HandHotspot;
        }

        static void BuildSlogan(Transform parent)
        {
            var card = Card("Slogan", parent, AccentSoft, 14);
            Vlg(card, 20, 20, 18, 18, 0, true);
            AddText(card.transform, "Quote",
                "<size=200%><color=#0A84FF><b>“</b></color></size>  UGUI만으로 운영체제 수준의 창 UI를 혼자 " +
                "설계·구현하며 UI 아키텍처 · 자료구조 직접 구현 · 예방적 최적화 · CI/CD를 다뤘습니다. 복잡한 " +
                "클라이언트 UI를 <b>끝까지 책임지고 설계하는 역량</b>의 직접적인 근거 — 그리고 지금 이 " +
                "포트폴리오가 그 위에서 돌아갑니다.",
                FMedium, 12.5f, Ink, TextAlignmentOptions.TopLeft, true, 6f);
        }

        // ============================ Helpers ============================

        static Transform Section(string name, Transform parent, string eyebrow)
        {
            var go = NewChild(name, parent);
            Vlg(go, 0, 0, 0, 0, 11, true);
            var eb = AddText(go.transform, "Eyebrow", eyebrow, FSemiBold, 11, Accent, TextAlignmentOptions.Left, false);
            eb.characterSpacing = 5f;
            return go.transform;
        }

        static void InfoRow(Transform parent, string k, string v, float keyW, bool divider)
        {
            var row = Row("Row", parent, 12, TextAnchor.UpperLeft, false);
            row.padding = new RectOffset(15, 15, 11, 11);
            var kt = AddText(row.transform, "K", k, FSemiBold, 12, Ink2, TextAlignmentOptions.TopLeft, true);
            var kle = kt.gameObject.AddComponent<LayoutElement>();
            kle.minWidth = kle.preferredWidth = keyW; kle.flexibleWidth = 0;
            AddText(row.transform, "V", v, FMedium, 12, Ink, TextAlignmentOptions.TopLeft, true, 4f)
                .gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            if (divider)
            {
                var dv = NewChild("Divider", parent);
                var img = dv.AddComponent<Image>(); img.color = Divider; img.raycastTarget = false;
                var le = dv.AddComponent<LayoutElement>(); le.minHeight = le.preferredHeight = 1;
            }
        }

        // 작은 라운드 칩 (배지/태그). ContentSizeFitter로 텍스트에 맞춰 크기.
        static GameObject Chip(string name, Transform parent, Color bg, string text, Color textColor)
        {
            var chip = Card(name, parent, bg, 7);
            var h = Hlg(chip, 8, 8, 3, 3, 0, false);
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleCenter;
            var csf = chip.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            chip.AddComponent<LayoutElement>().flexibleWidth = 0;
            AddText(chip.transform, "T", text, FSemiBold, 11, textColor, TextAlignmentOptions.Center, false);
            return chip;
        }

        static GameObject NewChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static VerticalLayoutGroup Vlg(GameObject go, int l, int r, int t, int b, float spacing, bool controlH)
        {
            var v = go.GetComponent<VerticalLayoutGroup>() ?? go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(l, r, t, b);
            v.spacing = spacing;
            v.childControlWidth = true; v.childControlHeight = controlH;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.UpperLeft;
            return v;
        }

        static HorizontalLayoutGroup Hlg(GameObject go, int l, int r, int t, int b, float spacing, bool controlH)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(l, r, t, b);
            h.spacing = spacing;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.UpperLeft;
            return h;
        }

        static HorizontalLayoutGroup Row(string name, Transform parent, float spacing, TextAnchor align, bool controlH)
        {
            var go = NewChild(name, parent);
            var h = Hlg(go, 0, 0, 0, 0, spacing, controlH);
            h.childAlignment = align;
            return h;
        }

        static void Fitter(GameObject go)
        {
            var f = go.GetComponent<ContentSizeFitter>() ?? go.AddComponent<ContentSizeFitter>();
            f.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static GameObject Card(string name, Transform parent, Color color, float radius)
        {
            var go = NewChild(name, parent);
            var img = go.AddComponent<MPImage>();
            var so = new SerializedObject(img);
            so.FindProperty("m_Color").colorValue = color;
            so.FindProperty("m_DrawShape").enumValueIndex = 3; // Rectangle
            so.FindProperty("m_FalloffDistance").floatValue = 0.5f;
            so.FindProperty("m_Rectangle.m_UniformCornerRadius").boolValue = false;
            so.FindProperty("m_Rectangle.m_CornerRadius").vector4Value = new Vector4(radius, radius, radius, radius);
            so.ApplyModifiedPropertiesWithoutUndo();
            img.raycastTarget = false;
            return go;
        }

        static TextMeshProUGUI AddText(Transform parent, string name, string text, TMP_FontAsset font,
            float size, Color color, TextAlignmentOptions align, bool wrap, float lineSpacing = 0f)
        {
            var go = NewChild(name, parent);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.lineSpacing = lineSpacing;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static TMP_FontAsset LoadFont(string name)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + name + ".asset");
        }

        static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}

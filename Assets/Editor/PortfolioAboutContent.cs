using System.Collections.Generic;
using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PortfolioOS.EditorTools
{
    /// <summary>
    /// AboutWindow.prefab의 Content/Viewport/ScrollContent 하위에
    /// 이력서 1페이지(소개·핵심역량·경력·학력/자격/교육·프로필·슬로건)를
    /// 구조화된 UGUI 레이아웃으로 구축한다.
    ///
    /// 디자인 기반: scratchpad/about-mockup.html (HTML/CSS 시안).
    /// 폰트: WantedSans SDF(가중치별). 카드 라운드: MPUIKit MPImage(DrawShape=Rectangle).
    /// 콘텐츠 SSOT: C:\Users\zxc98\Documents\GitHub\-\김민영_이력서_2026.pptx (1페이지).
    ///
    /// 메뉴: Portfolio → Build About Content
    /// </summary>
    public static class PortfolioAboutContent
    {
        const string AboutPrefab = "Assets/Resources/Windows/AboutWindow.prefab";
        const string FontDir = "Assets/UGUIWindowSample/Fonts/";
        const string HandCursorPath = "Assets/UGUIWindowSample/Posys-Cursors-Improved-by-wrinkdater/BandiView_Posy hand.png";

        // 링크 hover 커서(손가락). 없어도 링크 클릭은 동작.
        static Texture2D FHandCursor;
        static readonly Vector2 HandHotspot = new Vector2(8, 4);

        // ---- 팔레트 (about-mockup.html 라이트 테마) ----
        static readonly Color Ink       = Hex("1D1D1F");
        static readonly Color Ink2      = Hex("6E6E73");
        static readonly Color Ink3      = Hex("8E8E93");
        static readonly Color Panel     = Hex("F5F5F7");
        static readonly Color Panel2    = Hex("ECECF0");
        static readonly Color Divider   = Hex("E5E5EA");
        static readonly Color Accent    = Hex("0A84FF");
        static readonly Color AccentSoft = new Color(10f/255f, 132f/255f, 1f, 0.10f);
        static readonly Color White     = Color.white;

        // ---- 폰트 (Build()에서 로드) ----
        static TMP_FontAsset FRegular, FMedium, FSemiBold, FBold, FExtraBold;

        [MenuItem("Portfolio/Build About Content")]
        public static void Build()
        {
            FRegular   = LoadFont("WantedSans-Regular SDF");
            FMedium    = LoadFont("WantedSans-Medium SDF");
            FSemiBold  = LoadFont("WantedSans-SemiBold SDF");
            FBold      = LoadFont("WantedSans-Bold SDF");
            FExtraBold = LoadFont("WantedSans-ExtraBold SDF");
            if (FRegular == null || FMedium == null || FSemiBold == null || FBold == null || FExtraBold == null)
            {
                Debug.LogError("[About] WantedSans SDF 폰트 로드 실패. " + FontDir + " 확인.");
                return;
            }

            FHandCursor = AssetDatabase.LoadAssetAtPath<Texture2D>(HandCursorPath);
            if (FHandCursor == null)
            {
                Debug.LogWarning("[About] 손가락 커서 텍스처 없음(" + HandCursorPath + "). 링크 hover 커서 없이 진행.");
            }

            var root = PrefabUtility.LoadPrefabContents(AboutPrefab);
            try
            {
                var scroll = root.transform.Find("Content/Viewport/ScrollContent") as RectTransform;
                if (scroll == null)
                {
                    Debug.LogError("[About] Content/Viewport/ScrollContent 없음.");
                    return;
                }

                // ScrollRect: 세로 스크롤만.
                var scrollRect = root.transform.Find("Content") != null
                    ? root.transform.Find("Content").GetComponent<ScrollRect>() : null;
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
                var rootVlg = Vlg(scroll.gameObject, 20, 20, 22, 22, 22, true);
                rootVlg.childAlignment = TextAnchor.UpperCenter;
                Fitter(scroll.gameObject);

                BuildHero(scroll);
                BuildIntro(scroll);
                BuildCompetency(scroll);
                BuildCareer(scroll);
                BuildEducation(scroll);
                BuildProfile(scroll);
                BuildSlogan(scroll);

                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll);

                PrefabUtility.SaveAsPrefabAsset(root, AboutPrefab);
                Debug.Log("[About] Content built. height=" + scroll.rect.height.ToString("0"));
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
            var hero = Row("Hero", parent, 14, TextAnchor.MiddleLeft, false);
            hero.padding = new RectOffset(0, 0, 0, 0);

            // Avatar (원형)
            var avatar = new GameObject("Avatar", typeof(RectTransform));
            avatar.transform.SetParent(hero.transform, false);
            Circle(avatar, Accent);
            var le = avatar.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 66; le.minHeight = le.preferredHeight = 66; le.flexibleWidth = 0;
            var init = AddText(avatar.transform, "Initial", "김", FExtraBold, 30, White, TextAlignmentOptions.Center, false);
            Stretch((RectTransform)init.transform);

            // Who (이름 + 역할)
            var who = new GameObject("Who", typeof(RectTransform));
            who.transform.SetParent(hero.transform, false);
            var whoV = Vlg(who, 0, 0, 0, 0, 4, true);
            whoV.childAlignment = TextAnchor.MiddleLeft;
            who.AddComponent<LayoutElement>().flexibleWidth = 1;
            AddText(who.transform, "Name", "김민영", FExtraBold, 26, Ink, TextAlignmentOptions.Left, false);
            AddText(who.transform, "Role",
                "Kim Min-Yeong · 유니티 · 언리얼 멀티 스택 게임 클라이언트 프로그래머",
                FMedium, 12.5f, Ink2, TextAlignmentOptions.Left, true);
        }

        static void BuildIntro(Transform parent)
        {
            var s = Section("Intro", parent, "소개");
            AddText(s, "Lead",
                "유니티와 언리얼, C#과 C++, 클라이언트와 서버를 경계 없이 넘나들며, 기획된 기능을 " +
                "실제 출시·배포까지 완수하는 멀티 스택 역량을 갖춘 유니티·언리얼 프로그래머입니다. " +
                "와이드브레인에서 고아미 캠프의 클라이언트를 단독 개발해 양대 스토어에 출시했고 " +
                "Colyseus 기반 서버-클라이언트 동기화를 직접 구현했으며, 언리얼 부트캠프에서 팀장으로 " +
                "GAS·코어 시스템 개발을 주도했습니다.",
                FRegular, 14, Ink, TextAlignmentOptions.TopLeft, true, 8f);
        }

        static void BuildCompetency(Transform parent)
        {
            var s = Section("Competency", parent, "핵심 역량");
            var card = Card("Card", s, Panel, 14);
            var v = Vlg(card, 16, 16, 13, 13, 11, true);

            CompRow(v.transform, "게임 엔진", "언리얼 엔진 · 유니티 엔진");
            CompRow(v.transform, "네트워크 / 멀티플레이", "유니티 Colyseus, Photon · 언리얼 Dedicated Server, Replication");
            CompRow(v.transform, "언어", "C++ · C#");
            CompRow(v.transform, "형상관리 / 협업", "Git · Notion");
        }

        static void CompRow(Transform parent, string k, string val)
        {
            var row = Row("Row", parent, 12, TextAnchor.UpperLeft, false);
            var kt = AddText(row.transform, "K", k, FSemiBold, 12.5f, Ink2, TextAlignmentOptions.TopLeft, true);
            var kle = kt.gameObject.AddComponent<LayoutElement>();
            kle.minWidth = kle.preferredWidth = 128; kle.flexibleWidth = 0;
            var vt = AddText(row.transform, "V", val, FMedium, 12.5f, Ink, TextAlignmentOptions.TopLeft, true);
            vt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        static void BuildCareer(Transform parent)
        {
            var s = Section("Career", parent, "경력");
            var tl = Vlg(NewChild("Timeline", s), 0, 0, 0, 0, 20, true);

            var job1 = Job(tl.transform, "와이드브레인", "2023.10 – 2025.03 · 1년 5개월",
                "유니티 클라이언트 프로그래머", true);
            Bullet(job1, "<b>고아미 캠프</b> (육군인사사령부) — 클라이언트 전 영역 단독 개발, " +
                         "Google Play · App Store 양대 스토어 출시. Colyseus 기반 서버-클라이언트 동기화 및 채팅 구현");
            Bullet(job1, "<b>협력 퀴즈</b> (아이스크림 에듀) — 교육용 앱 클라이언트 버그 수정 및 유지보수");
            Chips(job1, new[] { "Unity (C#)", "Colyseus", "Node.js", "Git" });

            Job(tl.transform, "GS25", "2020.05 – 2021.05 · 1년",
                "편의점 주말 야간 근무 (아르바이트)", false);
        }

        // 반환: 콘텐츠 컬럼 Transform (Bullet/Chips가 여기에 추가됨)
        static Transform Job(Transform parent, string org, string date, string title, bool rail)
        {
            var job = Row("Job", parent, 0, TextAnchor.UpperLeft, false);

            // 레일 컬럼 (dot + 연결선)
            var railGo = NewChild("Rail", job.transform);
            var rle = railGo.AddComponent<LayoutElement>();
            rle.minWidth = rle.preferredWidth = 18; rle.flexibleWidth = 0;
            if (rail)
            {
                var line = NewChild("Line", railGo.transform);
                var img = line.AddComponent<Image>(); img.color = Divider; img.raycastTarget = false;
                var lrt = (RectTransform)line.transform;
                lrt.anchorMin = new Vector2(0.5f, 0); lrt.anchorMax = new Vector2(0.5f, 1);
                lrt.pivot = new Vector2(0.5f, 0.5f); lrt.sizeDelta = new Vector2(2, 0);
                lrt.anchoredPosition = new Vector2(0, 0);
            }
            var dotGo = NewChild("Dot", railGo.transform);
            Circle(dotGo, Accent);
            var drt = (RectTransform)dotGo.transform;
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 1); drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(10, 10); drt.anchoredPosition = new Vector2(0, -7);

            // 콘텐츠 컬럼
            var col = NewChild("Content", job.transform);
            var cv = Vlg(col, 0, 0, 0, 0, 6, true);
            col.AddComponent<LayoutElement>().flexibleWidth = 1;

            var top = Row("Top", cv.transform, 10, TextAnchor.LowerLeft, false);
            var o = AddText(top.transform, "Org", org, FBold, 14, Ink, TextAlignmentOptions.BottomLeft, false);
            o.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var d = AddText(top.transform, "Date", date, FMedium, 11.5f, Ink3, TextAlignmentOptions.BottomRight, false);
            d.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0;

            AddText(cv.transform, "Title", title, FSemiBold, 12, Accent, TextAlignmentOptions.TopLeft, false);
            return cv.transform;
        }

        static void Bullet(Transform jobContent, string text)
        {
            // 첫 Bullet 앞에 Bullets 컨테이너가 없으면 생성
            var bullets = jobContent.Find("Bullets");
            if (bullets == null)
            {
                var go = NewChild("Bullets", jobContent);
                Vlg(go, 0, 0, 3, 0, 6, true);
                bullets = go.transform;
            }
            AddText(bullets, "Bullet", "<color=#8E8E93>·</color>  " + text,
                FRegular, 12, Ink2, TextAlignmentOptions.TopLeft, true, 6f);
        }

        static void Chips(Transform jobContent, string[] labels)
        {
            var go = NewChild("Chips", jobContent);
            var h = Hlg(go, 0, 0, 4, 0, 6, false);
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            foreach (var lb in labels)
            {
                var chip = Card("Chip", go.transform, Panel2, 7);
                var ch = Hlg(chip, 8, 8, 3, 3, 0, false);
                ch.childControlWidth = true; ch.childControlHeight = true;
                ch.childForceExpandWidth = false; ch.childForceExpandHeight = false;
                ch.childAlignment = TextAnchor.MiddleCenter;
                var csf = chip.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                AddText(chip.transform, "T", lb, FSemiBold, 11, Ink2, TextAlignmentOptions.Center, false);
            }
        }

        static void BuildEducation(Transform parent)
        {
            var s = Section("Education", parent, "학력 · 자격 · 교육");
            var list = Vlg(NewChild("List", s), 0, 0, 0, 0, 0, true).transform;

            EduItem(list, "협성대학교 컴퓨터공학과 (학사)", "학점 3.41 / 4.5", "2017.03 – 2023.02", true);
            EduItem(list, "정보처리기사", "자격증", "2022.11", true);
            EduItem(list, "[기업연계] 언리얼 엔진을 활용한 게임 개발자 부트캠프", "디벨로켓에듀", "2025.11 – 2026.07", true);
            EduItem(list, "게임엔진 프로그래밍 전문가 양성과정 9기", "아텐츠게임아카데미", "2022.12 – 2023.06", false);
        }

        static void EduItem(Transform parent, string title, string sub, string date, bool divider)
        {
            var item = Row("Item", parent, 10, TextAnchor.UpperLeft, false);
            item.padding = new RectOffset(0, 0, 11, 11);

            var main = NewChild("Main", item.transform);
            Vlg(main, 0, 0, 0, 0, 2, true);
            main.AddComponent<LayoutElement>().flexibleWidth = 1;
            AddText(main.transform, "T", title, FSemiBold, 12.5f, Ink, TextAlignmentOptions.TopLeft, true);
            AddText(main.transform, "S", sub, FRegular, 12, Ink2, TextAlignmentOptions.TopLeft, true);

            var dt = AddText(item.transform, "D", date, FMedium, 11.5f, Ink3, TextAlignmentOptions.TopRight, false);
            dt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0;

            if (divider)
            {
                var dv = NewChild("Divider", parent);
                var img = dv.AddComponent<Image>(); img.color = Divider; img.raycastTarget = false;
                var le = dv.AddComponent<LayoutElement>(); le.minHeight = le.preferredHeight = 1;
            }
        }

        static void BuildProfile(Transform parent)
        {
            var s = Section("Profile", parent, "프로필");
            var grid = Vlg(NewChild("Grid", s), 0, 0, 0, 0, 10, true).transform;

            var r1 = ProfileRow(grid);
            ProfileCell(r1, "Birth", "1998 / 06 / 14");
            ProfileCell(r1, "Military", "육군 병장 만기 전역\n(2018.04 – 2019.12)");

            var r2 = ProfileRow(grid);
            ProfileCell(r2, "Address", "경기도 고양시 일산서구 킨텍스로 300");

            var r3 = ProfileRow(grid);
            ProfileCell(r3, "Phone", "");
            ProfileCell(r3, "E-mail", "zxc9876zxc@gmail.com", "mailto:zxc9876zxc@gmail.com");

            var r4 = ProfileRow(grid);
            ProfileCell(r4, "GitHub", "github.com/ThunderVolt45", "https://github.com/ThunderVolt45");
        }

        static Transform ProfileRow(Transform parent)
        {
            var row = Row("Row", parent, 10, TextAnchor.UpperLeft, false);
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.childForceExpandHeight = true; h.childControlHeight = true; // 셀 높이 동일화
            return row.transform;
        }

        static void ProfileCell(Transform parent, string label, string value, string url = null)
        {
            var cell = Card("Cell", parent, Panel, 12);
            Vlg(cell, 15, 15, 12, 12, 4, true);
            cell.AddComponent<LayoutElement>().flexibleWidth = 1;
            var l = AddText(cell.transform, "L", label.ToUpperInvariant(), FSemiBold, 10, Ink3, TextAlignmentOptions.TopLeft, false);
            l.characterSpacing = 4f;
            var v = AddText(cell.transform, "V",
                url != null ? "<link=\"" + url + "\">" + value + "</link>" : value,
                FMedium, 12.5f, url != null ? Accent : Ink, TextAlignmentOptions.TopLeft, true);
            if (url != null)
            {
                MakeLink(v);
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
                "<size=200%><color=#0A84FF><b>“</b></color></size>  교육으로 기초를 다지고, 현업과 프로젝트로 이를 검증하는 — " +
                "유니티와 언리얼, C#과 C++ 어느 환경에서도 대응할 수 있는 넓은 기술 폭과, " +
                "서비스를 출시까지 끝맺는 개발의 깊이를 갖춘 개발자",
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

        static void Circle(GameObject go, Color color)
        {
            var img = go.AddComponent<MPImage>();
            var so = new SerializedObject(img);
            so.FindProperty("m_Color").colorValue = color;
            so.FindProperty("m_DrawShape").enumValueIndex = 1; // Circle
            so.FindProperty("m_FalloffDistance").floatValue = 0.5f;
            so.FindProperty("m_Circle.m_FitRadius").boolValue = true; // 렉트에 맞춰 반지름 자동
            so.ApplyModifiedPropertiesWithoutUndo();
            img.raycastTarget = false;
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

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}

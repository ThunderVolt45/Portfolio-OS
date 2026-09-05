using System.Collections.Generic;
using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PortfolioOS.EditorTools
{
    /// <summary>
    /// 프로젝트 PDF 창 3종과 이력서 창을 UGUISampleWindow 계열의 소개 화면으로 구축한다.
    /// PDF는 Unity 안에 임베드하지 않으며, DetailsButton을 통해 브라우저 기본 PDF 뷰어로 연다.
    /// 프로젝트 문구 SSOT: C:/Users/zxc98/Documents/GitHub/-/portfolio/projects/*.md
    /// </summary>
    public static class PortfolioProjectContent
    {
        const string FontDir = "Assets/UGUIWindowSample/Fonts/";

        static TMP_FontAsset FRegular;
        static TMP_FontAsset FMedium;
        static TMP_FontAsset FSemiBold;
        static TMP_FontAsset FBold;
        static TMP_FontAsset FExtraBold;

        sealed class Palette
        {
            public Color Ink;
            public Color Ink2;
            public Color Panel;
            public Color Divider;
            public Color Accent;
            public Color AccentSoft;

            public Palette(string ink, string ink2, string panel, string divider, string accent, string accentSoft)
            {
                Ink = Hex(ink);
                Ink2 = Hex(ink2);
                Panel = Hex(panel);
                Divider = Hex(divider);
                Accent = Hex(accent);
                AccentSoft = Hex(accentSoft);
            }
        }

        sealed class Highlight
        {
            public string Tag;
            public string Title;
            public string Description;

            public Highlight(string tag, string title, string description)
            {
                Tag = tag;
                Title = title;
                Description = description;
            }
        }

        sealed class Spec
        {
            public string PrefabPath;
            public string WindowTitle;
            public string ProjectTitle;
            public string Role;
            public string FocusTag;
            public string Focus;
            public string Summary;
            public string IconPath;
            public string[][] InfoRows;
            public Highlight[] Highlights;
            public Palette Colors;
            public string ButtonLabel;
        }

        [MenuItem("Portfolio/Build Project Introduction Content")]
        public static void BuildAll()
        {
            if (!LoadFonts())
            {
                return;
            }

            Build(BrawlStars());
            Build(NovaRevolution());
            Build(ProjectBlackout());
            Build(Resume());
            AssetDatabase.SaveAssets();
            Debug.Log("[PortfolioProjectContent] Project introductions + resume launcher built.");
        }

        static Spec BrawlStars()
        {
            return new Spec
            {
                PrefabPath = "Assets/Resources/Windows/BrawlStarsTPSDocWindow.prefab",
                WindowTitle = "BrawlStarsTPS",
                ProjectTitle = "BrawlStarsTPS",
                Role = "TPS PvE · Unreal Engine 5.7 · C++ 개인 프로젝트",
                FocusTag = "C++",
                Focus = "GAS 기반 전투와 런타임 최적화를 C++로 단독 구현",
                Summary = "모바일 탑다운 슈팅의 전투를 3인칭 슈팅으로 재해석한 PvE 프로젝트입니다. " +
                          "데이터 주도 GAS 전투를 중심에 두고, 발사체와 환경 액터의 반복 생성 비용을 줄이는 " +
                          "범용 오브젝트 풀링과 전략 기반 AI를 함께 설계했습니다.",
                IconPath = "Assets/Textures/icon_main.png",
                InfoRows = new[]
                {
                    new[] { "장르 · 플랫폼", "TPS PvE (vs AI 봇) · PC" },
                    new[] { "기간", "2026.01.19 ~ 2026.02.13 · 약 4주" },
                    new[] { "인원 · 역할", "개인 프로젝트 · 전 영역 단독 개발" },
                    new[] { "담당", "GAS 전투 · 오브젝트 풀링 · 게임 모드 · AI · UI" }
                },
                Highlights = new[]
                {
                    new Highlight("GAS", "데이터 주도 전투", "체력·탄약·슈퍼·가젯을 Attribute와 Ability로 모델링해 확장 경로를 통일했습니다."),
                    new Highlight("POOL", "재귀적 프리워밍", "하위 의존성까지 자동 추적하는 World Subsystem 풀로 런타임 할당과 GC를 예방합니다."),
                    new Highlight("AI", "전략 기반 봇", "Behavior Tree가 상황별 전략을 고르고 브롤러별 전술은 동적 서브트리로 교체합니다.")
                },
                Colors = new Palette("24180F", "6B5847", "FFF8EE", "E9D7C2", "A84A00", "FFF0D2"),
                ButtonLabel = "BrawlStarsTPS 기술 문서 자세히 보기"
            };
        }

        static Spec NovaRevolution()
        {
            return new Spec
            {
                PrefabPath = "Assets/Resources/Windows/NovaRevolutionDocWindow.prefab",
                WindowTitle = "Nova-Revolution",
                ProjectTitle = "Nova-Revolution",
                Role = "실시간 전략 · Unreal Engine 5.7 · C++ 팀 프로젝트",
                FocusTag = "RTS",
                Focus = "부품 조립형 유닛 전투와 사령관 단위 전략 AI",
                Summary = "노바 1492의 부품 조립과 실시간 전략 전투를 재구성한 3인 팀 프로젝트입니다. " +
                          "팀장으로서 Core·GAS·GameplayCue·AI를 담당했고, 데이터만으로 유닛 조합이 달라지는 " +
                          "전투 구조와 자원 운영·빌드 오더를 수행하는 사령관 AI를 구현했습니다.",
                IconPath = "Assets/Textures/노바 1492 로고.png",
                InfoRows = new[]
                {
                    new[] { "장르 · 플랫폼", "실시간 전략(RTS) · PC (Windows)" },
                    new[] { "기간", "2026.02 ~ 2026.04 · 약 4주" },
                    new[] { "인원 · 역할", "3인 팀 · 팀장" },
                    new[] { "담당", "Core · GAS · GameplayCue · AI" }
                },
                Highlights = new[]
                {
                    new Highlight("DATA", "부품 조립형 전투", "부품의 스탯과 어빌리티를 합산해 코드 변경 없이 새로운 유닛 조합을 만듭니다."),
                    new Highlight("AI", "사령관 AI", "진영 전략과 개별 유닛 행동을 계층 분리하고 웨이브 단위로 생산과 공격을 지휘합니다."),
                    new Highlight("FOW", "안개와 연출 통합", "GameplayCue를 시야 가시성과 연동해 비가시 영역의 전투 정보 노출을 차단합니다.")
                },
                Colors = new Palette("101828", "55647A", "F4F7FC", "DCE5F0", "2E5DA8", "E8F1FF"),
                ButtonLabel = "Nova-Revolution 기술 문서 자세히 보기"
            };
        }

        static Spec ProjectBlackout()
        {
            return new Spec
            {
                PrefabPath = "Assets/Resources/Windows/ProjectBlackoutDocWindow.prefab",
                WindowTitle = "Project Blackout",
                ProjectTitle = "Project Blackout",
                Role = "협동 TPS 소울라이크 · Unreal Engine 5.7.4 · C++",
                FocusTag = "CO-OP",
                Focus = "Dedicated Server 기반 4인 협동 보스 레이드",
                Summary = "4인이 병과 역할을 나눠 보스 레이드를 공략하는 협동 PvE 프로젝트입니다. " +
                          "4인 팀의 팀장으로 Core·GAS·GameplayCue·UI/UX를 맡아, 서버 권위 전투와 " +
                          "다운·부활을 견디는 상태 구조, 이벤트 기반 HUD를 설계했습니다.",
                IconPath = "Assets/Textures/T_Blackout_Icon_B_Transparent.png",
                InfoRows = new[]
                {
                    new[] { "장르 · 플랫폼", "TPS 소울라이크 협동 PvE · PC (Steam)" },
                    new[] { "기간", "2026.04.24 ~ 2026.06.19 · 약 8주" },
                    new[] { "인원 · 역할", "4인 팀 · 팀장" },
                    new[] { "담당", "Core · GAS · GameplayCue · UI/UX" }
                },
                Highlights = new[]
                {
                    new Highlight("NET", "서버 권위 전투", "Dedicated Server 전용 구조에서 비용·쿨다운·판정을 GAS의 권위 경로로 일원화했습니다."),
                    new Highlight("STATE", "지속되는 플레이어 상태", "ASC를 PlayerState가 소유해 다운·부활·체크포인트 리스폰에도 상태가 유지됩니다."),
                    new Highlight("UI", "이벤트 기반 HUD", "WidgetController가 게임 상태를 표시 이벤트로 변환하고 값이 바뀔 때만 UI를 갱신합니다.")
                },
                Colors = new Palette("20191B", "68565B", "FAF5F6", "E8D8DB", "B4232E", "FDEBED"),
                ButtonLabel = "Project Blackout 기술 문서 자세히 보기"
            };
        }

        static Spec Resume()
        {
            return new Spec
            {
                PrefabPath = "Assets/Resources/Windows/DocumentViewerWindow.prefab",
                WindowTitle = "Resume",
                ProjectTitle = "김민영 이력서",
                Role = "게임 클라이언트 · 서버 개발 포트폴리오",
                FocusTag = "PDF",
                Focus = "브라우저에서 더 선명하고 편하게 읽기",
                Summary = "전체 이력서는 브라우저의 기본 PDF 뷰어에서 열립니다. 확대·검색·다운로드 같은 " +
                          "브라우저 기본 조작을 그대로 사용할 수 있으며, 이 창을 닫지 않고 새 탭에서 확인할 수 있습니다.",
                IconPath = "Assets/Portfolio/Icons/Resume.png",
                InfoRows = new[]
                {
                    new[] { "문서", "김민영 이력서 2026" },
                    new[] { "형식", "PDF · 브라우저 새 탭" },
                    new[] { "공개 연락", "zxc9876zxc@gmail.com" }
                },
                Highlights = new Highlight[0],
                Colors = new Palette("1D1D1F", "6E6E73", "F5F5F7", "E5E5EA", "315FCB", "EAF1FF"),
                ButtonLabel = "이력서 PDF 자세히 보기"
            };
        }

        static void Build(Spec spec)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(spec.PrefabPath);
            if (root == null)
            {
                Debug.LogError("[PortfolioProjectContent] Prefab not found: " + spec.PrefabPath);
                return;
            }

            try
            {
                SetDefaultTitle(root, spec.WindowTitle);

                RectTransform scroll = root.transform.Find("Content/Viewport/ScrollContent") as RectTransform;
                if (scroll == null)
                {
                    Debug.LogError("[PortfolioProjectContent] ScrollContent not found: " + spec.PrefabPath);
                    return;
                }

                Transform content = root.transform.Find("Content");
                ScrollRect scrollRect = content != null ? content.GetComponent<ScrollRect>() : null;
                if (scrollRect != null)
                {
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                }

                var children = new List<GameObject>();
                foreach (Transform child in scroll)
                {
                    children.Add(child.gameObject);
                }
                foreach (GameObject child in children)
                {
                    Object.DestroyImmediate(child);
                }

                scroll.anchorMin = new Vector2(0f, 1f);
                scroll.anchorMax = new Vector2(1f, 1f);
                scroll.pivot = new Vector2(0.5f, 1f);
                scroll.anchoredPosition = Vector2.zero;
                scroll.sizeDelta = new Vector2(0f, scroll.sizeDelta.y);

                VerticalLayoutGroup layout = Vlg(scroll.gameObject, 22, 22, 24, 28, 22f);
                layout.childAlignment = TextAnchor.UpperCenter;
                Fitter(scroll.gameObject);

                BuildHero(scroll, spec);
                BuildFocus(scroll, spec);
                BuildSummary(scroll, spec);
                BuildDetailsButton(scroll, spec);
                BuildInfo(scroll, spec);
                if (spec.Highlights.Length > 0)
                {
                    BuildHighlights(scroll, spec);
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll);
                PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
                Debug.Log("[PortfolioProjectContent] Built " + spec.WindowTitle + " height=" + scroll.rect.height.ToString("0"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void BuildHero(Transform parent, Spec spec)
        {
            HorizontalLayoutGroup hero = Row("Hero", parent, 16f, TextAnchor.MiddleLeft);

            GameObject tile = Card("AppIcon", hero.transform, spec.Colors.AccentSoft, 14f, false);
            LayoutElement tileLayout = tile.AddComponent<LayoutElement>();
            tileLayout.minWidth = tileLayout.preferredWidth = 70f;
            tileLayout.minHeight = tileLayout.preferredHeight = 70f;
            tileLayout.flexibleWidth = 0f;

            GameObject art = NewChild("Artwork", tile.transform);
            Image image = art.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.IconPath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            RectTransform artRect = (RectTransform)art.transform;
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = new Vector2(7f, 7f);
            artRect.offsetMax = new Vector2(-7f, -7f);

            GameObject titleColumn = NewChild("Title", hero.transform);
            Vlg(titleColumn, 0, 0, 0, 0, 5f);
            titleColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddText(titleColumn.transform, "Name", spec.ProjectTitle, FExtraBold, 21f, spec.Colors.Ink,
                TextAlignmentOptions.Left, false);
            AddText(titleColumn.transform, "Role", spec.Role, FMedium, 12.5f, spec.Colors.Ink2,
                TextAlignmentOptions.Left, true, 3f);
        }

        static void BuildFocus(Transform parent, Spec spec)
        {
            GameObject callout = Card("Focus", parent, spec.Colors.AccentSoft, 14f, false);
            HorizontalLayoutGroup row = Hlg(callout, 15, 15, 13, 13, 11f);
            row.childAlignment = TextAnchor.MiddleLeft;

            GameObject label = Chip("Label", row.transform, spec.Colors.Accent, spec.FocusTag, Color.white);
            label.GetComponent<LayoutElement>().flexibleWidth = 0f;

            TextMeshProUGUI text = AddText(row.transform, "Text", spec.Focus, FSemiBold, 12.5f,
                spec.Colors.Ink, TextAlignmentOptions.Left, true, 4f);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        static void BuildSummary(Transform parent, Spec spec)
        {
            Transform section = Section("Introduction", parent, "프로젝트 소개", spec.Colors.Accent);
            AddText(section, "Body", spec.Summary, FRegular, 13.5f, spec.Colors.Ink,
                TextAlignmentOptions.TopLeft, true, 7f);
        }

        static void BuildInfo(Transform parent, Spec spec)
        {
            Transform section = Section("BasicInfo", parent, "기본 정보", spec.Colors.Accent);
            GameObject card = Card("Card", section, spec.Colors.Panel, 14f, false);
            Vlg(card, 0, 0, 0, 0, 0f);

            for (int i = 0; i < spec.InfoRows.Length; i++)
            {
                InfoRow(card.transform, spec.InfoRows[i][0], spec.InfoRows[i][1], spec, i < spec.InfoRows.Length - 1);
            }
        }

        static void BuildHighlights(Transform parent, Spec spec)
        {
            Transform section = Section("Highlights", parent, "대표 구현", spec.Colors.Accent);
            GameObject panel = Card("Panel", section, spec.Colors.Panel, 14f, false);
            Vlg(panel, 0, 0, 0, 0, 0f);

            for (int i = 0; i < spec.Highlights.Length; i++)
            {
                HighlightRow(panel.transform, spec.Highlights[i], spec, i < spec.Highlights.Length - 1);
            }
        }

        static void BuildDetailsButton(Transform parent, Spec spec)
        {
            GameObject buttonObject = Card("DetailsButton", parent, spec.Colors.Accent, 13f, true);
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 50f;

            MPImage image = buttonObject.GetComponent<MPImage>();
            image.raycastTarget = true;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TextMeshProUGUI label = AddText(buttonObject.transform, "Label", spec.ButtonLabel, FSemiBold, 13f,
                Color.white, TextAlignmentOptions.Center, false);
            Stretch((RectTransform)label.transform);

            AddText(parent, "OpenHint", "브라우저 새 탭에서 PDF가 열립니다.", FMedium, 11.5f,
                spec.Colors.Ink2, TextAlignmentOptions.Center, false);
        }

        static void InfoRow(Transform parent, string key, string value, Spec spec, bool divider)
        {
            HorizontalLayoutGroup row = Row("Row", parent, 12f, TextAnchor.UpperLeft);
            row.padding = new RectOffset(15, 15, 11, 11);

            TextMeshProUGUI keyText = AddText(row.transform, "Key", key, FSemiBold, 12f,
                spec.Colors.Ink2, TextAlignmentOptions.TopLeft, true);
            LayoutElement keyLayout = keyText.gameObject.AddComponent<LayoutElement>();
            keyLayout.minWidth = keyLayout.preferredWidth = 96f;
            keyLayout.flexibleWidth = 0f;

            TextMeshProUGUI valueText = AddText(row.transform, "Value", value, FMedium, 12f,
                spec.Colors.Ink, TextAlignmentOptions.TopLeft, true, 4f);
            valueText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            if (divider)
            {
                Divider(parent, spec.Colors.Divider);
            }
        }

        static void HighlightRow(Transform parent, Highlight highlight, Spec spec, bool divider)
        {
            HorizontalLayoutGroup row = Row("Row", parent, 12f, TextAnchor.UpperLeft);
            row.padding = new RectOffset(15, 15, 13, 13);

            GameObject tag = Chip("Tag", row.transform, spec.Colors.AccentSoft, highlight.Tag, spec.Colors.Accent);
            LayoutElement tagLayout = tag.GetComponent<LayoutElement>();
            tagLayout.minWidth = 48f;
            tagLayout.flexibleWidth = 0f;

            GameObject textColumn = NewChild("Text", row.transform);
            Vlg(textColumn, 0, 0, 0, 0, 4f);
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddText(textColumn.transform, "Title", highlight.Title, FSemiBold, 12.5f,
                spec.Colors.Ink, TextAlignmentOptions.TopLeft, true);
            AddText(textColumn.transform, "Description", highlight.Description, FMedium, 11.7f,
                spec.Colors.Ink2, TextAlignmentOptions.TopLeft, true, 4f);

            if (divider)
            {
                Divider(parent, spec.Colors.Divider);
            }
        }

        static Transform Section(string name, Transform parent, string label, Color accent)
        {
            GameObject section = NewChild(name, parent);
            Vlg(section, 0, 0, 0, 0, 10f);
            TextMeshProUGUI heading = AddText(section.transform, "Heading", label, FSemiBold, 11.5f,
                accent, TextAlignmentOptions.Left, false);
            heading.characterSpacing = 4f;
            return section.transform;
        }

        static GameObject Chip(string name, Transform parent, Color background, string text, Color textColor)
        {
            GameObject chip = Card(name, parent, background, 7f, false);
            HorizontalLayoutGroup layout = Hlg(chip, 8, 8, 3, 3, 0f);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            ContentSizeFitter fitter = chip.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            chip.AddComponent<LayoutElement>().flexibleWidth = 0f;
            AddText(chip.transform, "Text", text, FSemiBold, 10.5f, textColor,
                TextAlignmentOptions.Center, false);
            return chip;
        }

        static void Divider(Transform parent, Color color)
        {
            GameObject divider = NewChild("Divider", parent);
            Image image = divider.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            LayoutElement layout = divider.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 1f;
        }

        static GameObject Card(string name, Transform parent, Color color, float radius, bool raycastTarget)
        {
            GameObject card = NewChild(name, parent);
            MPImage image = card.AddComponent<MPImage>();
            var serialized = new SerializedObject(image);
            serialized.FindProperty("m_Color").colorValue = color;
            serialized.FindProperty("m_DrawShape").enumValueIndex = 3;
            serialized.FindProperty("m_FalloffDistance").floatValue = 0.5f;
            serialized.FindProperty("m_Rectangle.m_UniformCornerRadius").boolValue = false;
            serialized.FindProperty("m_Rectangle.m_CornerRadius").vector4Value = new Vector4(radius, radius, radius, radius);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            image.raycastTarget = raycastTarget;
            return card;
        }

        static GameObject NewChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        static VerticalLayoutGroup Vlg(GameObject target, int left, int right, int top, int bottom, float spacing)
        {
            VerticalLayoutGroup layout = target.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = target.AddComponent<VerticalLayoutGroup>();
            }
            layout.padding = new RectOffset(left, right, top, bottom);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            return layout;
        }

        static HorizontalLayoutGroup Hlg(GameObject target, int left, int right, int top, int bottom, float spacing)
        {
            HorizontalLayoutGroup layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(left, right, top, bottom);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            return layout;
        }

        static HorizontalLayoutGroup Row(string name, Transform parent, float spacing, TextAnchor alignment)
        {
            GameObject row = NewChild(name, parent);
            HorizontalLayoutGroup layout = Hlg(row, 0, 0, 0, 0, spacing);
            layout.childAlignment = alignment;
            return layout;
        }

        static void Fitter(GameObject target)
        {
            ContentSizeFitter fitter = target.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = target.AddComponent<ContentSizeFitter>();
            }
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static TextMeshProUGUI AddText(Transform parent, string name, string text, TMP_FontAsset font,
            float size, Color color, TextAlignmentOptions alignment, bool wrap, float lineSpacing = 0f)
        {
            GameObject child = NewChild(name, parent);
            TextMeshProUGUI label = child.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.lineSpacing = lineSpacing;
            label.raycastTarget = false;
            label.richText = true;
            return label;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void SetDefaultTitle(GameObject root, string title)
        {
            UGUIWindow.UGUIWindow window = root.GetComponent<UGUIWindow.UGUIWindow>();
            if (window == null)
            {
                Debug.LogWarning("[PortfolioProjectContent] UGUIWindow component missing: " + root.name);
                return;
            }

            var serialized = new SerializedObject(window);
            SerializedProperty property = serialized.FindProperty("defaultTitle");
            if (property != null)
            {
                property.stringValue = title;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static bool LoadFonts()
        {
            FRegular = LoadFont("WantedSans-Regular SDF");
            FMedium = LoadFont("WantedSans-Medium SDF");
            FSemiBold = LoadFont("WantedSans-SemiBold SDF");
            FBold = LoadFont("WantedSans-Bold SDF");
            FExtraBold = LoadFont("WantedSans-ExtraBold SDF");
            if (FRegular != null && FMedium != null && FSemiBold != null && FBold != null && FExtraBold != null)
            {
                return true;
            }

            Debug.LogError("[PortfolioProjectContent] WantedSans SDF font load failed: " + FontDir);
            return false;
        }

        static TMP_FontAsset LoadFont(string name)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + name + ".asset");
        }

        static Color Hex(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString("#" + hex, out color);
            return color;
        }
    }
}

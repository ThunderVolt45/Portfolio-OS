using System.Collections.Generic;
using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PortfolioOS.EditorTools
{
    /// <summary>
    /// DpiSettingWindow.prefab의 Content/Viewport/ScrollContent 하위에
    /// UI 배율(DPI) 설정 콘텐츠를 구축하고, DpiSettingWindow의 직렬화 필드
    /// (supportDPI / presetButtons / currentLabel)를 배선한다.
    ///
    /// 폰트: WantedSans SDF. 버튼 라운드: MPUIKit MPImage. 팔레트는 About/UGUISample과 동일 톤.
    ///
    /// 메뉴: Portfolio → Build DPI Setting Content
    /// (스켈레톤 프리팹이 없으면 먼저 Portfolio → Build DPI Setting Window 실행)
    /// </summary>
    public static class PortfolioDpiSettingContent
    {
        const string Prefab = "Assets/Resources/Windows/DpiSettingWindow.prefab";
        const string FontDir = "Assets/UGUIWindowSample/Fonts/";

        /// <summary>선택 가능한 배율. DpiSettingWindow.supportDPI에 그대로 굽는다.</summary>
        static readonly float[] Scales = { 1f, 1.25f, 1.5f, 1.75f, 2f };

        static readonly Color Ink = Hex("1D1D1F");
        static readonly Color Ink2 = Hex("6E6E73");
        static readonly Color Panel = Hex("F5F5F7");

        static TMP_FontAsset FRegular, FSemiBold, FBold;

        [MenuItem("Portfolio/Build DPI Setting Content")]
        public static void Build()
        {
            FRegular = LoadFont("WantedSans-Regular SDF");
            FSemiBold = LoadFont("WantedSans-SemiBold SDF");
            FBold = LoadFont("WantedSans-Bold SDF");
            if (FRegular == null || FSemiBold == null || FBold == null)
            {
                Debug.LogError("[DpiSetting] WantedSans SDF 폰트 로드 실패. " + FontDir + " 확인.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var scroll = root.transform.Find("Content/Viewport/ScrollContent") as RectTransform;
                if (scroll == null)
                {
                    Debug.LogError("[DpiSetting] Content/Viewport/ScrollContent 없음. 먼저 Build DPI Setting Window 실행.");
                    return;
                }

                var win = root.GetComponent<UGUIWindow.DpiSettingWindow>();
                if (win == null)
                {
                    Debug.LogError("[DpiSetting] root에 DpiSettingWindow 컴포넌트 없음. 먼저 Build DPI Setting Window 실행.");
                    return;
                }

                // 내용이 창보다 작으므로 스크롤은 사실상 쓰지 않지만, 배율을 키우면 넘칠 수 있어 세로만 허용.
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
                // controlH: true — 각 자식 높이를 preferred size로 맞춘다(false면 기본 100px씩 차지).
                var vlg = Vlg(scroll.gameObject, 22, 22, 22, 22, 10, true);
                vlg.childAlignment = TextAnchor.UpperCenter;
                Fitter(scroll.gameObject);

                AddText(scroll, "Title", "화면 배율", FBold, 20, Ink, TextAlignmentOptions.TopLeft);
                AddText(scroll, "Desc", "창과 아이콘 등 모든 UI 요소의 크기를 조절합니다.",
                        FRegular, 13, Ink2, TextAlignmentOptions.TopLeft);

                // 배율 프리셋 버튼 행.
                var row = NewChild("PresetRow", scroll);
                var hlg = row.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(0, 0, 8, 4);
                hlg.spacing = 8;
                hlg.childControlWidth = true; hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = false;
                hlg.childAlignment = TextAnchor.MiddleCenter;

                var buttons = new List<Button>();
                foreach (var scale in Scales)
                {
                    buttons.Add(PresetButton(row.transform, scale));
                }

                var label = AddText(scroll, "CurrentLabel", "현재 배율 100%",
                                    FSemiBold, 13, Ink2, TextAlignmentOptions.TopLeft);

                // DpiSettingWindow 필드 배선.
                var so = new SerializedObject(win);

                var dpiProp = so.FindProperty("supportDPI");
                dpiProp.arraySize = Scales.Length;
                for (int i = 0; i < Scales.Length; i++)
                {
                    dpiProp.GetArrayElementAtIndex(i).floatValue = Scales[i];
                }

                var btnProp = so.FindProperty("presetButtons");
                btnProp.arraySize = buttons.Count;
                for (int i = 0; i < buttons.Count; i++)
                {
                    btnProp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                }

                so.FindProperty("currentLabel").objectReferenceValue = label;
                so.ApplyModifiedPropertiesWithoutUndo();

                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll);

                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                Debug.Log("[DpiSetting] Content built. buttons=" + buttons.Count +
                          " height=" + scroll.rect.height.ToString("0"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
        }

        // 배율 프리셋 버튼 1개. 색/라벨색의 선택 상태는 런타임(DpiSettingWindow.Refresh)이 칠한다.
        static Button PresetButton(Transform parent, float scale)
        {
            var go = NewChild("Preset_" + Mathf.RoundToInt(scale * 100f), parent);

            var img = go.AddComponent<MPImage>();
            var so = new SerializedObject(img);
            so.FindProperty("m_Color").colorValue = Panel;
            so.FindProperty("m_DrawShape").enumValueIndex = 3; // Rectangle
            so.FindProperty("m_FalloffDistance").floatValue = 0.5f;
            so.FindProperty("m_Rectangle.m_UniformCornerRadius").boolValue = false;
            so.FindProperty("m_Rectangle.m_CornerRadius").vector4Value = new Vector4(8, 8, 8, 8);
            so.ApplyModifiedPropertiesWithoutUndo();
            img.raycastTarget = true;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 60;
            le.preferredHeight = 38;

            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var text = AddText(go.transform, "Label", Mathf.RoundToInt(scale * 100f) + "%",
                               FSemiBold, 14, Ink, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);

            return button;
        }

        // ============================ Helpers ============================

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

        static void Fitter(GameObject go)
        {
            var f = go.GetComponent<ContentSizeFitter>() ?? go.AddComponent<ContentSizeFitter>();
            f.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static TextMeshProUGUI AddText(Transform parent, string name, string text, TMP_FontAsset font,
            float size, Color color, TextAlignmentOptions align)
        {
            var go = NewChild(name, parent);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
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

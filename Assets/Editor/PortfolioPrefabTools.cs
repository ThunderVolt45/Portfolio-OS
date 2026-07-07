using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PortfolioOS.EditorTools
{
    /// <summary>
    /// 포트폴리오 앱 창 프리팹을 base UGUIWindow.prefab의 Prefab Variant로 재생성한다.
    /// - root의 UGUIWindow 컴포넌트를 서브클래스(AboutWindow/DocumentViewerWindow)로 스왑
    ///   (변형에서 m_Script 교체 불가 → 제거+추가+직렬화 값 복사로 우회)
    /// - Content/Viewport/ScrollContent의 데모 텍스트를 정리하고 단일 ContentText 배선
    /// - 아이콘/문서 경로 등 서브클래스 특화 필드 설정
    /// 메뉴: Portfolio → Rebuild Window Prefab Variants
    /// </summary>
    public static class PortfolioPrefabTools
    {
        const string BasePath = "Assets/Resources/Windows/UGUIWindow.prefab";

        [MenuItem("Portfolio/Rebuild Window Prefab Variants")]
        public static void RebuildVariants()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath);
            if (basePrefab == null)
            {
                Debug.LogError("[PortfolioPrefabTools] base prefab not found: " + BasePath);
                return;
            }

            BuildVariant(basePrefab, typeof(UGUIWindow.AboutWindow),
                "Assets/Resources/Windows/AboutWindow.prefab",
                "Assets/Portfolio/Icons/About.png",
                wireContentText: true, docPath: null);

            BuildVariant(basePrefab, typeof(UGUIWindow.DocumentViewerWindow),
                "Assets/Resources/Windows/DocumentViewerWindow.prefab",
                "Assets/Portfolio/Icons/Resume.png",
                wireContentText: false, docPath: "docs/resume.pdf");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PortfolioPrefabTools] Done. Variants rebuilt.");
        }

        [MenuItem("Portfolio/Build PDF Doc Windows + Icons")]
        public static void BuildPdfDocWindows()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath);
            if (basePrefab == null)
            {
                Debug.LogError("[PortfolioPrefabTools] base prefab not found: " + BasePath);
                return;
            }

            // PDF별 전용 창 = base UGUIWindow.prefab의 Variant + 서브클래스 컴포넌트.
            // 스프라이트(windowIcon)는 미지정(iconPath=null) → 사용자가 추후 직접 할당.
            BuildVariant(basePrefab, typeof(UGUIWindow.BrawlStarsTPSDocWindow),
                "Assets/Resources/Windows/BrawlStarsTPSDocWindow.prefab", null, false, null);
            BuildVariant(basePrefab, typeof(UGUIWindow.NovaRevolutionDocWindow),
                "Assets/Resources/Windows/NovaRevolutionDocWindow.prefab", null, false, null);
            BuildVariant(basePrefab, typeof(UGUIWindow.ProjectBlackoutDocWindow),
                "Assets/Resources/Windows/ProjectBlackoutDocWindow.prefab", null, false, null);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AddPdfDocIcons();
            Debug.Log("[PortfolioPrefabTools] Done. PDF doc windows + icons built.");
        }

        // 열려 있는 씬의 IconGrid에 PDF 문서 아이콘 3개를 추가한다(Icon_About 복제 기반).
        static void AddPdfDocIcons()
        {
            var template = GameObject.Find("UGUI_Desktop/IconGrid/Icon_About");
            if (template == null)
            {
                Debug.LogError("[PortfolioPrefabTools] template 'UGUI_Desktop/IconGrid/Icon_About' not found in open scene.");
                return;
            }
            var grid = template.transform.parent;

            // Icon_About(50,-60), Icon_Resume(50,-170) → 세로 열 간격 110
            AddIcon(template, grid, "BrawlStarsTPSDocWindow", "BrawlStars\nTPS", new Vector2(50f, -280f));
            AddIcon(template, grid, "NovaRevolutionDocWindow", "Nova\nRevolution", new Vector2(50f, -390f));
            AddIcon(template, grid, "ProjectBlackoutDocWindow", "Project\nBlackout", new Vector2(50f, -500f));

            var scene = template.scene;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[PortfolioPrefabTools] PDF doc icons added & scene saved.");
        }

        static void AddIcon(GameObject template, Transform grid, string className, string label, Vector2 anchoredPos)
        {
            string goName = "Icon_" + className.Replace("DocWindow", string.Empty);
            var existing = grid.Find(goName);
            if (existing != null)
            {
                Debug.Log("[PortfolioPrefabTools] icon already exists, skip: " + goName);
                return;
            }

            var go = (GameObject)UnityEngine.Object.Instantiate(template, grid);
            go.name = goName;

            var icon = go.GetComponent<UGUIWindow.UGUIIcon>();
            if (icon != null) icon.targetClassName = className;

            var tmp = go.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) tmp.text = label;

            var rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = anchoredPos;

            Debug.Log("[PortfolioPrefabTools] icon added: " + goName + " -> " + className);
        }

        static void BuildVariant(GameObject basePrefab, Type windowType, string outPath,
                                 string iconPath, bool wireContentText, string docPath)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            try
            {
                // 1) root 컴포넌트 스왑: UGUIWindow(base) -> 서브클래스
                var oldWin = inst.GetComponent<UGUIWindow.UGUIWindow>();
                var newWin = (UGUIWindow.UGUIWindow)inst.AddComponent(windowType);
                CopySerialized(oldWin, newWin);
                UnityEngine.Object.DestroyImmediate(oldWin);

                // 2) 아이콘 설정 (iconPath 비면 스프라이트 미지정 → 사용자가 추후 직접 할당)
                if (!string.IsNullOrEmpty(iconPath))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                    if (sprite != null) SetObjectField(newWin, "windowIcon", sprite);
                    else Debug.LogWarning("[PortfolioPrefabTools] icon not found: " + iconPath);
                }
                else
                {
                    SetObjectField(newWin, "windowIcon", null);
                }

                // 3) 데모 텍스트 정리 + 단일 ContentText 생성
                var scroll = inst.transform.Find("Content/Viewport/ScrollContent");
                TMP_Text contentText = null;
                if (scroll != null)
                {
                    var toDelete = new List<GameObject>();
                    foreach (Transform c in scroll) toDelete.Add(c.gameObject);
                    foreach (var g in toDelete) UnityEngine.Object.DestroyImmediate(g);

                    var go = new GameObject("ContentText", typeof(RectTransform));
                    go.transform.SetParent(scroll, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    var tmp = go.AddComponent<TextMeshProUGUI>();
                    tmp.alignment = TextAlignmentOptions.TopLeft;
                    tmp.fontSize = 16;
                    tmp.margin = new Vector4(12, 10, 12, 10);
                    tmp.text = string.Empty;
                    contentText = tmp;
                }
                else
                {
                    Debug.LogWarning("[PortfolioPrefabTools] ScrollContent not found under Content/Viewport.");
                }

                // 4) AboutWindow.contentText 배선
                if (wireContentText && contentText != null)
                    SetObjectField(newWin, "contentText", contentText);

                // 5) DocumentViewerWindow.documentRelativePath 설정
                if (!string.IsNullOrEmpty(docPath))
                    SetStringField(newWin, "documentRelativePath", docPath);

                // 6) 루트 이름 정리 + Variant로 저장
                inst.name = windowType.Name;
                var saved = PrefabUtility.SaveAsPrefabAsset(inst, outPath);
                if (saved != null)
                    Debug.Log("[PortfolioPrefabTools] Saved variant: " + outPath +
                              "  (isVariant=" + (PrefabUtility.GetPrefabAssetType(saved) == PrefabAssetType.Variant) + ")");
                else
                    Debug.LogError("[PortfolioPrefabTools] SaveAsPrefabAsset failed: " + outPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        static void CopySerialized(Component src, Component dst)
        {
            var so = new SerializedObject(src);
            var dso = new SerializedObject(dst);
            var it = so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.propertyPath == "m_Script") continue;
                dso.CopyFromSerializedProperty(it);
            }
            dso.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetObjectField(Component c, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(c);
            var p = so.FindProperty(field);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning("[PortfolioPrefabTools] field not found: " + field + " on " + c.GetType().Name);
        }

        static void SetStringField(Component c, string field, string value)
        {
            var so = new SerializedObject(c);
            var p = so.FindProperty(field);
            if (p != null) { p.stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning("[PortfolioPrefabTools] field not found: " + field + " on " + c.GetType().Name);
        }
    }
}

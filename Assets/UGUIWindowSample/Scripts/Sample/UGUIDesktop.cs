using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class UGUIDesktop : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private List<UGUIIcon> icons = new();

        [Space(5f)]
        public UnityEvent<UGUIIcon> OnIconClicked;

        private CanvasScaler canvasScaler;

        #region Initialize
        private void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();
        }

        private void Start()
        {
            FindIconInTransformRecursion(transform);
            OnIconClicked.AddListener(DivertOtherIcon);

            // 데스크톱은 비어 있는 상태로 시작한다. 창은 사용자가 아이콘을 클릭할 때 열린다.
            // (기존 샘플의 초기 창 자동 생성 로직 제거 — 포트폴리오용 정리)
        }

        private void FindIconInTransformRecursion(Transform transform)
        {
            if (transform.TryGetComponent(out UGUIIcon icon))
            {
                icons.Add(icon);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                FindIconInTransformRecursion(child);
            }
        }
        #endregion

        #region Event Listener
        private void DivertOtherIcon(UGUIIcon clickedIcon)
        {
            foreach (var icon in icons)
            {
                if (icon != clickedIcon)
                {
                    icon.Divert();
                }
            }
        }

        public void OnDPIChange(int screenWidth, int screenHeight, float dpi)
        {
            canvasScaler.referenceResolution =
                new Vector2(screenWidth / dpi, screenHeight / dpi);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            foreach (var icon in icons)
            {
                icon.Divert();
            }
        }
        #endregion
    }
}

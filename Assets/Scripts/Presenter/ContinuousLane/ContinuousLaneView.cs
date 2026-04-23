using NoteEditor.Utility;
using UnityEngine;

namespace NoteEditor.Presenter
{
    public class ContinuousLaneView : SingletonMonoBehaviour<ContinuousLaneView>
    {
        [SerializeField]
        RectTransform laneRectTransform = default;

        public static RectTransform LaneRectTransform
        {
            get
            {
                var rectTransform = Instance.laneRectTransform;
                return rectTransform != null ? rectTransform : Instance.GetComponent<RectTransform>();
            }
        }
    }
}

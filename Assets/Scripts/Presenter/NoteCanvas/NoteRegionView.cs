using NoteEditor.Utility;
using UnityEngine;

namespace NoteEditor.Presenter
{
    public class NoteRegionView : SingletonMonoBehaviour<NoteRegionView>
    {
        [SerializeField]
        RectTransform noteRegionRectTransform = default;

        public static RectTransform NoteRegionRectTransform
        {
            get
            {
                var rectTransform = Instance.noteRegionRectTransform;
                return rectTransform != null ? rectTransform : Instance.GetComponent<RectTransform>();
            }
        }
    }
}

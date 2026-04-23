using NoteEditor.ContinuousNotes;
using NoteEditor.Model;
using NoteEditor.Presenter;
using NoteEditor.Utility;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteEditor.GLDrawing
{
    public class ContinuousGuideRenderer : MonoBehaviour
    {
        [SerializeField]
        Color guideLineColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
        [SerializeField]
        Color boundaryLineColor = new Color(0.75f, 0.75f, 0.75f, 0.9f);
        [SerializeField]
        Color previewLineColor = new Color(0.69f, 1f, 0.31f, 0.9f);

        readonly float[] guideValues = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        void Awake()
        {
            this.LateUpdateAsObservable()
                .Where(_ => Audio.Source.clip != null)
                .Subscribe(_ =>
                {
                    var corners = new Vector3[4];
                    ContinuousLaneView.LaneRectTransform.GetWorldCorners(corners);

                    foreach (var value in guideValues)
                    {
                        var y = ContinuousConvertUtils.ValueToScreenY(value);
                        GLLineDrawer.Draw(new Line(
                            new Vector3(corners[0].x, y, 0),
                            new Vector3(corners[3].x, y, 0),
                            value == 0f || value == 1f ? boundaryLineColor : guideLineColor));
                    }

                    if (ContinuousEditState.IsMouseOverLane.Value)
                    {
                        var previewTime = ContinuousEditState.ClosestTime.Value;
                        if (!previewTime.Equals(ContinuousNoteTime.None))
                        {
                            var previewPos = ContinuousConvertUtils.TimeToScreenPosition(previewTime, ContinuousEditState.ClosestValue.Value);
                            GLLineDrawer.Draw(new Line(
                                new Vector3(previewPos.x, corners[0].y, 0),
                                new Vector3(previewPos.x, corners[1].y, 0),
                                previewLineColor));
                        }
                    }
                });
        }
    }
}

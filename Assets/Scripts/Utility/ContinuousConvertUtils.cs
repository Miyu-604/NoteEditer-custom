using NoteEditor.ContinuousNotes;
using NoteEditor.Model;
using NoteEditor.Presenter;
using UnityEngine;

namespace NoteEditor.Utility
{
    public class ContinuousConvertUtils
    {
        public static bool ContainsScreenPoint(Vector2 screenPoint)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(ContinuousLaneView.LaneRectTransform, screenPoint);
        }

        public static float RoundValue(float value)
        {
            return Mathf.Clamp(Mathf.Round(value * 100f) / 100f, 0f, 1f);
        }

        public static float ScreenYToValue(float screenY)
        {
            var corners = new Vector3[4];
            ContinuousLaneView.LaneRectTransform.GetWorldCorners(corners);

            return RoundValue(Mathf.InverseLerp(corners[0].y, corners[1].y, screenY));
        }

        public static float ValueToScreenY(float value)
        {
            var corners = new Vector3[4];
            ContinuousLaneView.LaneRectTransform.GetWorldCorners(corners);

            return Mathf.Lerp(corners[0].y, corners[1].y, Mathf.Clamp01(value));
        }

        public static ContinuousNoteTime ScreenXToTime(float screenX)
        {
            if (Audio.Source.clip == null)
            {
                return ContinuousNoteTime.None;
            }

            var canvasX = ConvertUtils.ScreenToCanvasPosition(new Vector3(screenX, 0, 0)).x;
            var samples = ConvertUtils.CanvasPositionXToSamples(canvasX);
            var unitBeatSamples = Audio.Source.clip.frequency * 60f / EditData.BPM.Value / EditData.LPB.Value;
            var beat = Mathf.RoundToInt(samples / unitBeatSamples);

            return new ContinuousNoteTime(EditData.LPB.Value, Mathf.Max(beat, 0));
        }

        public static Vector3 TimeToScreenPosition(ContinuousNoteTime time, float value)
        {
            var samples = time.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value);
            var x = ConvertUtils.CanvasToScreenPosition(new Vector3(ConvertUtils.SamplesToCanvasPositionX(samples), 0, 0)).x;
            return new Vector3(x, ValueToScreenY(value), 0);
        }
    }
}

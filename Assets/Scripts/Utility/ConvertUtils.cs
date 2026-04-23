using NoteEditor.Model;
using NoteEditor.DTO;
using NoteEditor.Notes;
using NoteEditor.Presenter;
using UnityEngine;

namespace NoteEditor.Utility
{
    public class ConvertUtils : SingletonMonoBehaviour<ConvertUtils>
    {
        public static int CanvasPositionXToSamples(float x)
        {
            var per = (x - SamplesToCanvasPositionX(0)) / NoteCanvas.Width.Value;
            return Mathf.RoundToInt(Audio.Source.clip.samples * per);
        }

        public static float SamplesToCanvasPositionX(int samples)
        {
            if (Audio.Source.clip == null)
                return 0;

            return (samples - Audio.SmoothedTimeSamples.Value + EditData.OffsetSamples.Value)
                * NoteCanvas.Width.Value / Audio.Source.clip.samples
                + NoteCanvas.OffsetX.Value;
        }

        public static float BlockNumToCanvasPositionY(int blockNum)
        {
            var maxIndex = EditData.MaxBlock.Value - 1;
            if (maxIndex <= 0)
            {
                return 0f;
            }

            var corners = new Vector3[4];
            NoteRegionView.NoteRegionRectTransform.GetWorldCorners(corners);

            var topY = corners[1].y;
            var bottomY = corners[0].y;
            var screenY = Mathf.Lerp(bottomY, topY, (maxIndex - blockNum) / (float)maxIndex);

            return ScreenToCanvasPosition(new Vector3(0, screenY, 0)).y / NoteCanvas.ScaleFactor.Value;
        }

        public static Vector3 NoteToCanvasPosition(NotePosition notePosition)
        {
            return new Vector3(
                SamplesToCanvasPositionX(notePosition.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value)),
                BlockNumToCanvasPositionY(notePosition.block) * NoteCanvas.ScaleFactor.Value,
                0);
        }

        public static Vector3 ScreenToCanvasPosition(Vector3 screenPosition)
        {
            return (screenPosition - new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0)) * NoteCanvas.ScaleFactor.Value;
        }

        public static Vector3 CanvasToScreenPosition(Vector3 canvasPosition)
        {
            return (canvasPosition / NoteCanvas.ScaleFactor.Value + new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
        }
    }
}

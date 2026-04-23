using NoteEditor.Model;
using NoteEditor.Notes;
using NoteEditor.Utility;
using UnityEngine;

namespace NoteEditor.GLDrawing
{
    public class ContinuousNoteRenderer : MonoBehaviour
    {
        static readonly Color longNoteColor = new Color(0 / 255f, 255 / 255f, 255 / 255f);
        static readonly Color invalidStateColor = new Color(255 / 255f, 0 / 255f, 0 / 255f);

        void LateUpdate()
        {
            if (Audio.Source.clip == null)
            {
                return;
            }

            foreach (var noteObj in EditData.ContinuousNotes.Values)
            {
                var screenPos = ContinuousConvertUtils.TimeToScreenPosition(noteObj.note.time, noteObj.note.value);

                if (screenPos.x < 0 || screenPos.x > Screen.width)
                {
                    continue;
                }

                if (noteObj.note.type == NoteTypes.Long && EditData.ContinuousNotes.ContainsKey(noteObj.note.next))
                {
                    var nextPos = ContinuousConvertUtils.TimeToScreenPosition(
                        EditData.ContinuousNotes[noteObj.note.next].note.time,
                        EditData.ContinuousNotes[noteObj.note.next].note.value);
                    GLLineDrawer.Draw(new Line(
                        screenPos,
                        nextPos,
                        0f < nextPos.x - screenPos.x ? longNoteColor : invalidStateColor));
                }

                if (noteObj.note.type == NoteTypes.Long
                    && EditState.NoteType.Value == NoteTypes.Long
                    && ContinuousEditState.LongNoteTailTime.Value.Equals(noteObj.note.time))
                {
                    var previewEnd = (Vector3)Input.mousePosition;
                    GLLineDrawer.Draw(new Line(
                        screenPos,
                        previewEnd,
                        0f < previewEnd.x - screenPos.x ? longNoteColor : invalidStateColor));
                }

                var drawSize = 9 / NoteCanvas.ScaleFactor.Value;
                GLQuadDrawer.Draw(new Geometry(
                    new[]
                    {
                        new Vector3(screenPos.x, screenPos.y - drawSize, 0),
                        new Vector3(screenPos.x + drawSize, screenPos.y, 0),
                        new Vector3(screenPos.x, screenPos.y + drawSize, 0),
                        new Vector3(screenPos.x - drawSize, screenPos.y, 0)
                    },
                    noteObj.NoteColor));
            }
        }
    }
}

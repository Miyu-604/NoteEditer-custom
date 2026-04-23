using NoteEditor.Model;
using NoteEditor.Utility;
using UnityEngine;

namespace NoteEditor.GLDrawing
{
    public class ContinuousNoteRenderer : MonoBehaviour
    {
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

using UnityEngine;

namespace NoteEditor.ContinuousNotes
{
    public class ContinuousNoteObject
    {
        static readonly Color noteColor = new Color(175 / 255f, 255 / 255f, 78 / 255f);

        public ContinuousNote note = new ContinuousNote();
        public Color NoteColor { get { return noteColor; } }
    }
}

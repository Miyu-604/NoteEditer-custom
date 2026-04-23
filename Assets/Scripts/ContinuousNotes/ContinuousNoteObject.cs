using NoteEditor.Notes;
using UnityEngine;

namespace NoteEditor.ContinuousNotes
{
    public class ContinuousNoteObject
    {
        static readonly Color singleNoteColor = new Color(175 / 255f, 255 / 255f, 78 / 255f);
        static readonly Color longNoteColor = new Color(0 / 255f, 255 / 255f, 255 / 255f);

        public ContinuousNote note = new ContinuousNote();
        public Color NoteColor { get { return note.type == NoteTypes.Long ? longNoteColor : singleNoteColor; } }
    }
}

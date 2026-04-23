using NoteEditor.Notes;

namespace NoteEditor.ContinuousNotes
{
    public class ContinuousNote
    {
        public ContinuousNoteTime time = ContinuousNoteTime.None;
        public NoteTypes type = NoteTypes.Single;
        public ContinuousNoteTime next = ContinuousNoteTime.None;
        public ContinuousNoteTime prev = ContinuousNoteTime.None;
        public float value;

        public ContinuousNote(ContinuousNoteTime time, float value, NoteTypes type, ContinuousNoteTime next, ContinuousNoteTime prev)
        {
            this.time = time;
            this.type = type;
            this.next = next;
            this.prev = prev;
            this.value = value;
        }

        public ContinuousNote(ContinuousNoteTime time, float value, NoteTypes type)
        {
            this.time = time;
            this.type = type;
            this.value = value;
        }

        public ContinuousNote(ContinuousNoteTime time, float value)
        {
            this.time = time;
            this.value = value;
        }

        public ContinuousNote(ContinuousNote note)
        {
            time = note.time;
            type = note.type;
            next = note.next;
            prev = note.prev;
            value = note.value;
        }

        public ContinuousNote() { }
    }
}

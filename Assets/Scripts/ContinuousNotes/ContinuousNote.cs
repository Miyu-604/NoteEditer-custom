namespace NoteEditor.ContinuousNotes
{
    public class ContinuousNote
    {
        public ContinuousNoteTime time = ContinuousNoteTime.None;
        public float value;

        public ContinuousNote(ContinuousNoteTime time, float value)
        {
            this.time = time;
            this.value = value;
        }

        public ContinuousNote(ContinuousNote note)
        {
            time = note.time;
            value = note.value;
        }

        public ContinuousNote() { }
    }
}

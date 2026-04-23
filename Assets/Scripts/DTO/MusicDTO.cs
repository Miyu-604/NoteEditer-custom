using System.Collections.Generic;

namespace NoteEditor.DTO
{
    public class MusicDTO
    {
        [System.Serializable]
        public class EditData
        {
            public string name;
            public int maxBlock;
            public int BPM;
            public int offset;
            public List<Note> notes;
            public List<ContinuousNote> continuousNotes;
        }

        [System.Serializable]
        public class Note
        {
            public int LPB;
            public int num;
            public int block;
            public int type;
            public List<Note> notes;
        }

        [System.Serializable]
        public class ContinuousNote
        {
            public int LPB;
            public int num;
            public int type;
            public float value;
            public List<ContinuousNote> notes;
        }
    }
}

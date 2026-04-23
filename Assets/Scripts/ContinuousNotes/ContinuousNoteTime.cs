using UnityEngine;

namespace NoteEditor.ContinuousNotes
{
    public struct ContinuousNoteTime
    {
        public int LPB;
        public int num;

        public ContinuousNoteTime(int LPB, int num)
        {
            this.LPB = LPB;
            this.num = num;
        }

        public int ToSamples(int frequency, int BPM)
        {
            return Mathf.FloorToInt(num * (frequency * 60f / BPM / LPB));
        }

        public override string ToString()
        {
            return LPB + "-" + num;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ContinuousNoteTime))
            {
                return false;
            }

            var target = (ContinuousNoteTime)obj;
            return Mathf.Approximately((float)num / LPB, (float)target.num / target.LPB);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public static ContinuousNoteTime None
        {
            get { return new ContinuousNoteTime(-1, -1); }
        }
    }
}

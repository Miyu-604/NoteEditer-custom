using NoteEditor.ContinuousNotes;
using NoteEditor.Utility;
using UniRx;

namespace NoteEditor.Model
{
    public class ContinuousEditState : SingletonMonoBehaviour<ContinuousEditState>
    {
        ReactiveProperty<bool> isMouseOverLane_ = new ReactiveProperty<bool>(false);
        ReactiveProperty<ContinuousNoteTime> closestTime_ = new ReactiveProperty<ContinuousNoteTime>(ContinuousNoteTime.None);
        ReactiveProperty<ContinuousNoteTime> longNoteTailTime_ = new ReactiveProperty<ContinuousNoteTime>(ContinuousNoteTime.None);
        ReactiveProperty<float> closestValue_ = new ReactiveProperty<float>(0f);

        public static ReactiveProperty<bool> IsMouseOverLane { get { return Instance.isMouseOverLane_; } }
        public static ReactiveProperty<ContinuousNoteTime> ClosestTime { get { return Instance.closestTime_; } }
        public static ReactiveProperty<ContinuousNoteTime> LongNoteTailTime { get { return Instance.longNoteTailTime_; } }
        public static ReactiveProperty<float> ClosestValue { get { return Instance.closestValue_; } }
    }
}

using NoteEditor.Common;
using NoteEditor.ContinuousNotes;
using NoteEditor.Model;
using NoteEditor.Notes;
using NoteEditor.Utility;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace NoteEditor.Presenter
{
    public class EditContinuousNotesPresenter : SingletonMonoBehaviour<EditContinuousNotesPresenter>
    {
        [SerializeField]
        float dragStartDistance = 4f;

        public readonly Subject<ContinuousNote> RequestForAddNote = new Subject<ContinuousNote>();
        public readonly Subject<ContinuousNote> RequestForRemoveNote = new Subject<ContinuousNote>();
        public readonly Subject<ContinuousNote> RequestForChangeNote = new Subject<ContinuousNote>();

        bool isPointerDownOnNote;
        bool isDragging;
        Vector3 pointerDownScreenPosition;
        ContinuousNote initialNote = new ContinuousNote();
        float draggingValue;

        void Awake()
        {
            Audio.OnLoad.Subscribe(_ => ResetInteractionState());

            EditState.NoteType
                .Where(type => type == NoteTypes.Single)
                .Subscribe(_ => ContinuousEditState.LongNoteTailTime.Value = ContinuousNoteTime.None);

            this.UpdateAsObservable()
                .Where(_ => Audio.Source.clip != null)
                .Subscribe(_ =>
                {
                    var mousePosition = (Vector2)Input.mousePosition;
                    var isMouseOverLane = ContinuousConvertUtils.ContainsScreenPoint(mousePosition);

                    ContinuousEditState.IsMouseOverLane.Value = isMouseOverLane;
                    ContinuousEditState.ClosestTime.Value = isMouseOverLane
                        ? ContinuousConvertUtils.ScreenXToTime(mousePosition.x)
                        : ContinuousNoteTime.None;
                    ContinuousEditState.ClosestValue.Value = isMouseOverLane
                        ? ContinuousConvertUtils.ScreenYToValue(mousePosition.y)
                        : 0f;

                    if (Settings.IsOpen.Value || KeyInput.CtrlKey())
                    {
                        if (Input.GetMouseButtonUp(0))
                        {
                            FinishInteraction();
                        }
                        return;
                    }

                    if (isMouseOverLane && Input.GetMouseButtonDown(0))
                    {
                        BeginInteraction(mousePosition);
                    }

                    if (isPointerDownOnNote && Input.GetMouseButton(0))
                    {
                        UpdateDrag(mousePosition);
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        FinishInteraction();
                    }
                });
        }

        void BeginInteraction(Vector2 mousePosition)
        {
            var hasClosestNote = TryGetClosestNote(mousePosition, out var noteObject);

            if (EditState.NoteType.Value == NoteTypes.Single && KeyInput.ShiftKey())
            {
                EditState.NoteType.Value = NoteTypes.Long;
                BeginLongEditing(mousePosition, hasClosestNote ? noteObject : null);
                return;
            }

            if (EditState.NoteType.Value == NoteTypes.Long)
            {
                BeginLongModeInteraction(mousePosition, hasClosestNote ? noteObject : null);
                return;
            }

            if (hasClosestNote)
            {
                isPointerDownOnNote = true;
                isDragging = false;
                pointerDownScreenPosition = mousePosition;
                initialNote = new ContinuousNote(noteObject.note);
                draggingValue = noteObject.note.value;
                return;
            }

            var time = ContinuousConvertUtils.ScreenXToTime(mousePosition.x);
            if (time.Equals(ContinuousNoteTime.None) || EditData.ContinuousNotes.ContainsKey(time))
            {
                return;
            }

            var note = new ContinuousNote(time, ContinuousConvertUtils.ScreenYToValue(mousePosition.y), NoteTypes.Single);
            EditCommandManager.Do(new Command(
                () =>
                {
                    AddNote(note);
                    RequestForAddNote.OnNext(new ContinuousNote(note));
                },
                () => RemoveNote(note)));
        }

        void BeginLongEditing(Vector2 mousePosition, ContinuousNoteObject noteObject)
        {
            if (noteObject != null)
            {
                if (noteObject.note.type == NoteTypes.Long)
                {
                    if (noteObject.note.next.Equals(ContinuousNoteTime.None))
                    {
                        ContinuousEditState.LongNoteTailTime.Value = noteObject.note.time;
                    }
                    return;
                }

                var current = new ContinuousNote(
                    noteObject.note.time,
                    noteObject.note.value,
                    NoteTypes.Long,
                    ContinuousNoteTime.None,
                    ContinuousEditState.LongNoteTailTime.Value);
                var previous = new ContinuousNote(noteObject.note);

                EditCommandManager.Do(new Command(
                    () =>
                    {
                        ApplyNoteState(current);
                        RequestForChangeNote.OnNext(new ContinuousNote(current));
                    },
                    () =>
                    {
                        ApplyNoteState(previous);
                        RequestForChangeNote.OnNext(new ContinuousNote(previous));
                    }));
                return;
            }

            AddLongNoteAt(mousePosition);
        }

        void BeginLongModeInteraction(Vector2 mousePosition, ContinuousNoteObject noteObject)
        {
            if (noteObject != null)
            {
                if (noteObject.note.type != NoteTypes.Long)
                {
                    return;
                }

                var tailTime = ContinuousEditState.LongNoteTailTime.Value;
                if (!tailTime.Equals(ContinuousNoteTime.None)
                    && noteObject.note.prev.Equals(ContinuousNoteTime.None)
                    && !tailTime.Equals(noteObject.note.time)
                    && EditData.ContinuousNotes.ContainsKey(tailTime))
                {
                    var tailPrevious = new ContinuousNote(EditData.ContinuousNotes[tailTime].note);
                    var selfPrevious = new ContinuousNote(noteObject.note);
                    var tailCurrent = new ContinuousNote(tailPrevious);
                    var selfCurrent = new ContinuousNote(selfPrevious);
                    tailCurrent.next = selfCurrent.time;
                    selfCurrent.prev = tailCurrent.time;

                    EditCommandManager.Do(new Command(
                        () =>
                        {
                            ApplyNoteState(tailCurrent);
                            ApplyNoteState(selfCurrent);
                            RequestForChangeNote.OnNext(new ContinuousNote(tailCurrent));
                            RequestForChangeNote.OnNext(new ContinuousNote(selfCurrent));
                        },
                        () =>
                        {
                            ApplyNoteState(tailPrevious);
                            ApplyNoteState(selfPrevious);
                            RequestForChangeNote.OnNext(new ContinuousNote(tailPrevious));
                            RequestForChangeNote.OnNext(new ContinuousNote(selfPrevious));
                        }));
                    return;
                }

                var removed = new ContinuousNote(noteObject.note);
                if (EditData.ContinuousNotes.ContainsKey(removed.prev) && !EditData.ContinuousNotes.ContainsKey(removed.next))
                {
                    ContinuousEditState.LongNoteTailTime.Value = removed.prev;
                }

                EditCommandManager.Do(new Command(
                    () =>
                    {
                        RemoveNote(removed);
                        RequestForRemoveNote.OnNext(new ContinuousNote(removed));
                    },
                    () =>
                    {
                        AddNote(removed);
                        RequestForAddNote.OnNext(new ContinuousNote(removed));
                    }));
                return;
            }

            AddLongNoteAt(mousePosition);
        }

        void AddLongNoteAt(Vector2 mousePosition)
        {
            var time = ContinuousConvertUtils.ScreenXToTime(mousePosition.x);
            if (time.Equals(ContinuousNoteTime.None) || EditData.ContinuousNotes.ContainsKey(time))
            {
                return;
            }

            var note = new ContinuousNote(
                time,
                ContinuousConvertUtils.ScreenYToValue(mousePosition.y),
                NoteTypes.Long,
                ContinuousNoteTime.None,
                ContinuousEditState.LongNoteTailTime.Value);

            EditCommandManager.Do(new Command(
                () =>
                {
                    AddNote(note);
                    RequestForAddNote.OnNext(new ContinuousNote(note));
                },
                () => RemoveNote(note)));
        }

        void UpdateDrag(Vector2 mousePosition)
        {
            if (!isDragging && dragStartDistance <= Vector2.Distance(pointerDownScreenPosition, mousePosition))
            {
                isDragging = true;
            }

            if (!isDragging || !EditData.ContinuousNotes.ContainsKey(initialNote.time))
            {
                return;
            }

            draggingValue = ContinuousConvertUtils.ScreenYToValue(mousePosition.y);
            ChangeNote(new ContinuousNote(
                initialNote.time,
                draggingValue,
                initialNote.type,
                initialNote.next,
                initialNote.prev));
        }

        void FinishInteraction()
        {
            if (!isPointerDownOnNote)
            {
                ResetInteractionState();
                return;
            }

            if (!EditData.ContinuousNotes.ContainsKey(initialNote.time))
            {
                ResetInteractionState();
                return;
            }

            if (!isDragging)
            {
                EditCommandManager.Do(new Command(
                    () =>
                    {
                        RemoveNote(initialNote);
                        RequestForRemoveNote.OnNext(new ContinuousNote(initialNote));
                    },
                    () => AddNote(initialNote)));
            }
            else
            {
                var current = new ContinuousNote(
                    initialNote.time,
                    draggingValue,
                    initialNote.type,
                    initialNote.next,
                    initialNote.prev);
                if (!Mathf.Approximately(initialNote.value, current.value))
                {
                    EditCommandManager.Do(new Command(
                        () =>
                        {
                            ChangeNote(current);
                            RequestForChangeNote.OnNext(new ContinuousNote(current));
                        },
                        () => ChangeNote(initialNote),
                        () =>
                        {
                            ChangeNote(current);
                            RequestForChangeNote.OnNext(new ContinuousNote(current));
                        }));
                }
                else
                {
                    ChangeNote(initialNote);
                }
            }

            ResetInteractionState();
        }

        void ResetInteractionState()
        {
            isPointerDownOnNote = false;
            isDragging = false;
            pointerDownScreenPosition = Vector3.zero;
            initialNote = new ContinuousNote();
            draggingValue = 0f;
        }

        bool TryGetClosestNote(Vector2 mousePosition, out ContinuousNoteObject noteObject)
        {
            const float hitDistance = 14f;

            noteObject = EditData.ContinuousNotes.Values
                .Select(note => new
                {
                    note,
                    distance = Vector2.Distance(
                        (Vector2)ContinuousConvertUtils.TimeToScreenPosition(note.note.time, note.note.value),
                        mousePosition)
                })
                .Where(x => x.distance <= hitDistance)
                .OrderBy(x => x.distance)
                .Select(x => x.note)
                .FirstOrDefault();

            return noteObject != null;
        }

        public void AddNote(ContinuousNote note)
        {
            if (note.time.Equals(ContinuousNoteTime.None))
            {
                return;
            }

            ApplyNoteState(note);
        }

        void ApplyNoteState(ContinuousNote note)
        {
            note.value = ContinuousConvertUtils.RoundValue(note.value);

            if (EditData.ContinuousNotes.ContainsKey(note.time))
            {
                var current = EditData.ContinuousNotes[note.time].note;
                if (current.type == NoteTypes.Long)
                {
                    RemoveLink(current);
                }

                EditData.ContinuousNotes[note.time].note = new ContinuousNote(note);
            }
            else
            {
                EditData.ContinuousNotes.Add(note.time, new ContinuousNoteObject { note = new ContinuousNote(note) });
            }

            if (note.type == NoteTypes.Long)
            {
                InsertLink(note);
                ContinuousEditState.LongNoteTailTime.Value = ContinuousEditState.LongNoteTailTime.Value.Equals(note.prev)
                    ? note.time
                    : ContinuousNoteTime.None;
            }
            else if (ContinuousEditState.LongNoteTailTime.Value.Equals(note.time))
            {
                ContinuousEditState.LongNoteTailTime.Value = ContinuousNoteTime.None;
            }
        }

        void ChangeNote(ContinuousNote note)
        {
            if (!EditData.ContinuousNotes.ContainsKey(note.time))
            {
                return;
            }

            ApplyNoteState(note);
        }

        void RemoveNote(ContinuousNote note)
        {
            if (!EditData.ContinuousNotes.ContainsKey(note.time))
            {
                return;
            }

            var current = EditData.ContinuousNotes[note.time].note;
            if (current.type == NoteTypes.Long)
            {
                RemoveLink(current);
            }

            EditData.ContinuousNotes.Remove(note.time);

            if (ContinuousEditState.LongNoteTailTime.Value.Equals(note.time))
            {
                ContinuousEditState.LongNoteTailTime.Value = ContinuousNoteTime.None;
            }
        }

        void RemoveLink(ContinuousNote note)
        {
            if (EditData.ContinuousNotes.ContainsKey(note.prev))
            {
                EditData.ContinuousNotes[note.prev].note.next = note.next;
            }

            if (EditData.ContinuousNotes.ContainsKey(note.next))
            {
                EditData.ContinuousNotes[note.next].note.prev = note.prev;
            }
        }

        void InsertLink(ContinuousNote note)
        {
            if (EditData.ContinuousNotes.ContainsKey(note.prev))
            {
                EditData.ContinuousNotes[note.prev].note.next = note.time;
            }

            if (EditData.ContinuousNotes.ContainsKey(note.next))
            {
                EditData.ContinuousNotes[note.next].note.prev = note.time;
            }
        }
    }
}

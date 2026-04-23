using NoteEditor.Common;
using NoteEditor.ContinuousNotes;
using NoteEditor.Model;
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
            if (TryGetClosestNote(mousePosition, out var noteObject))
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

            EditCommandManager.Do(new Command(
                () =>
                {
                    var note = new ContinuousNote(time, ContinuousConvertUtils.ScreenYToValue(mousePosition.y));
                    AddNote(note);
                    RequestForAddNote.OnNext(new ContinuousNote(note));
                },
                () => RemoveNote(new ContinuousNote(time, 0f))));
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
            ChangeNote(new ContinuousNote(initialNote.time, draggingValue));
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
                var current = new ContinuousNote(initialNote.time, draggingValue);
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

            note.value = ContinuousConvertUtils.RoundValue(note.value);

            if (EditData.ContinuousNotes.ContainsKey(note.time))
            {
                ChangeNote(note);
                return;
            }

            EditData.ContinuousNotes.Add(note.time, new ContinuousNoteObject { note = new ContinuousNote(note) });
        }

        void ChangeNote(ContinuousNote note)
        {
            if (!EditData.ContinuousNotes.ContainsKey(note.time))
            {
                return;
            }

            EditData.ContinuousNotes[note.time].note.value = ContinuousConvertUtils.RoundValue(note.value);
        }

        void RemoveNote(ContinuousNote note)
        {
            if (!EditData.ContinuousNotes.ContainsKey(note.time))
            {
                return;
            }

            EditData.ContinuousNotes.Remove(note.time);
        }
    }
}

using NoteEditor.ContinuousNotes;
using NoteEditor.Model;
using NoteEditor.Notes;
using System.Collections;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

namespace NoteEditor.Presenter
{
    public class MusicLoader : MonoBehaviour
    {
        void Awake()
        {
            ResetEditor();
        }

        public void Load(string fileName)
        {
            StartCoroutine(LoadMusic(fileName));
        }

        IEnumerator LoadMusic(string fileName)
        {
            var filePath = Path.Combine(MusicSelector.DirectoryPath.Value, fileName);
            using (var request = UnityWebRequestMultimedia.GetAudioClip(new System.Uri(filePath).AbsoluteUri, AudioType.UNKNOWN))
            {
                yield return request.SendWebRequest();

                EditCommandManager.Clear();
                ResetEditor();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Audio.Source.clip = DownloadHandlerAudioClip.GetContent(request);
                }

                if (Audio.Source.clip == null)
                {
                    // TODO: 読み込み失敗時の処理
                }
                else
                {
                    EditData.Name.Value = fileName;
                    LoadEditData();
                    Audio.OnLoad.OnNext(Unit.Default);
                }
            }
        }

        void LoadEditData()
        {
            var fileName = Path.ChangeExtension(EditData.Name.Value, "json");
            var directoryPath = Path.Combine(Path.GetDirectoryName(MusicSelector.DirectoryPath.Value), "Notes");
            var filePath = Path.Combine(directoryPath, fileName);

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                EditDataSerializer.Deserialize(json);
            }
        }

        public void ResetEditor()
        {
            Audio.TimeSamples.Value = 0;
            Audio.SmoothedTimeSamples.Value = 0;
            Audio.IsPlaying.Value = false;
            Audio.Source.clip = null;
            EditState.NoteType.Value = NoteTypes.Single;
            EditState.LongNoteTailPosition.Value = NotePosition.None;
            EditData.BPM.Value = 120;
            EditData.OffsetSamples.Value = 0;
            EditData.Name.Value = "Note Editor";
            EditData.MaxBlock.Value = Settings.MaxBlock;
            EditData.LPB.Value = 4;

            foreach (var note in EditData.Notes.Values)
            {
                note.Dispose();
            }

            EditData.Notes.Clear();
            EditData.ContinuousNotes.Clear();
            ContinuousEditState.IsMouseOverLane.Value = false;
            ContinuousEditState.ClosestTime.Value = ContinuousNoteTime.None;
            ContinuousEditState.LongNoteTailTime.Value = ContinuousNoteTime.None;
            ContinuousEditState.ClosestValue.Value = 0f;
            Resources.UnloadUnusedAssets();
        }
    }
}

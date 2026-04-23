# NoteEditor Unity実装ガイド

## 1. 目的

この文書は、現行プロジェクトが Unity 上でどのように構成・実装されているかを整理したものである。  
仕様書が「何をするか」を説明するのに対し、この文書は「Unity でどう組まれているか」を説明する。

## 2. 全体アーキテクチャ

## 2.1 レイヤ分割

主要コードは以下の責務で分割されている。

- `Assets/Scripts/Model`
  共有状態と永続化対象の中心
- `Assets/Scripts/Presenter`
  入力、UI、状態更新、コマンド化
- `Assets/Scripts/Notes`
  ノートのドメイン表現
- `Assets/Scripts/ContinuousNotes`
  continuous ノートのドメイン表現
- `Assets/Scripts/GLDrawing`
  グリッド、ノート、波形補助描画
- `Assets/Scripts/Common`
  Undo/Redo の基盤
- `Assets/Scripts/Utility`
  シングルトン、座標変換、入力補助、UI 拡張
- `Assets/Scripts/SoundEffect`
  クラップ音再生
- `Assets/Scripts/DTO`
  JSON シリアライズ用 DTO

## 2.2 状態管理パターン

状態は `SingletonMonoBehaviour<T>` によるシーン共有インスタンスとして実装されている。  
各モデルは主に `ReactiveProperty<T>` と `Subject<T>` を公開し、Presenter が購読・更新する。

代表的なモデル:

- `Audio`
- `EditData`
- `EditState`
- `ContinuousEditState`
- `EditorState`
- `NoteCanvas`
- `MusicSelector`
- `Settings`

この設計により、Unity の参照配線が薄い箇所でもグローバル状態へ直接アクセスできる。

## 3. シーン構成

`Assets/Scenes/NoteEditor.unity` に主要 UI とマネージャが集約されている。

確認できる主要 GameObject:

- `Main Camera`
- `MusicSelector`
- `SettingsWindow`
- `SaveButton`
- `CanvasOffsetXHandler`
- `EditSections`
- `EditSection`
- `NoteCanvas`
- `PlaybackPosition`
- `GLDrawer`
- `TogglePlayPauseButton`
- `WaveformImage`
- `CanvasWidthScaler`
- `EventSystem`
- `ClickAudioSource`

## 3.1 NoteCanvas 周辺

`NoteCanvas` は譜面画面の親 RectTransform で、少なくとも以下の子を持つ。

- `WaveformImage`
- `ContinuousLaneRoot`
- ノート/グリッド関連の描画要素
- 補助線群
- 編集セクション表示

ここに対して、各 Presenter が UI イベントと描画状態を重ねている。

## 3.2 GLDrawer

`GLDrawer` はシーンルートにあり、以下の 2 コンポーネントを持つ。

- `GLLineDrawer`
- `GLQuadDrawer`

どちらも `OnRenderObject` で GL 即時描画を行う。  
描画要求は静的 `Draw(...)` メソッドに蓄積し、フレーム末尾に一括で出力する。

## 4. 起動から楽曲読込までの実装フロー

## 4.1 Settings 初期化

`SettingsWindowPresenter.Awake()` で以下を行う。

1. `Settings/settings.json` をロード
2. 無ければデフォルト設定を生成
3. `SettingsSerializer.Deserialize` で `Settings` に反映
4. `EditData.MaxBlock` の変化に応じて入力キー設定 UI を再生成

## 4.2 MusicSelector 初期化

`MusicSelectorPresenter.Start()` で以下を行う。

- `Settings.WorkSpacePath` から `Musics` ディレクトリを入力欄へ反映
- `MusicSelector.DirectoryPath` と入力欄を双方向同期
- 300ms ごとにディレクトリ内容を走査して `MusicSelector.FilePathList` を更新
- 一覧更新時に子要素を破棄して再生成
- 選択ファイルに対して `MusicLoader.Load` を呼ぶ

## 4.3 MusicLoader の責務

`MusicLoader` は楽曲と譜面のロード入口である。

`Load(fileName)`:

1. 音声を `UnityWebRequestMultimedia.GetAudioClip` で取得
2. `EditCommandManager.Clear`
3. `ResetEditor`
4. `Audio.Source.clip` 設定
5. `LoadEditData`
6. `Audio.OnLoad.OnNext(Unit.Default)`

`Audio.OnLoad` は複数コンポーネントの遅延初期化トリガとして機能する。  
これにより、音声長や `AudioSource.clip` を必要とする処理は、ロード後に初期化される。

## 5. Model の詳細

## 5.1 Audio

`Audio` は実質的な再生サブシステムの状態コンテナである。

- `AudioSource Source`
- `Subject<Unit> OnLoad`
- `ReactiveProperty<float> Volume`
- `ReactiveProperty<bool> IsPlaying`
- `ReactiveProperty<int> TimeSamples`
- `ReactiveProperty<float> SmoothedTimeSamples`

`SmoothedTimeSamples` は表示用の補間値で、波形やグリッドの滑らかな追従に使う。

## 5.2 EditData

譜面の編集対象そのものを表す。  
`Dictionary<NotePosition, NoteObject>` と `Dictionary<ContinuousNoteTime, ContinuousNoteObject>` を直接持っているため、Model が表示オブジェクトまで抱えているのが特徴である。

これは一般的な純粋データモデルではなく、Unity 実行時オブジェクトを含むランタイム中心設計である。

## 5.3 EditState / ContinuousEditState / EditorState / Settings / MusicSelector / NoteCanvas

それぞれの責務:

- `EditState`: 編集モードやロングノート編集中の一時状態
- `ContinuousEditState`: continuous レーンでの最近傍時間、最近傍値、long tail の一時状態
- `EditorState`: 波形表示、クラップ音などのエディタ全体表示状態
- `Settings`: ワークスペースやキー配置などのユーザー設定
- `MusicSelector`: 楽曲選択パネル状態
- `NoteCanvas`: 描画領域のスケール、オフセット、マウス位置解釈

## 6. Presenter の詳細

## 6.1 EditNotesPresenter

ノート編集の中枢である。

公開 Subject:

- `RequestForEditNote`
- `RequestForRemoveNote`
- `RequestForAddNote`
- `RequestForChangeNoteStatus`

入力解釈:

- クリック位置は `NoteCanvas.ClosestNotePosition` を参照
- 既存ノートなら `NoteObject.OnClickObservable`
- 空位置なら `RequestForEditNote`

コマンド化:

- add/remove/change を 1 フレーム単位でバッファ
- `EditCommandManager.Do(new Command(...))` で Undo/Redo 対象にする

実体更新:

- `AddNote`
- `ChangeNoteStates`
- `RemoveNote`

## 6.1b EditContinuousNotesPresenter

continuous ノート編集の中枢である。

公開 Subject:

- `RequestForAddNote`
- `RequestForRemoveNote`
- `RequestForChangeNote`

入力解釈:

- `ContinuousLaneView` の Rect 内だけを対象にする
- `ContinuousConvertUtils.ScreenXToTime` で時間位置を求める
- `ContinuousConvertUtils.ScreenYToValue` で縦方向値を求める
- single モードでは追加、削除、ドラッグ値変更
- long モードでは節点追加、tail 選択、接続、削除

実体更新:

- `ApplyNoteState`
- `RemoveLink`
- `InsertLink`

continuous long も block long と同じく `prev` / `next` を直接更新して管理する。

## 6.2 RangeSelectionPresenter

選択系機能を担当する。

- Ctrl + ドラッグ矩形選択
- Ctrl + A
- Ctrl + C / X / V
- Delete / Backspace

実装上の重要点:

- 選択状態は `selectedNoteObjects` で保持
- コピー時、ロングノートの `prev` / `next` は「選択範囲内の節点だけ」に再マッピング
- 貼り付け後、遅延 1 フレームで新規ノートを再選択

## 6.3 InputNotesByKeyboardPresenter

設定キーによる譜面入力を担当する。  
キー押下時に `Audio.Source.timeSamples` を `BPM` と `LPB` で拍へ変換し、`RequestForEditNote` を発火する。

## 6.4 PlaybackPositionPresenter

再生位置に関する全入力を統合する。

入力ソース:

- 左右矢印
- 波形ドラッグ
- マウスホイール
- 再生位置スライダー

設計上のポイント:

- 入力結果は最終的に `Audio.Source.timeSamples` へ反映
- `Audio.Source.timeSamples` を監視して `Audio.TimeSamples` へ転写
- `Audio.TimeSamples` から UI スライダーと表示文字列へ反映
- 操作のまとまりを Command 化して Undo/Redo 可能にしている

## 6.5 CanvasWidthScalePresenter / CanvasOffsetXPresenter

この 2 つが譜面ビューの見え方を支える。

`CanvasWidthScalePresenter`:

- Ctrl + ホイール
- ↑ / ↓ キー
- スライダー

`CanvasOffsetXPresenter`:

- 波形上の垂直ラインをドラッグして横移動

どちらも値変更を `Command` 化しているため、表示操作も Undo/Redo できる。

## 6.6 SavePresenter

保存と終了制御を担当する。

- 未保存状態の追跡
- 保存ボタン色変更
- メッセージ表示
- 終了時確認ダイアログ
- `EditDataSerializer.Serialize()` を使ったファイル保存

現在は block ノート系イベントに加えて、continuous ノートの add/remove/change も未保存判定に含めている。

## 6.7 Toolstrip 系 Presenter

小さな Presenter を多数配置して、UI 部品ごとに責務を分離している。

- `BPMSpinBoxPresenter`
- `BeatOffsetSpinBoxPresenter`
- `LPBSpinBoxPresenter`
- `MusicNameTextPresenter`
- `ToggleClapSoundEffectEnablePresenter`
- `ToggleDisplaySettingsPresenter`
- `ToggleEditTypePresenter`
- `TogglePlayPausePresenter`
- `ToggleWaveformDisplayPresenter`
- `VolumePresenter`

この粒度により、各 UI コンポーネントは小さな監視と代入に集中している。

## 6.8 SettingsWindowPresenter

設定ウィンドウを組み立てる。

- JSON 読込
- キー設定項目の動的生成
- 設定変更時の自動保存

`InputNoteKeyCodeSettingsItem` は各レーンの設定セルで、選択中セルに対して `Input.anyKeyDown` を待ち受けてキーコードを書き換える。

## 7. 描画実装

## 7.1 GL 描画の採用理由

このプロジェクトはノートや補助線を GameObject で大量生成せず、GL 即時描画で描いている。  
そのため、譜面の密度が高くてもヒエラルキー数を抑えられる。

## 7.2 GridLineRenderer

役割:

- 拍線描画
- レーン線描画
- 最近傍グリッドのハイライト
- `NoteCanvas.ClosestNotePosition` の計算
- 拍番号表示の起点管理

処理概要:

1. 楽曲長、BPM、LPB から拍線配列を作る
2. `ConvertUtils.SamplesToCanvasPositionX` で画面位置へ変換
3. マウス最近傍の拍線とレーン線を求める
4. 閾値内ならハイライトし、ノート入力位置として採用
5. 可視範囲の線だけ描画

## 7.3 NoteRenderer / ContinuousNoteRenderer

`NoteRenderer` は毎フレーム `EditData.Notes.Values` を走査し、可視範囲内の block ノートだけを描画する。  
`ContinuousNoteRenderer` は `EditData.ContinuousNotes.Values` を走査し、continuous ノート本体と long 接続線を描画する。

- ノート本体は菱形クアッド
- ロングノート接続線は `NoteObject` 側の `LateUpdateObservable` で描く
- continuous long の接続線と編集中プレビュー線は `ContinuousNoteRenderer` 側で描く

## 7.4 WaveformRenderer

`RawImage` に 1 ピクセル高の `Texture2D` を載せ、シェーダで縦方向に波形へ展開している。

流れ:

1. `AudioClip.GetData` でサンプル取得
2. 横方向へ最大振幅を間引き集約
3. `Texture2D.SetPixel` で 1 行分を書き換え
4. `Waveform.shader` が上下対称の波形として描く

利点:

- 実装が単純
- `RawImage` と Material だけで波形表示できる

注意:

- 配列長が固定
- 毎更新で `texture.Apply()` を呼ぶ
- 高頻度更新時の負荷は軽くない

## 7.5 BeatNumberRenderer

拍番号は GameObject プールで管理する。  
GL ではなく Text を使っているため、数字描画だけは UI として重ねている。

## 8. 座標変換の中心

`ConvertUtils` が時間軸と画面座標をつなぐ。

主要関数:

- `CanvasPositionXToSamples`
- `SamplesToCanvasPositionX`
- `BlockNumToCanvasPositionY`
- `NoteToCanvasPosition`
- `ScreenToCanvasPosition`
- `CanvasToScreenPosition`

continuous 側は `ContinuousConvertUtils` が担当する。

- `ContainsScreenPoint`
- `RoundValue`
- `ScreenYToValue`
- `ValueToScreenY`
- `ScreenXToTime`
- `TimeToScreenPosition`

重要な依存状態:

- `Audio.Source.clip.samples`
- `Audio.SmoothedTimeSamples`
- `EditData.OffsetSamples`
- `NoteCanvas.Width`
- `NoteCanvas.OffsetX`
- `NoteCanvas.ScaleFactor`
- `NoteRegionView.NoteRegionRectTransform`
- `ContinuousLaneView.LaneRectTransform`

ここが崩れると描画も入力判定も一斉に壊れるため、このクラスは実質的に表示系の基準座標系である。

## 9. Undo / Redo 実装

## 9.1 基盤

`Common/Command` と `Common/CommandManager` は単純な do/undo/redo のスタック実装である。

## 9.2 利用箇所

主要利用箇所:

- `EditNotesPresenter`
- `EditContinuousNotesPresenter`
- `PlaybackPositionPresenter`
- `SpinBoxPresenterBase`
- `CanvasWidthScalePresenter`
- `CanvasOffsetXPresenter`
- `EditSectionHandlePresenter`
- `MusicSelectorPresenter` 経由の `ChangeLocationCommandManager`

## 9.3 設計上の特徴

- UI の直接代入ではなく、できるだけ「変更のまとまり」をコマンド化している
- UniRx の `Buffer`、`ThrottleFrame`、`Throttle(TimeSpan)` を使って、ドラッグや連打を 1 操作に圧縮している

## 10. ロングノート実装

## 10.1 表現

ロングノートは節点列である。  
各節点が `prev` と `next` を持ち、連結リストとして表現する。

## 10.2 接続更新

`NoteObject.SetState` 内で以下を行う。

- シングル化時は既存リンクを外す
- ロング化時は前後ノードのリンクを現在位置へ差し替える
- 現在のロング編集テール位置を更新する

## 10.3 描画

`NoteObject.Init()` が `LateUpdateObservable` を購読し、現在ノードから次ノードまでの線を `GLLineDrawer.Draw` する。

色分け:

- 正方向接続: シアン
- 選択を含む接続: マゼンタ
- 左向きなど不正方向: 赤

continuous long ノートは `ContinuousNoteRenderer` が接続線と tail プレビュー線を直接描画する。

## 11. 今後改修しやすいポイント

## 11.1 良い点

- 状態の所在が比較的明確
- Presenter が小さく、責務分離が進んでいる
- DTO が単純で保存形式が追いやすい
- GL 描画によりオブジェクト数を抑えている

## 11.2 改修時の注意点

- Model が `NoteObject` まで持つため、純粋データ化はされていない
- Singleton 自動生成が配線漏れを隠す可能性がある
- 入力と描画が `ConvertUtils` に強く結合している
- `ReactiveProperty` の副作用が広く、変更影響範囲を見落としやすい
- Scene 直結参照が多く、Prefab 分離されていない箇所は差し替えコストが高い

## 12. 新規実装や移植のための再構成方針

このプロジェクトを別実装で再現するなら、最小単位は以下になる。

1. 共有状態層
2. 音声ロード/再生層
3. block / continuous のノート辞書とロングノート接続ロジック
4. 入力解釈 Presenter
5. 座標変換ユーティリティ
6. グリッド/ノート/波形描画
7. 保存/設定永続化
8. Undo/Redo

特に重要なのは次の 3 点である。

- `NotePosition` / `ContinuousNoteTime` と BPM/LPB/サンプルの変換仕様
- block / continuous のロングノート `prev/next` 接続仕様
- `Audio.OnLoad` を起点にした初期化順序

この 3 点を維持すれば、UI 実装方式を変更してもほぼ同じ挙動を再現できる。

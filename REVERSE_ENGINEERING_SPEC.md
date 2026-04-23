# NoteEditor Reverse Engineering 仕様書

## 1. 文書の目的

この文書は、`Assets/Scenes/NoteEditor.unity` を中心とした現行プロジェクトをリバースエンジニアリングし、アプリケーションとしての仕様を整理したものである。  
コード上の実装根拠は主に `Assets/Scripts/Model`、`Assets/Scripts/Presenter`、`Assets/Scripts/Notes`、`Assets/Scripts/GLDrawing` にある。

## 2. プロダクト概要

- 本プロジェクトは音楽ゲーム用の譜面エディタである。
- 音声ファイルを読み込み、時間軸上にノーツを配置して譜面データを JSON 形式で保存する。
- ノート系統は「グリッド型 block ノート」と「0.00〜1.00 の連続値を持つ continuous ノート」の 2 系統である。
- 画面は大きく以下の 4 領域で構成される。
- 楽曲選択領域
- ツールストリップ領域
- 譜面キャンバス領域
- 設定ウィンドウおよび保存確認ダイアログ

README では `wav` 対応と記載されているが、現実装では `UnityWebRequestMultimedia.GetAudioClip(..., AudioType.UNKNOWN)` を使っているため、読み込み可否は Unity のデコーダに依存する。  
一方、ファイル一覧 UI のアイコン判定は拡張子 `.wav` を前提にしているため、実運用上は `wav` を主対象とみなすのが妥当である。

## 3. 実行環境と前提

- README 記載の開発環境: `Unity 2019.1.5f1`
- 現在の `ProjectSettings/ProjectVersion.txt`: `6000.3.11f1`
- UI は uGUI ベース
- 状態管理は `UniRx`
- 入力はキーボード、マウス、UI イベントの併用

## 4. 機能一覧

### 4.1 楽曲選択

- ワークスペース配下の `Musics` ディレクトリを参照する。
- ディレクトリ一覧とファイル一覧を表示する。
- 項目クリックでファイル名を選択する。
- ディレクトリ項目を選択済み状態でもう一度クリックすると、そのディレクトリへ移動する。
- 読み込みボタンで楽曲をロードする。
- 楽曲読込時に既存編集状態はリセットされる。
- 同名の譜面 JSON が `Notes` ディレクトリにあれば自動読込する。

### 4.2 再生制御

- Space キーまたは再生ボタンで再生/停止を切り替える。
- 再生位置はスライダー、左右矢印、波形ドラッグ、マウスホイールで操作できる。
- 再生中に再生位置を動かすと、一時停止して操作後に再開する。
- 再生終端に達すると再生フラグを false にする。

### 4.3 block ノート編集

- block ノートはシングルノートとロングノートの 2 種類を扱う。
- 編集モードはボタンまたは Alt キーで切り替える。
- 譜面上の最近傍グリッド位置に対してクリック入力を行う。
- シングルモード:
- 空位置クリックでノート追加
- 既存シングルノートクリックで削除
- シングルモード中に Shift を押しながらクリックするとロングノート編集開始
- ロングモード:
- 空位置クリックでロングノート節点を追加
- 既存ロングノートクリックで節点削除または連結更新
- Esc または右クリックでロングモード終了

### 4.4 continuous ノート編集

- `NoteCanvas` 下部の continuous レーンで編集する。
- 時間軸は block ノートと共有し、縦方向だけが `0.00`〜`1.00` の連続値になる。
- 値は `0.01` 単位に丸めて扱う。
- 同一 `LPB, num` に配置できる continuous ノートは 1 件だけである。
- single モード:
- 空位置クリックでノート追加
- 既存ノートクリックで削除
- 既存ノートドラッグで値を上下に微調整
- Shift + クリックで long 編集へ入る
- long モード:
- 空位置クリックで long 節点追加
- 既存 long ノートクリックで tail 指定、接続、または削除
- Esc または右クリックで long モード終了

### 4.5 キーボード入力による打鍵登録

- 設定されたキー配列に対応して各レーンへノートを打ち込める。
- 再生中は `-5000` サンプルの補正を入れて、体感遅延を吸収する設計になっている。
- 入力時刻は `BPM` と `LPB` に基づき最寄り分解能へ丸める。

### 4.6 範囲選択と編集

- Ctrl + ドラッグで矩形範囲選択
- Ctrl + A ですべて選択
- Delete / Backspace で選択ノート削除
- Ctrl + C でコピー
- Ctrl + X で切り取り
- Ctrl + V で貼り付け

貼り付けは「コピー範囲の末尾拍の次の拍」へオフセットして行う。  
ロングノートは、コピー対象内に含まれる節点どうしだけが `prev` / `next` で再接続される。

continuous ノートは現状この範囲選択・コピー貼り付けの対象外である。

### 4.7 表示操作

- Ctrl + マウスホイール、上下矢印、または専用スライダーで横方向ズーム
- 波形領域の垂直線ハンドルのドラッグで表示オフセット移動
- 波形表示の ON/OFF 切替
- グリッド線、拍番号、ノート、ロングノート接続線を描画

### 4.8 音声補助

- 再生中、ノート位置に合わせてクラップ音を再生できる。
- クラップ音はトグルで有効/無効を切り替える。
- ボリュームスライダーで楽曲音量を変更できる。
- クラップ対象には block ノートに加えて continuous ノートも含まれる。

### 4.9 保存

- Ctrl + S または保存ボタンで JSON 保存
- 未保存状態を画面表示とボタン色で通知
- アプリ終了時、未保存なら保存確認ダイアログを表示

## 5. ドメインモデル仕様

### 5.1 EditData

`EditData` は譜面の中核状態を持つ。

- `Name`: 読込中の楽曲ファイル名
- `MaxBlock`: レーン数
- `LPB`: Lines Per Beat。グリッド分解能
- `BPM`: テンポ
- `OffsetSamples`: 譜面オフセット
- `Notes`: `Dictionary<NotePosition, NoteObject>`
- `ContinuousNotes`: `Dictionary<ContinuousNoteTime, ContinuousNoteObject>`

### 5.2 NotePosition

ノート位置は以下の 3 要素で表す。

- `LPB`
- `num`
- `block`

意味:

- `num` は `LPB` 単位の拍インデックス
- `block` はレーン番号

サンプル変換:

- `num * (frequency * 60 / BPM / LPB)` を切り捨て

等価判定:

- `num / LPB` が同じ拍位置
- `block` が同じ

このため、`4-2-1` と `8-4-1` は同位置として扱われる。

### 5.3 Note

- `position`: 自身位置
- `type`: `Single` または `Long`
- `next`: 次節点
- `prev`: 前節点

ロングノートは単独オブジェクトの配列ではなく、各節点の双方向リンクで表現される。

### 5.4 NoteObject

描画・選択・クリック反応を伴うランタイムオブジェクトである。

- `note`
- `isSelected`
- `LateUpdateObservable`
- `OnClickObservable`
- `NoteColor`

色の状態:

- 通常シングル: 黄緑
- 通常ロング: シアン
- 選択中: マゼンタ
- 不正ロング接続: 赤

### 5.5 ContinuousNoteTime

- `LPB`
- `num`

意味:

- block ノートと同じ時間軸分解能を表す
- `block` を持たず、縦方向情報は `value` に分離される

### 5.6 ContinuousNote

- `time`
- `type`: `Single` または `Long`
- `next`
- `prev`
- `value`: `0.00`〜`1.00`

continuous long ノートも block long ノートと同様に、節点間の `prev` / `next` で表現される。

### 5.7 ContinuousNoteObject

- `note`
- `NoteColor`

色の状態:

- 通常 single: 黄緑
- 通常 long: シアン

## 6. データ保存仕様

### 6.1 譜面保存先

- 参照楽曲: `WorkSpacePath/Musics/<music file>`
- 譜面 JSON: `WorkSpacePath/Notes/<music name>.json`

### 6.2 譜面 JSON 構造

DTO は `Assets/Scripts/DTO/MusicDTO.cs` に定義されている。

トップレベル:

- `name`
- `maxBlock`
- `BPM`
- `offset`
- `notes`
- `continuousNotes`

ノート要素:

- `LPB`
- `num`
- `block`
- `type`
- `notes`

`type` の値:

- `1`: シングルノート
- `2`: ロングノート

ロングノートは「先頭節点 1 件 + 後続節点配列」で保存される。  
デシリアライズ時は順に `AddNote` した後で `prev` / `next` を再構築する。

continuous ノート要素:

- `LPB`
- `num`
- `type`
- `value`
- `notes`

`type` の値:

- `1`: continuous single
- `2`: continuous long

continuous long も block long と同じく「先頭節点 1 件 + 後続節点配列」で保存される。

### 6.3 設定ファイル

保存場所:

- `Settings/settings.json`

内容:

- `workSpacePath`
- `maxBlock`
- `noteInputKeyCodes`

初期値は `SettingsDTO.GetDefaultSettings()` にハードコードされている。

## 7. ユーザー操作仕様

### 7.1 ショートカット

実装上確認できる主なキー操作:

- `Ctrl + Z`: Undo
- `Ctrl + Y`: Redo
- `Ctrl + S`: Save
- `Ctrl + A`: 全選択
- `Ctrl + C`: コピー
- `Ctrl + X`: 切り取り
- `Ctrl + V`: 貼り付け
- `Alt`: 編集モード切替
- `Shift + クリック`: シングルモードからロング編集開始
- `Esc`: ロング編集解除、設定ウィンドウ閉、または終了処理
- `Space`: 再生/停止
- `Delete / Backspace`: 選択削除
- `↑ / ↓`: 横ズーム
- `← / →`: 再生位置移動
- `Ctrl + ← / →`: より大きく再生位置移動

### 7.2 Undo / Redo 対象

コマンドマネージャにより、以下の変更が Undo/Redo 対象になる。

- ノート追加
- ノート削除
- ノート状態変更
- 再生位置変更
- 各種スピンボックス値変更
- キャンバス横幅変更
- キャンバス横オフセット変更
- 楽曲選択ディレクトリ変更

## 8. 状態遷移の要点

### 8.1 楽曲ロード時

1. `MusicLoader.Load`
2. `UnityWebRequest` で音声取得
3. `EditCommandManager.Clear`
4. `ResetEditor`
5. `Audio.Source.clip` へ設定
6. 同名 JSON が存在すれば `EditDataSerializer.Deserialize`
7. `Audio.OnLoad.OnNext`

### 8.2 ノート編集時

1. `CanvasEvents` がマウス入力を流す
2. `GridLineRenderer` が最近傍グリッドを計算し `NoteCanvas.ClosestNotePosition` を更新
3. `EditNotesPresenter` が入力解釈
4. `RequestForAddNote` / `RequestForRemoveNote` / `RequestForChangeNoteStatus`
5. バッファリング後に `EditCommandManager.Do`
6. `EditData.Notes` を更新
7. `NoteRenderer` がフレームごとに描画

### 8.3 保存判定

`SavePresenter` は以下の変更を監視し、未保存フラグを立てる。

- BPM
- オフセット
- MaxBlock
- ノート編集関連イベント
- continuous ノート編集関連イベント

楽曲ロードまたは保存操作時には未保存フラグを下ろす。

## 9. 画面仕様

### 9.1 楽曲選択パネル

- 入力欄でディレクトリ表示
- ファイル一覧を縦に並べる
- Undo/Redo ボタンでフォルダ移動履歴を戻す/進める
- Load ボタンで選択ファイルをロード

### 9.2 ツールストリップ

- 再生ボタン
- 楽曲名表示
- 再生位置スライダー
- BPM スピンボックス
- オフセットスピンボックス
- LPB スピンボックス
- 音量スライダー
- 編集モード切替ボタン
- 波形表示トグル
- クラップ音トグル
- 設定表示ボタン
- 保存ボタン

### 9.3 譜面キャンバス

- 中央に波形とグリッド
- 水平方向に時間
- 垂直方向にレーン
- 上段に block ノート編集領域
- 下段に continuous ノート編集領域
- 補助線、選択矩形、ロング接続線を GL 描画
- 拍番号は UI テキストを動的生成して重ねる

## 10. 実装から読み取れる制約・注意点

- `SettingsSerializer` と `SettingsDTO.GetDefaultSettings()` の初期ワークスペースパスが `/Users/miyu/Documents/Charts` に固定されている。
- `BlockNumToCanvasPositionY` は `maxIndex = EditData.MaxBlock.Value - 1` を使うが、現行実装では `maxIndex <= 0` の場合に `0f` を返す。
- `WaveformRenderer` のサンプル配列長は `500000` 固定であり、長尺音源やズーム条件によっては表示精度とコストに影響する。
- `SavePresenter` の終了確認はアプリ終了時のみで、楽曲切替前確認はない。
- `NotePosition.Equals` は拍比率比較であり、LPB 違いでも等値になるため、辞書キーの意味を理解して扱う必要がある。
- `ContinuousNoteTime.Equals` も拍比率比較であり、LPB 違いでも同時刻として扱われる。
- `SingletonMonoBehaviour` は存在しない場合に新規 GameObject を生成するため、シーン結線が欠けていても null にはならないが、意図しないランタイム生成が起こりうる。

## 11. 現在のアーキテクチャ要約

- 状態: `Model` の Singleton + ReactiveProperty
- 入力解釈: `Presenter`
- 描画: `GLDrawing` と一部 uGUI
- 永続化: DTO + JsonUtility
- 編集履歴: 独自 `Command` / `CommandManager`

このプロジェクトは、MVC よりも「Reactive な共有状態 + Presenter 分散」の構成に近い。  
Unity シーン上の各 UI/描画コンポーネントが `Model` の共有状態を購読し、入力時にはイベントを `Presenter` へ戻す実装になっている。

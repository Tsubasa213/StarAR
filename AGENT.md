# StarAR 卒研用エージェント指示

## 目的

このプロジェクトでは、観測地点・時刻・端末の向きに対応した星空をAR表示する卒業研究用アプリを開発する。作業を始める前に、必ず [`卒研進捗まとめ.md`](./卒研進捗まとめ.md) を読み、チャット上の到達点と現在のワークスペース状態を混同しない。

## 技術前提

- Unity `6000.4.3f1`
- AR Foundation、ARCore／ARKit、XR Interaction Toolkit
- 対象Scene: `Assets/Scenes/MainARScene.unity`
- 星データ: `Assets/Resources/stars_6.csv` など
- 星Prefab: `Assets/Prefabs/CoreSphere.prefab`
- CSVの数値は英語圏形式の小数点を前提にし、C#では `CultureInfo.InvariantCulture` を使う。

## 重要な責務分離

次の構成を基本方針とする。既存コードを変更する場合も、どのTransformを誰が変更するかを崩さない。

- `SkyRoot`: コンパス・ジャイロによる端末姿勢と北合わせのみ。
- `CelestialSphere`: 観測地点の緯度による傾きと、恒星時による天球回転のみ。
- `StarManager` / `StarGenerator`: CSVの読み込み、星の生成、等級・B-Vによる見た目のみ。
- `GalacticRoot`: 天の川など銀河系表現のみ。
- `NorthMarker`: 方位合わせのデバッグ表示。

`SkyRoot` と `CelestialSphere` の両方へ同じ恒星時・コンパス回転を書き込まない。`SkyRotation.cs` の開発用時間回転は、恒星時制御と同時に有効化しない。

## 座標系の前提

星表の `ra` と `dec` は度単位の赤経・赤緯として扱う。現在の星生成コードの基本式は次の通り。

```text
x = R * cos(dec) * cos(ra)
y = R * sin(dec)
z = R * cos(dec) * sin(ra)
```

現在の半径は `R = 100`。座標軸や回転の符号を変更する場合は、固定日時・固定地点で基準星を使った検証結果を残す。

## 現在の既知の未完了箇所

- `MainARScene` の `StarGenerator.celestialSphere` が未設定になっている可能性がある。Inspectorで確認してから星生成を評価する。
- `LocationProvider.cs`、`CelestialSphereController.cs`、`SiderealTime.cs` は作成済みだが、Scene接続と実機検証が未完了。
- `SkyController` はコンパスを直接読み、`CompassSensor` も存在する。センサーの読み取り元を一つに統一する。
- `StarGenerator` はCSV nullチェックより前に `csvFile.text` を参照している。列数・ヘッダー・不正行の扱いも見直す。
- 天の川のBloomと星のHDR／Emissionの見え方が未検証。

## 作業手順

1. `卒研進捗まとめ.md`、`git status`、対象ファイルを確認する。
2. 既存の未コミット変更を保持し、無関係な変更を上書きしない。
3. まず星生成の基準状態を復元・確認する。
4. GPS、恒星時、コンパスの順で一つずつ接続する。
5. 各段階でUnity Console、Inspector、Play Mode、可能なら実機で確認する。
6. 座標系・回転順序・センサー値をログまたはテスト条件として記録する。
7. 重要な進捗や未解決問題が変わったら `卒研進捗まとめ.md` を更新する。

## 編集・検証ルール

- `Library`、`Temp`、生成された `.csproj` は直接編集しない。
- Sceneを編集するときはInspector参照を確認し、参照切れを残さない。
- GPSやコンパスが使えない環境でも、固定地点・固定時刻のデフォルト値で検証できるようにする。
- Unity EditorのPlay Modeでの動作確認を優先する。`dotnet build` が成功しても、Scene参照・権限・センサー・描画結果までは保証されない。
- 研究上の判断（座標系、評価方法、許容誤差、データ出典）は、コードに埋め込まず文書にも記録する。
- 破壊的なGit操作、未依頼のデータ削除、既存変更のリセットは行わない。

## 次に着手する作業

最優先は、`StarManager` の `celestialSphere` 参照を正しいTransformへ接続し、`stars_6.csv` の読み込み件数と生成件数を確認すること。その後、`LocationProvider` をSceneへ接続し、固定地点を使った緯度反映、恒星時回転、コンパスの北合わせを順番に検証する。
## 2026-07-14 現在の実装メモ

- 恒星時による天球回転は `Assets/Scripts/Sensor/SiderealTimeRotation.cs` が担当する。
- このコンポーネントは `Assets/Scenes/MainARScene.unity` の `SkyRoot` にアタッチされている。起動時の回転を基準にして恒星時の経過分を連続積算するため、現在の天の川配置を保ったまま時間変化だけを反映する。
- 現在のSceneでは `StarGenerator.celestialSphere` も `SkyRoot` を参照する。これは第1段階の暫定構成であり、後段で位置情報・端末姿勢を統合するときに、必要なら独立した `CelestialSphere` Transformへ分離する。
- `Assets/Scripts/Sensor/ARSkyController.cs` は `SkyRoot` の回転を直接変更しない。恒星時回転を上書きしないためであり、コンパスによる方位合わせは別の回転層として実装する。
- 現段階は起動時配置からの相対回転である。GPSの緯度・経度による絶対姿勢の決定と、端末の東西南北方位への最終的な合わせ込みは未完了。
- 実装後の基本検証として `dotnet build Assembly-CSharp.csproj --no-restore -t:Rebuild` を実行し、0エラーを確認した。ただしUnity EditorのPlay Modeと実機センサー検証は別途必要。
## デバッグ高速化・現在地取得のルール

- 回転速度を確認するときは `CelestialSphereController` の `siderealTimeScale` を見る。`1` が実時間、600はデバッグ用である。現在のScene設定は1。
- デバッグ倍率は恒星時の計算式を変えず、フレーム間の経過角度にだけ適用する。実機で正しい時間の動きを確認するときは `1` に戻す。
- 現在地取得は `Assets/Scripts/Sensor/LocationProvider.cs` が担当する。緯度・経度を提供し、天球の回転は担当しない。
- `MainARScene` には `LocationProvider` GameObjectを配置済み。Editorでは大阪（34.693738, 135.502165）を使い、Android実機ではGPSを使う。
- 位置情報を天球へ反映する処理を追加するときも、`LocationProvider` 内でTransformを直接回転させない。緯度補正、恒星時回転、端末方位の責務を分離する。
- 位置情報関連の変更後は、Editorのフォールバック動作とAndroid実機の権限・GPS動作を別々に確認する。
## 卒業研究の最終拡張方針

- 最終目的は、実カメラ画像中の星と天文計算で生成したAR星を対応付け、その差からジャイロ・コンパス・AR座標の誤差を補正すること。
- 座標系は、赤道座標系（RA/Dec）、地平座標系（方位角/高度）、ARワールド座標系、カメラ座標系、端末センサ姿勢系を分離して扱う。
- 画像認識は、星候補抽出、外れ値除去、星間角距離による対応付け、姿勢誤差推定、センサフュージョンの順に段階実装する。
- `LocationProvider` は位置情報提供、`CelestialSphereController` は天球姿勢、将来の画像認識クラスはカメラ画像からの観測値生成を担当する。各クラスが同じTransformを直接上書きしない。
- 画像認識による補正は、ジャイロを予測、星認識を観測として扱う。検出星数・対応誤差・環境条件から補正信頼度を決める。
- 卒研の評価では、GPS・恒星時のみ、センサ統合、画像補正ありを比較し、実星とAR星の角度誤差、ドリフト、検出率、処理時間を記録する。
## AR画面の明るさ調整

- `Assets/Scripts/Debug/ARBrightnessController.cs` はAndroid実機で `ARCameraBackground` のカスタムマテリアルを使い、カメラ背景だけを暗くする。
- 黒フィルターの初期値は不透明度80%、調整範囲は0〜100%である。最大値ではカメラ背景が完全な黒になる。
- 露出補正（`ColorAdjustments.postExposure`）とURPポストプロセスは使用しない。通常のカメラ映像をカスタムシェーダーで暗くする。
- 描画順は「カメラ映像 → カメラ用黒フィルター → AR星・天の川 → 操作UI」とする。星やUIは暗くしない。
## Android/Windows別の明るさフィルター

- `ARBrightnessController` はAndroid実機だけで黒フィルターと調整用スライダーを有効化する。スライダーはカメラ背景の濃度だけを変更する。
- Android UIは `InputSystemUIInputModule` にデフォルトアクションを割り当て、EventSystemが存在しない場合は実行時に生成する。
- 画面全体を覆う黒Imageは生成しないため、スライダーを操作してもAR星・天の川は暗くならない。
- WindowsおよびUnity EditorではARCameraBackgroundを無効化し、Main Cameraを黒背景にする。
- Androidのカメラ背景マテリアルは `Assets/Shaders/ARCoreBackgroundWithBlackFilter.shader` と `Assets/Materials/ARCoreBackgroundWithBlackFilter.mat` を使用する。最大値ではカメラ背景が完全な黒になる。

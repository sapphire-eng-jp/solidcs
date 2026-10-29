# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

SolidJS 風の Signal / Effect ベースのリアクティブプリミティブを C# で実装した実験プロジェクト。`SolidCS` 本体は UI フレームワーク非依存のリアクティブコア、`SolidCS.Avalonia` がそのコアの上に Avalonia 用 UI コンポーネントをラップして提供する小さな GUI フレームワーク、`examples/Counter` がそれらを利用するリファレンス実装、という三層構成。

## ソリューション構成

- `SolidCS/` — コアライブラリ (net10.0, nullable enable)。`Signal<T>`、`Context`、`Effect` の 3 ファイルで構成される。UI 非依存。
- `SolidCS.Avalonia/` — Avalonia 11.3.18 ラッパーライブラリ (`net10.0` / `net10.0-browser` の multi-target)。`Component` 抽象基底と `Label` / `Button` / `StackPanel` などのリアクティブ UI コンポーネント、およびアプリ起動用の `App` / `Window` を `SolidCS.Avalonia` namespace に公開する。型名は Avalonia 側 (`Avalonia.Controls.Label` 等) と素朴に被るので、ラッパー実装の内側では Avalonia 型を `global::Avalonia.Controls.X` で完全修飾している。`SolidCS` を ProjectReference、Avalonia 系パッケージ (`Avalonia` / `Avalonia.Themes.Fluent` / `Avalonia.Fonts.Inter`) を PackageReference — bootstrap を担う関係でテーマ / フォントもライブラリ側が持つ。bootstrap 用パッケージだけ TFM 条件付き: desktop TFM は `Avalonia.Desktop`、browser TFM は `Avalonia.Browser` (コンポーネント群は Avalonia コアのみに依存するので両 TFM 共通コンパイル)。
- `SolidCS.Tests/` — xUnit テスト (net10.0)。`SolidCS` を ProjectReference。
- `examples/Counter/` — サンプル (`PublishAot=true`, `WinExe`)。`Label` / `Button` / `StackPanel` を使う最小デモ + `Menu` / `MenuItem` / `Separator` のメニューバーデモ (リアクティブ header とクリックアクション)。**Avalonia を直接参照しない** — `SolidCS` と `SolidCS.Avalonia` の ProjectReference のみで、Avalonia パッケージは SolidCS.Avalonia から推移的に来る。
- `examples/Form/` — 同上のサンプル。入力系コントロール (`TextBox` / `CheckBox`) と表示系 (`TextBlock` / `Label`) の双方向バインドおよびリアクティブ表示の動作確認用。root を `ScrollViewer` で包み、ウィンドウより縦長のリアクティブコンテンツでスクロールの動作確認も兼ねる。フォーム欄は `Grid` の auto-flow モード、統計 + サマリ行は explicit モード (colSpan 付き) で組んであり、Grid 両モードのデモでもある。さらに root の `WithFont(Func 版)` + A-/A+ ボタンで全体のフォントサイズを実行時に変えるデモと、挨拶行の静的 `WithFont` (20px Bold — 内側が勝つので root の変更に追従しない) のデモを兼ねる。Phase 1 コントロール群 (`Slider` / `NumericUpDown` / `ProgressBar` / `RadioGroup`) のデモ行もある — Slider と ProgressBar は同じ `Signal<double>` を共有し、値はサマリ行にリアクティブ反映される。
- `examples/AppShell/` — 同上のサンプル。`DockPanel` でアプリの外枠 (ヘッダー Top / ステータスバー Bottom / サイドバー Left / メインコンテンツが残りを埋める) を組み、サイドバーのボタンで `Signal<string>` のページを切り替えるデモ。メインコンテンツには `WrapPanel` のタグボタンギャラリー (ウィンドウ幅で折り返し + クリックで selected 更新) も同居。
- `examples/Todo/` — 同上のサンプル。動的 children (`For.Each` / `Show`) のデモ。TODO の追加・削除でリストの行が増減し (`Signal<IReadOnlyList<T>>` のイミュータブル更新規約)、完了数の統計行を `Show` (fallback 付き) で出し分ける。行ラベルの `#n` は render 通し番号 — keyed diff (For v2) で既存行が再利用され増減後も番号が変わらないことの実証用。設計の経緯は `docs/dynamic-children-design.md`。
- `examples/Gallery/` — 同上のサンプル。対応する全コンポーネントのショーケース (よくある GUI ツールキットのギャラリースタイル)。左ペインはカテゴリ見出し + カテゴリ別 `ListBox` 群で、**全 ListBox が 1 つの `page` Signal を共有**する — SelectorBinding の「selected の値が items に無い状態 = 未選択」仕様により、他カテゴリの項目を選ぶと前のカテゴリの選択表示が自然に外れる。右ペインは page の値を**単一要素の keyed リストとして `For.Each` に渡すページスイッチャ** — page が変わると key が変わり、keyed diff が旧ページを Dispose して新ページだけを構築する (unmount 切替が O(1)、名前→ファクトリ索引は ToDictionary なので Name 重複は起動時に fail-fast) — を `ScrollViewer` で包んだもの。Basic / Input / Selection / Display / Layout / Dynamic / Window の 7 カテゴリ 22 ページで、各ページはタイトル + 説明 + 動作デモ (`Pages.Page(...)` 共通レイアウト)。ページ一覧 (`Pages.Catalog`) が sidebar とページ切替の両方の源。Window の `menu` 引数のデモも兼ねる — メニューバー (`Pages.MainMenu`) のクリック結果は private な MenuLog Signal 経由で Menu ページに表示され (両方 Pages.Window.cs 内で完結)、トップレベル項目 `Page: ...` はリアクティブ header のデモ。実装は `Program.cs` (shell) + `Pages.cs` (Catalog + 共通レイアウト) + `Pages.{Basic,Input,Selection,Display,Layout,Dynamic,Window}.cs` (カテゴリ別 partial class)。
- `examples/Autosave/` — 同上のサンプル。**ライブラリ利用者が `Context.RegisterEffect` を直接呼ぶ**例 (他のサンプルはコンポーネント内蔵の effect だけで完結している)。メモ (`TextBox` の `multiline: true`) の内容 Signal を effect で watch してファイルへ自動保存し、保存時刻をステータス行にリアクティブ表示する — UI 以外の副作用 (ファイル I/O) を Signal に反応させるのが RegisterEffect を直接使う典型パターン、という位置づけ。effect 冒頭で `enabled` Signal (Autosave チェックボックス) を見て false なら text を読まずに抜けることで、依存の動的再追跡 (オフの間は保存が走らず、再オンで最新内容がまとめて保存される) のデモも兼ねる。外枠は `DockPanel` (ヘッダー Top / ステータス Bottom / メモ欄が残りを fill)。
- `examples/CounterVB/` — Counter の VB.NET 移植 (`WinExe`、`PublishAot` なし — VB は Native AOT 非対応)。ライブラリが VB から使えることの実証用。VB 側の流儀: collection initializer は `From { ... }` (DockPanel 等の複数引数 Add はネスト波括弧)、ラムダは `Function()` / `Sub()`、named argument は `:=`。`For` クラスは VB の予約語なので使うときは `[For].Each(...)` とエスケープする (このサンプルでは未使用)。
- `examples/CounterFS/` — Counter の F# 移植 (`WinExe`、`PublishAot=true` — F# は Native AOT 対応だが FSharp.Core 由来の IL2104 / IL3053 トリム警告が出る)。ライブラリが F# から使えることの実証用。F# 側の流儀: collection initializer 構文が無いので `stack` / `menu` / `submenu` の小さなヘルパー関数 (`Component list` 等を受けて `Add` を回す) で子を渡す — 引数の要素型が決まっているので list 内の各要素は暗黙アップキャストされる。ラムダはメソッド引数に直接渡せば `Func` / `Action` に自動変換される (`new Label(fun () -> ...)`)、Signal の書き込みは `count.Value <- ...`、named argument は `=`、`Component` は IDisposable なので `new` を明示しないと FS0760 警告。`[<EntryPoint; STAThread>]` の `main` は `App.Run` を呼んで `0` を返す。`Program.fs` は fsproj の `Compile Include` で明示列挙 (F# はコンパイル順が明示)。
- `examples/CounterBrowser/` — Counter のブラウザ (WebAssembly) 版 PoC (`Microsoft.NET.Sdk.WebAssembly`、`net10.0-browser`、`OutputType=Exe`)。エントリは `App.Run` ではなく `App.RunBrowser(args, () => rootComponent)` — ブラウザは single view で OS ウィンドウが無いため `Window` / `Menu` / `Dialog` は使えず、root の `Component` を直接渡して `wwwroot/index.html` の `<div id="out">` にマウントされる。`wwwroot/` (index.html / main.js) と `runtimeconfig.template.json` は Avalonia 公式テンプレート (avalonia-dotnet-templates の xplat) 準拠の定型。**ビルドには wasm-tools workload が必須** — SkiaSharp / HarfBuzz のネイティブ `.a` を `dotnet.native.wasm` へ再リンクするため。未インストールでもビルドは通ってしまうが (`NativeFileReference ... won't be linked in` 警告が出る) 実行時に描画が壊れる。`dotnet run` で dev サーバーが立ち既定ブラウザが開く (launchSettings.json の port 5235)。
- `SolidCS.sln` — 上記 12 プロジェクトを束ねる。`examples` は solution folder 配下。

## よく使うコマンド

すべてリポジトリルートから実行する想定:

```pwsh
dotnet build SolidCS.sln                                   # ソリューション全体をビルド
dotnet test SolidCS.Tests                                  # 全テスト実行
dotnet test SolidCS.Tests --filter "FullyQualifiedName~SignalTests.SubscribeTest"   # 単一テスト
dotnet run --project examples/Counter                      # Counter サンプル起動
dotnet run --project examples/CounterVB                    # Counter の VB.NET 版を起動 (VB からの利用確認)
dotnet run --project examples/CounterFS                    # Counter の F# 版を起動 (F# からの利用確認)
dotnet run --project examples/Form                         # Form サンプル起動 (TextBox/CheckBox の双方向バインド確認)
dotnet run --project examples/AppShell                     # AppShell サンプル起動 (DockPanel レイアウト確認)
dotnet run --project examples/Todo                         # Todo サンプル起動 (For/Show の動的 children 確認)
dotnet run --project examples/Gallery                      # Gallery サンプル起動 (ComboBox/ListBox/TabControl 確認)
dotnet run --project examples/Autosave                     # Autosave サンプル起動 (ユーザー定義 effect の確認)
dotnet workload install wasm-tools                         # 初回のみ: CounterBrowser のビルドに必須 (ネイティブ再リンク)
dotnet run --project examples/CounterBrowser               # ブラウザ (WASM) 版 Counter — dev サーバー起動 + ブラウザが開く
dotnet publish examples/Counter -c Release                 # サンプルを AOT で発行
dotnet pack SolidCS.sln -c Release -o artifacts/packages   # NuGet パッケージ作成 (SolidCS / SolidCS.Avalonia + snupkg)
```

## NuGet パッケージング

`SolidCS` / `SolidCS.Avalonia` の 2 つだけを pack する (version 0.1.0、MIT)。メタデータは各 csproj の PropertyGroup に直接書いてある (Directory.Build.props に共通化はしていない)。examples は `examples/Directory.Build.props` の `IsPackable=false` で、テストは自身の csproj で pack 対象外 — なのでソリューション全体の `dotnet pack` でライブラリ 2 つだけが出る。パッケージ README はリポジトリルートの `README.md` (英語) を両パッケージに同梱。`GenerateDocumentationFile` が有効なので `///` XML doc は IntelliSense に載る (未ドキュメントメンバーの CS1591 は NoWarn 済み)。コミットメッセージは 0c97019 以降**英語**で書く規約。

## コアアーキテクチャ

リアクティビティは `SolidCS/Signal.cs` / `Context.cs` / `Effect.cs` の 3 ファイルで成立しており、依存関係はないので全部読めば把握できる。要点は以下:

- `Context` はプロセス全体で 1 つの **singleton** (`Context.Instance`)。`AsyncLocal<Stack<Effect>> Subscribers` に「現在登録中の effect」を積み、`Signal<T>.Value` の getter がそのスタックの先頭を `TryPeek` して自分の購読者リストに追加する。これが依存追跡の本体。
- `Context.RegisterEffect(Action)` は `Effect` を 1 つ作って `effect.Run()` を呼ぶだけ。Push/Pop と依存の張り替えは `Effect.Run()` の責務。
- `Effect.Run()` は (1) 前回の購読を `Untrack()` で全部解除、(2) 自分を Subscribers に Push、(3) コールバックを実行、(4) finally で Pop、という 1 セット。これにより条件分岐で読まれる Signal が変わっても依存が再追跡される。`_disposed` 中はノーオペ。
- `Signal<T>.Value` の getter は `Context.Instance.Subscribers.TryPeek` で現在の Effect を取り、`_subscribers` (HashSet) に追加。同時に effect 側に「自分を購読者リストから外す」cleanup を仕込んで、次の `Run()` 冒頭の `Untrack()` で解除されるようにする。
- `Signal<T>.Value` の setter は `EqualityComparer<T>.Default` で前値と一致した場合は通知をスキップ。購読者がいる場合は `_subscribers.ToArray()` でスナップショットを取ってから反復する (通知中に effect が _subscribers を変更してもコレクション変更例外にならない)。同一通知パス内で Dispose された effect にスナップショット経由で通知が届いても `Effect._disposed` ガードでノーオペ (この挙動は `SignalTests` / `UntrackTests` で固定済み — For/Show の再構築が依存する)。
- `Context.Untrack(Action)` / `Untrack<T>(Func<T>)` — 依存追跡を切って実行する。実装は Subscribers スタックを空スタックに差し替えて finally で戻す方式 (番兵 null 方式ではない) — これにより Untrack 中の `RegisterEffect` (子 Component の構築) は新しい空スタック上で正しく依存追跡できる。effect 内で子 Component を構築する For / Show が、子コンストラクタで読まれた Signal を自分に購読してしまう過追跡を防ぐのが主用途。public なのでライブラリユーザーも使える。

### Subscribers スタックは AsyncLocal で隔離される

`Context.Subscribers` は `AsyncLocal<Stack<Effect>>` で保持されていて、論理フロー (UI スレッド / 一つの async chain) ごとに別インスタンスを持つ。これにより effect 登録時の外側ロックは不要 — Subscribers スタックが他の論理フローと共有されないので Push/Pop が衝突しない。前提として **Signal の読み書きは単一の論理フロー内で完結させる** こと (Avalonia の UI スレッド制約とそのまま噛み合う)。別スレッドから Signal を書きたい場合は `Dispatcher.UIThread.Post` 等でマーシャリングする。

履歴メモ: 以前は `Context.Lock` を公開して呼び元が `lock(ctx.Lock) { ... }` で囲う規約だった (コミット `cea228e`)。AsyncLocal 化でその規約は撤廃されている。

### SolidCS.Avalonia の構成

`SolidCS.Avalonia` は以下のファイルで成立している:

- `Component.cs` — `abstract class Component : IDisposable`。`GetControl()` で内部の `Avalonia.Controls.Control` を返す。すべてのラッパーはこれを継承する。フォント設定の **`WithFont`** を基底で提供する: `WithFont(Font)` (静的) / `WithFont(Func<Font>)` (リアクティブ — 中で読まれた Signal の変化で再適用、実行時フォント変更) の 2 オーバーロードで、`this` を返すので collection initializer 内でチェーンできる (`new Label(...).WithFont(...)`)。適用は control の `Styles` に Style を足す方式 — `TextElement` の継承 attached property だと Fluent の ControlTheme (Button 等が FontSize を setter で持つ) に負けるが、Style は ControlTheme より優先されるため**コンテナに当てると子孫全体にカスケード**し、ネストした WithFont は近い方が勝つ。FontSize 等を `TemplatedControl` 系と `TextBlock` が別々の型で持つ都合で同内容の Style を 2 本 (selector `Is<TemplatedControl>` / `Is<TextBlock>`) 当てている。2 回目の `WithFont` は `InvalidOperationException` で fail-fast。Dispose は **template method**: `Dispose()` (非仮想) が WithFont の effect を解放してから `protected virtual OnDispose()` を呼ぶ — 派生クラスは `OnDispose()` をオーバーライドし、base 呼び出しは不要。
- `Label.cs` — `Func<string> contentFunc` を受け取って `Context.Instance.RegisterEffect(() => _label.Content = contentFunc())` を呼ぶ。effect 内で読まれた Signal が変わると Label が自動更新される。`Dispose()` で effect を解放。第 2 引数で `HorizontalAlignment` を渡せる (デフォルト `Center`)。
- `TextBlock.cs` — Label の TextBlock 版。表示専用テキストでは Label の `Content` (object) より TextBlock の `Text` (string) の方が軽量で素直なのでこちらを使う。API は Label と同じ + 第 3 引数 `textWrapping` (自前 `TextWrapping` 列挙型、デフォルト `NoWrap` = Avalonia デフォルト) で折り返しを指定できる。
- `Button.cs` — `Func<string> contentFunc` と `Action onClick` を受け取り、Content は Label と同じく effect でリアクティブに更新する。第 3 引数で `HorizontalAlignment` を渡せる (デフォルト `Center`)。
- `TextBox.cs` — `Signal<string> value` を受け取り、**双方向バインド**する。effect で Signal → UI、`TextChanged` ハンドラで UI → Signal。Signal の setter の equality check で循環が止まるので effect 経由の代入が引き起こす TextChanged のエコーは無限ループにならない。デフォルト `HorizontalAlignment` は `Stretch` (入力欄は幅いっぱい使う想定)。第 3 引数 `multiline` (デフォルト false) で複数行モード — AcceptsReturn + 折り返し + コンテンツ上寄せ (縦に stretch されたときテキストが縦中央に浮かないように) をまとめて設定する。
- `CheckBox.cs` — `Signal<bool> value` と `Func<string> contentFunc` を受け取り、value は双方向バインド、content はリアクティブ表示。`IsChecked` は `bool?` (Indeterminate あり) だが Signal<bool> に丸めるため `?? false`。イベントは `ToggleButton.IsCheckedChanged`。
- `Slider.cs` — `Signal<double> value` の双方向バインド。`min` / `max` (デフォルト 0〜100) は構造系オプション引数。イベントは `RangeBase.ValueChanged`。デフォルト `HorizontalAlignment` は `Stretch`。
- `NumericUpDown.cs` — `Signal<decimal> value` の双方向バインド。`min` / `max` は `decimal?` で null = 「制限なし」(Avalonia デフォルトのまま)、`increment` (デフォルト 1)。`Value` は `decimal?` (テキスト欄を空にできる) なので `?? 0` で丸める。デフォルト `Stretch`。
- `ProgressBar.cs` — 表示専用。`Func<double> valueFunc` のリアクティブ表示 (双方向ではない)。`min` / `max` オプション引数。デフォルト `Stretch`。
- `SelectorBinding.cs` — internal。ComboBox / ListBox 共通の「items + 選択中 Signal」バインド。ItemsSource には labelFunc で変換した文字列リストを渡し、T との対応は SelectedIndex で取る (Avalonia のテンプレート機構を公開 API に露出させない)。items effect が ItemsSource を差し替えるとき選択が飛ぶので `selected` から再適用する — その読取は `Untrack` 内 (items effect に selected を購読させない)。ラベルは items 再構築時にだけ再計算される (RadioGroup と違いリアクティブではない)。「未選択」は selected の値が items にない状態 (SelectedIndex = -1) で表現し、Signal に null / default を書き戻すことはない (UI → Signal は index >= 0 のときだけ)。
- `ComboBox.cs` — ドロップダウン選択。`ComboBox.Of(selected, items, labelFunc)` の static ファクトリ入口 (実体 `ComboBoxComponent<T>`)。items は `Func<IReadOnlyList<T>>` でリアクティブ (For と同じイミュータブル更新規約)。バインドは SelectorBinding に委譲。
- `ListBox.cs` — 選択セマンティクス付きリスト。API は ComboBox と同型 (`ListBox.Of` / `ListBoxComponent<T>` / SelectorBinding 共有)。棲み分けは「選択が要るなら ListBox、ただの動的表示なら For」。
- `TabControl.cs` — タブ切り替え。collection initializer の `{ header, content }` でタブを足す (header は静的 string / リアクティブ `Func<string>` の両対応)。content は全タブぶん構築時に作られてタブ切替でも生き続ける (keepAlive 相当 — 入力状態が保持される)。切替のたびに作り直したい場合は content を Show で包む。`Dispose()` は effect 解放 + 全 content へ伝播。
- `RadioGroup.cs` — 排他選択のラジオボタン群。個々の RadioButton の bool を管理させず「`Signal<T> selected` + `IEnumerable<T> options` + `Func<T,string> labelFunc`」のグループ単位 API。入口は `RadioGroup.Of(...)` static ファクトリ (For.Each と同じ理由 — コンストラクタはジェネリック推論が効かない)、実体は `RadioGroupComponent<T>` (内部 StackPanel、`orientation` オプション引数はデフォルト `Vertical`)。RadioButton の排他は同一親コンテナ単位の自動グループ化に乗るので GroupName 不要 = 複数グループが干渉しない。UI → Signal は `IsChecked == true` の側だけ拾う (排他で外れる側の false は無視)。
- `StackPanel.cs` — `Component` のコンテナ。`IEnumerable<Component>` 実装 + `Add(Component)` メソッドにより collection initializer 構文 (`new StackPanel { childA, childB }`) で子要素を渡せる。コンストラクタ引数で `Orientation` を渡せる (デフォルト `Vertical`)。`Dispose()` は全ての子に伝播する。
- `Window.cs` — トップレベルウィンドウ。root の `Component` と `title` / `width` / `height` / `menu` (すべてオプション引数) を受け取り、`Closed` イベントで content (と menu) の `Dispose()` を呼ぶ配線まで内蔵する。`menu` を渡すと内部で `DockPanel` を作ってメニューバーを上端にドックし、残り全面を content が埋める (`LastChildFill`)。`Component` は継承しない (子として置けるものではないため)。Avalonia 側の `Window` は internal な `GetWindow()` で `App` だけが取り出す。
- `Menu.cs` / `MenuElement.cs` / `MenuItem.cs` / `Separator.cs` — メニューバー。`Menu` はトップレベル `MenuItem` のコンテナ (collection initializer 可)。`MenuItem` は `Func<string> headerFunc` (状態系なので effect でリアクティブ更新) と `Action? onClick` を受け取り、自身も `MenuElement` のコンテナとしてサブメニューを collection initializer で組める。`MenuElement` は `MenuItem` / `Separator` だけをメニューに入れられるようにするためのマーカー抽象基底 (`Component` 派生)。`MenuItem.Click` は bubble する routed event で子のクリックが親にも届くため、ハンドラ内で `e.Source == _item` を確認して自分のクリックだけ拾う。`Dispose()` は effect 解放 + 子へ伝播。
- `App.cs` — static class。`App.Run(args, () => new Window(...))` が Avalonia の bootstrap (AppBuilder / `UsePlatformDetect` / `WithInterFont` / `LogToTrace`) と FluentTheme + Light テーマの適用をすべて隠蔽し、ウィンドウを表示してメインループに入る。`windowFactory` は Avalonia 初期化完了後に UI スレッドで呼ばれる — **Component / Window の生成は必ず factory の中で行う** (Main 冒頭で作ると Avalonia 未初期化でコントロール生成が壊れる)。internal static な `MainWindow` に表示したウィンドウを保持し、`Dialog` の owner に使う (Run は `IClassicDesktopStyleApplicationLifetime` を経由しないため lifetime からは取れない)。browser TFM では `Run` の代わりに `App.RunBrowser(args, rootFactory, divId = "out")` を公開する (`#if BROWSER` で丸ごと切り替え — `BROWSER` は net10.0-browser TFM で SDK が自動定義する定数)。実装は `Application` サブクラス (`BrowserApp`) の `OnFrameworkInitializationCompleted` でテーマ適用 + `ISingleViewApplicationLifetime.MainView` に rootFactory の Component を据え、`StartBrowserAppAsync(divId)` で index.html 内の div にアタッチする流儀。browser では `MainWindow` は常に null なので `Dialog` は使えない (owner fail-fast に落ちる)。
- `Dialog.cs` — モーダルダイアログの static class + `FileFilter` record。`Message` (OK のみ) / `Confirm` (OK/Cancel → `Task<bool>`、X 閉じは false) は Avalonia に MessageBox が無いので素の Avalonia コントロールで小さな modal Window を組む (静的コンテンツなので Component 不使用、IsDefault/IsCancel で Enter/Esc が効く)。`OpenFile` / `SaveFile` は StorageProvider のラップで `Task<string?>` (キャンセルは null)、フィルタは自前 `FileFilter(Name, Patterns)`。すべて async — await の続きは Avalonia の SynchronizationContext で UI スレッド再開されるので結果をそのまま Signal に書き戻せる。Button の onClick (Action) には async ラムダを渡す。owner 未表示 (App.Run 前) の呼び出しは `InvalidOperationException` で fail-fast。dialog は owner の `Topmost` を継承する (topmost な owner の裏に非 topmost なモーダルが開くと「見えないダイアログに親がロックされる」事故になるため)。
- `Grid.cs` — 行・列を文字列ミニ言語 (XAML の GridLength 記法: `"Auto"` / `"*"` / `"2*"` / `"100"` のカンマ区切り、パースは Avalonia の `RowDefinitions(string)` に委譲) で定義するグリッド。`RowDefinition` 等のオブジェクトは公開 API に露出しない。モードが 2 つ: **auto-flow** (`rows` 省略) は素の `Add(child)` で子を左→右に流し込み必要なだけ Auto 行を自動生成、**explicit** (`rows` 指定) は collection initializer の `{ child, row, col }` / `{ child, row, col, rowSpan, colSpan }` で明示配置。範囲外の配置は Avalonia が黙ってクランプする前に Add 時点で `ArgumentOutOfRangeException`、モード混在は `InvalidOperationException` で fail-fast。`Dispose()` は全子へ伝播。
- `WrapPanel.cs` — 子を並べて幅 (縦なら高さ) で折り返すコンテナ。API は StackPanel と同型 (collection initializer + `Orientation` オプション引数) だが、デフォルト Orientation は Avalonia に合わせて `Horizontal` (StackPanel の `Vertical` とは逆) なので注意。
- `DockPanel.cs` — 子を上下左右にドックするコンテナ。collection initializer で `{ child, Dock.Top }` (複数引数の `Add(Component, Dock)`) と `child` 単体 (Dock 指定なし) の両方を受け、コンストラクタ引数 `lastChildFill` (デフォルト true) で最後の子が残り全面を埋める。`Dispose()` は全子へ伝播。`Window` の menu 用内部 DockPanel とは独立 (あちらは素の Avalonia 型を直接使う)。
- `Show.cs` — 条件付き表示。`new Show(when, child, fallback, keepAlive)` で `Func<bool> when` が真なら `Func<Component> child`、偽なら `Func<Component>? fallback` (省略時は空) を表示する。実体は ContentControl の Content 差し替え。デフォルトは **unmount** (条件反転のたびに旧側を Dispose して作り直し — 状態リセット、SolidJS の Show と同じ)、`keepAlive: true` で一度構築した Component を保持して差し替えのみ (状態保持)。子の構築は `Context.Untrack` の中で行い、when の Signal だけを追跡する。**条件値が前回と同じなら何もしない** (`bool? _lastCond` ガード) — `Count > 0` のような派生条件では条件が反転しなくても effect が再実行されるため、これがないと子が無意味に破棄・再構築される。差し替えは新側を構築してから旧側を Dispose する順序 (ファクトリが throw しても旧側が生きたまま残る)。構築中にファクトリが when の読む Signal を書く再入は `InvalidOperationException` で fail-fast。
- `For.cs` — 動的リスト。`For.Each(() => items.Value, item => ..., key: null, orientation: ...)` の static ファクトリが入口 (C# はコンストラクタでジェネリック推論が効かないため)。実体の `ForComponent<T>` は内部 StackPanel を持つ自前パネル方式。**v2 (keyed diff)**: key (省略時はアイテム自身 — イミュータブル更新規約なら既存インスタンスが参照で一致) が一致する子は Component を再利用し、新規だけ render・消えた分だけ Dispose・並べ替えは `Children.Move` (位置不変の既存子は detach されないのでフォーカス等の UI 状態が残る)。**再利用された子は再 render されない** — アイテム内の変化はアイテム内の Signal で表現する規約 (key を Id にして中身だけ変えると表示が古いまま残る)。key 重複は fail-fast。リストは `Signal<IReadOnlyList<T>>` + イミュータブル更新規約 (`todos.Value = [.. todos.Value, item]`) — in-place 変更は setter の参照比較を素通りして通知されない。子構築の過追跡防止 (`Untrack`) は Show と同じ。render が途中で throw したら既存の子と表示は無傷 (作りかけの新規子だけ解放)。構築中に render がリスト Signal を書く再入は `InvalidOperationException` で fail-fast。
- `KeyedDiff.cs` — internal。For / TreeView 共通の keyed reconcile (`Reconcile(current, items, keyFn, create, dispose)`)。key 一致の既存 state を再利用、新規だけ create、消えた key だけ dispose。重複 key は fail-fast、create 途中の throw はそのラウンドの生成分だけ dispose して再 throw (呼び元の current は無傷)。コレクションへの反映 (並び順の差分適用) は呼び元の責務。
- `TreeView.cs` — 動的ツリー。`TreeView.Of(roots, labelFunc, childrenFunc, key, selected)` の static ファクトリ入口 (実体 `TreeViewComponent<T>`)。各ノード (internal `Node`) が自分の label effect と children effect を持ち、childrenFunc 内で読まれた Signal が変わったノードのサブツリーだけが `KeyedDiff.Reconcile` で更新される — key 一致のノードは TreeViewItem ごと再利用されるので**展開状態 (IsExpanded) が生き残る**。`selected` (`Signal<T>?`) を渡すと選択の双方向バインド — T ⇔ TreeViewItem の逆引きは `_itemMap` (key は兄弟間でしか一意でないため map で持つ)、「未選択」は値がツリー内に無い状態で表現し Signal に null/default を書き戻さない。ItemCollection に Move は無いので並べ替えは RemoveAt + Insert (IsExpanded はプロパティなので detach をまたいで保持)。ルート/各ノードの構築中に該当 Signal を書く再入は fail-fast。
- `ScrollViewer.cs` — 単一 child の `Component` を受け取ってスクロール可能にするラッパー。第 2 / 第 3 引数の `verticalScrollBar` / `horizontalScrollBar` (自前の `ScrollBarVisibility`) でスクロールバー表示を制御する。デフォルトは Avalonia と同じ縦 `Auto` / 横 `Disabled`。`Dispose()` は content へ伝播。
- `Font.cs` — `Component.WithFont` に渡す `Font` record (`Family` / `Size` / `Weight` / `Style`、全メンバーがオプションで null は「指定なし」= テーマ/外側の値のまま) と、自前列挙型 `FontWeight` / `FontStyle` + internal `ToAvalonia()`。`Family` は Avalonia の FontFamily 記法の文字列 (`"Consolas"` / `"Inter, Arial"` 等) で `FontFamily.Parse` に委譲。
- `Orientation.cs` / `HorizontalAlignment.cs` / `ScrollBarVisibility.cs` / `Dock.cs` / `TextWrapping.cs` — 公開 API 用の自前列挙型。ライブラリ利用者が Avalonia の型に触らずに済むよう、公開コンストラクタ引数では Avalonia の列挙型 (`Avalonia.Layout.Orientation` 等) を露出せずこちらを受け取る。各ファイル内の internal 拡張メソッド `ToAvalonia()` で Avalonia 側の列挙型へ変換する (キャストではなく switch 式で明示マッピング — 両者の値順序に依存しない)。

双方向バインドコンポーネント (`TextBox`, `CheckBox`) を新しく足すときは以下のパターンを踏襲:

1. コンストラクタで `Signal<T> value` を受け取る。
2. `Context.Instance.RegisterEffect(() => _control.Prop = value.Value)` で Signal → UI を貼る。
3. Avalonia 側の changed イベント (`TextChanged`, `IsCheckedChanged` 等) で `value.Value = _control.Prop` を書く。
4. 無限ループ防止は `Signal<T>.Value` setter の `EqualityComparer<T>.Default.Equals` に頼る (effect 経由で同値が書かれた場合 setter は何もしない)。Avalonia 側もたいてい同値代入では change event を発火しないので二重に守られる。

props 化方針: **状態系** (Content など値が時間で変わるもの) は `Func<T>` でリアクティブに受ける。**構造系** (Orientation, HorizontalAlignment など滅多に切り替わらないもの) は素の型のオプション引数で静的に受ける。**見た目系** (Foreground, FontSize, Background など) はコンストラクタ props にせず、Style ベースの専用メソッドでまとめて当てる (個別 props 化は API がふくらむので避ける) — フォント系は `Component.WithFont` で実現済みで、色などを足すときも同じ方式 (基底のメソッド + Style 適用) を踏襲する。かつて Button にあった `FontSize = 24` のハードコードは WithFont 導入時に撤去済み (Counter サンプル側の `WithFont(new Font(Size: 24))` に移動)。構造系 props の型が Avalonia の列挙型の場合は、そのまま露出せず `SolidCS.Avalonia` 側に対応する列挙型を新設して受ける (上記 `Orientation` / `HorizontalAlignment` のパターン)。

ラッパー実装ファイル内では Avalonia 型を `global::Avalonia.Controls.X` で完全修飾している。これは自身のクラス名 (`Label`, `Button`, `StackPanel`) と `Avalonia.Controls` 配下の同名型が衝突して `using Avalonia.Controls;` を素直に書けないため。新しいラッパーを足すときも同じ流儀を踏襲すること。

### ライブラリユーザーがコンポーネントを書くスタイル

`examples/Counter/Program.cs` の `Counter()` がリファレンス。`Component` を継承するのはライブラリ作者 (`Label` 等を作る側) で、ライブラリ利用者は **`Component` を返す関数** で UI を組み立てる SolidJS 風スタイルを取る:

```csharp
using SolidCS;
using SolidCS.Avalonia;

static Component Counter()
{
    var count = new Signal<int>(0);
    return new StackPanel
    {
        new Label(() => $"Count = {count.Value}"),
        new Button(() => count.Value += 1),
    };
}
```

エントリポイントは `App.Run(args, () => new Window(root, title: "...", width: ..., height: ...))` の 1 行。`Window` がウィンドウ閉時の `root.Dispose()` を配線するので、利用者が effect の解放を意識する必要はない (Dispose は子コンポへ再帰的に伝播する)。

これにより利用側 (examples) の using は `System` / `SolidCS` / `SolidCS.Avalonia` の 3 つだけで、**Avalonia の型・namespace には一切触れない**。かつては Avalonia の `Window` / `AppBuilder` を直接触るため完全修飾や `using static Avalonia.Controls.DesktopApplicationExtensions;` が必要だったが、`Window` / `App` の導入で撤廃済み。

### リアクティブ UI コンポーネントを新しく追加するパターン

新しいラッパー (例えば `STextBox` や `SCheckBox`) を `SolidCS.Avalonia` に足すときは以下を踏襲する:

1. `Component` を継承する。
2. コンストラクタ内で Avalonia の Control を生成。
3. リアクティブに更新したいプロパティごとに `Context.Instance.RegisterEffect(() => _control.Prop = func())` を呼び、戻り値の `IDisposable` をフィールドに保持する。
4. `GetControl()` で生成した Control を返す。
5. `OnDispose()` (protected override) ですべての effect を解放する (コンテナ系の場合はさらに子へ `Dispose()` を伝播)。`Dispose()` 自体は基底の非仮想メソッドで、WithFont の effect 解放 → `OnDispose()` の順に呼ぶ。

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

SolidJS 風の Signal / Effect ベースのリアクティブプリミティブを C# で実装した実験プロジェクト。`SolidCS` 本体は UI フレームワーク非依存のリアクティブコア、`SolidCS.Avalonia` がそのコアの上に Avalonia 用 UI コンポーネントをラップして提供する小さな GUI フレームワーク、`examples/Counter` がそれらを利用するリファレンス実装、という三層構成。

## ソリューション構成

- `SolidCS/` — コアライブラリ (net10.0, nullable enable)。`Signal<T>`、`Context`、`Effect` の 3 ファイルで構成される。UI 非依存。
- `SolidCS.Avalonia/` — Avalonia 11.3.18 ラッパーライブラリ (net10.0)。`Component` 抽象基底と `Label` / `Button` / `StackPanel` などのリアクティブ UI コンポーネント、およびアプリ起動用の `App` / `Window` を `SolidCS.Avalonia` namespace に公開する。型名は Avalonia 側 (`Avalonia.Controls.Label` 等) と素朴に被るので、ラッパー実装の内側では Avalonia 型を `global::Avalonia.Controls.X` で完全修飾している。`SolidCS` を ProjectReference、Avalonia 系パッケージ (`Avalonia` / `Avalonia.Desktop` / `Avalonia.Themes.Fluent` / `Avalonia.Fonts.Inter`) を PackageReference — bootstrap を担う関係で Desktop / テーマ / フォントもライブラリ側が持つ。
- `SolidCS.Tests/` — xUnit テスト (net10.0)。`SolidCS` を ProjectReference。
- `examples/Counter/` — サンプル (`PublishAot=true`, `WinExe`)。`Label` / `Button` / `StackPanel` を使う最小デモ + `Menu` / `MenuItem` / `Separator` のメニューバーデモ (リアクティブ header とクリックアクション)。**Avalonia を直接参照しない** — `SolidCS` と `SolidCS.Avalonia` の ProjectReference のみで、Avalonia パッケージは SolidCS.Avalonia から推移的に来る。
- `examples/Form/` — 同上のサンプル。入力系コントロール (`TextBox` / `CheckBox`) と表示系 (`TextBlock` / `Label`) の双方向バインドおよびリアクティブ表示の動作確認用。root を `ScrollViewer` で包み、ウィンドウより縦長のリアクティブコンテンツでスクロールの動作確認も兼ねる。フォーム欄は `Grid` の auto-flow モード、統計 + サマリ行は explicit モード (colSpan 付き) で組んであり、Grid 両モードのデモでもある。さらに root の `WithFont(Func 版)` + A-/A+ ボタンで全体のフォントサイズを実行時に変えるデモと、挨拶行の静的 `WithFont` (20px Bold — 内側が勝つので root の変更に追従しない) のデモを兼ねる。
- `examples/AppShell/` — 同上のサンプル。`DockPanel` でアプリの外枠 (ヘッダー Top / ステータスバー Bottom / サイドバー Left / メインコンテンツが残りを埋める) を組み、サイドバーのボタンで `Signal<string>` のページを切り替えるデモ。メインコンテンツには `WrapPanel` のタグボタンギャラリー (ウィンドウ幅で折り返し + クリックで selected 更新) も同居。
- `examples/Todo/` — 同上のサンプル。動的 children (`For.Each` / `Show`) のデモ。TODO の追加・削除でリストの行が増減し (`Signal<IReadOnlyList<T>>` のイミュータブル更新規約)、完了数の統計行を `Show` (fallback 付き) で出し分ける。設計の経緯は `docs/dynamic-children-design.md`。
- `SolidCS.sln` — 上記 7 プロジェクトを束ねる。`examples` は solution folder 配下。

## よく使うコマンド

すべてリポジトリルートから実行する想定:

```pwsh
dotnet build SolidCS.sln                                   # ソリューション全体をビルド
dotnet test SolidCS.Tests                                  # 全テスト実行
dotnet test SolidCS.Tests --filter "FullyQualifiedName~SignalTests.SubscribeTest"   # 単一テスト
dotnet run --project examples/Counter                      # Counter サンプル起動
dotnet run --project examples/Form                         # Form サンプル起動 (TextBox/CheckBox の双方向バインド確認)
dotnet run --project examples/AppShell                     # AppShell サンプル起動 (DockPanel レイアウト確認)
dotnet run --project examples/Todo                         # Todo サンプル起動 (For/Show の動的 children 確認)
dotnet publish examples/Counter -c Release                 # サンプルを AOT で発行
```

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
- `TextBlock.cs` — Label の TextBlock 版。表示専用テキストでは Label の `Content` (object) より TextBlock の `Text` (string) の方が軽量で素直なのでこちらを使う。API は Label と同じ。
- `Button.cs` — `Func<string> contentFunc` と `Action onClick` を受け取り、Content は Label と同じく effect でリアクティブに更新する。第 3 引数で `HorizontalAlignment` を渡せる (デフォルト `Center`)。
- `TextBox.cs` — `Signal<string> value` を受け取り、**双方向バインド**する。effect で Signal → UI、`TextChanged` ハンドラで UI → Signal。Signal の setter の equality check で循環が止まるので effect 経由の代入が引き起こす TextChanged のエコーは無限ループにならない。デフォルト `HorizontalAlignment` は `Stretch` (入力欄は幅いっぱい使う想定)。
- `CheckBox.cs` — `Signal<bool> value` と `Func<string> contentFunc` を受け取り、value は双方向バインド、content はリアクティブ表示。`IsChecked` は `bool?` (Indeterminate あり) だが Signal<bool> に丸めるため `?? false`。イベントは `ToggleButton.IsCheckedChanged`。
- `StackPanel.cs` — `Component` のコンテナ。`IEnumerable<Component>` 実装 + `Add(Component)` メソッドにより collection initializer 構文 (`new StackPanel { childA, childB }`) で子要素を渡せる。コンストラクタ引数で `Orientation` を渡せる (デフォルト `Vertical`)。`Dispose()` は全ての子に伝播する。
- `Window.cs` — トップレベルウィンドウ。root の `Component` と `title` / `width` / `height` / `menu` (すべてオプション引数) を受け取り、`Closed` イベントで content (と menu) の `Dispose()` を呼ぶ配線まで内蔵する。`menu` を渡すと内部で `DockPanel` を作ってメニューバーを上端にドックし、残り全面を content が埋める (`LastChildFill`)。`Component` は継承しない (子として置けるものではないため)。Avalonia 側の `Window` は internal な `GetWindow()` で `App` だけが取り出す。
- `Menu.cs` / `MenuElement.cs` / `MenuItem.cs` / `Separator.cs` — メニューバー。`Menu` はトップレベル `MenuItem` のコンテナ (collection initializer 可)。`MenuItem` は `Func<string> headerFunc` (状態系なので effect でリアクティブ更新) と `Action? onClick` を受け取り、自身も `MenuElement` のコンテナとしてサブメニューを collection initializer で組める。`MenuElement` は `MenuItem` / `Separator` だけをメニューに入れられるようにするためのマーカー抽象基底 (`Component` 派生)。`MenuItem.Click` は bubble する routed event で子のクリックが親にも届くため、ハンドラ内で `e.Source == _item` を確認して自分のクリックだけ拾う。`Dispose()` は effect 解放 + 子へ伝播。
- `App.cs` — static class。`App.Run(args, () => new Window(...))` が Avalonia の bootstrap (AppBuilder / `UsePlatformDetect` / `WithInterFont` / `LogToTrace`) と FluentTheme + Light テーマの適用をすべて隠蔽し、ウィンドウを表示してメインループに入る。`windowFactory` は Avalonia 初期化完了後に UI スレッドで呼ばれる — **Component / Window の生成は必ず factory の中で行う** (Main 冒頭で作ると Avalonia 未初期化でコントロール生成が壊れる)。
- `Grid.cs` — 行・列を文字列ミニ言語 (XAML の GridLength 記法: `"Auto"` / `"*"` / `"2*"` / `"100"` のカンマ区切り、パースは Avalonia の `RowDefinitions(string)` に委譲) で定義するグリッド。`RowDefinition` 等のオブジェクトは公開 API に露出しない。モードが 2 つ: **auto-flow** (`rows` 省略) は素の `Add(child)` で子を左→右に流し込み必要なだけ Auto 行を自動生成、**explicit** (`rows` 指定) は collection initializer の `{ child, row, col }` / `{ child, row, col, rowSpan, colSpan }` で明示配置。範囲外の配置は Avalonia が黙ってクランプする前に Add 時点で `ArgumentOutOfRangeException`、モード混在は `InvalidOperationException` で fail-fast。`Dispose()` は全子へ伝播。
- `WrapPanel.cs` — 子を並べて幅 (縦なら高さ) で折り返すコンテナ。API は StackPanel と同型 (collection initializer + `Orientation` オプション引数) だが、デフォルト Orientation は Avalonia に合わせて `Horizontal` (StackPanel の `Vertical` とは逆) なので注意。
- `DockPanel.cs` — 子を上下左右にドックするコンテナ。collection initializer で `{ child, Dock.Top }` (複数引数の `Add(Component, Dock)`) と `child` 単体 (Dock 指定なし) の両方を受け、コンストラクタ引数 `lastChildFill` (デフォルト true) で最後の子が残り全面を埋める。`Dispose()` は全子へ伝播。`Window` の menu 用内部 DockPanel とは独立 (あちらは素の Avalonia 型を直接使う)。
- `Show.cs` — 条件付き表示。`new Show(when, child, fallback, keepAlive)` で `Func<bool> when` が真なら `Func<Component> child`、偽なら `Func<Component>? fallback` (省略時は空) を表示する。実体は ContentControl の Content 差し替え。デフォルトは **unmount** (条件反転のたびに旧側を Dispose して作り直し — 状態リセット、SolidJS の Show と同じ)、`keepAlive: true` で一度構築した Component を保持して差し替えのみ (状態保持)。子の構築は `Context.Untrack` の中で行い、when の Signal だけを追跡する。**条件値が前回と同じなら何もしない** (`bool? _lastCond` ガード) — `Count > 0` のような派生条件では条件が反転しなくても effect が再実行されるため、これがないと子が無意味に破棄・再構築される。差し替えは新側を構築してから旧側を Dispose する順序 (ファクトリが throw しても旧側が生きたまま残る)。構築中にファクトリが when の読む Signal を書く再入は `InvalidOperationException` で fail-fast。
- `For.cs` — 動的リスト。`For.Each(() => items.Value, item => ..., key: null, orientation: ...)` の static ファクトリが入口 (C# はコンストラクタでジェネリック推論が効かないため)。実体の `ForComponent<T>` は内部 StackPanel を持つ自前パネル方式で、リスト Signal の変化のたび **v1 は全子を Dispose して作り直す** (アイテム内の UI 状態は消える)。`key` 引数は v2 の keyed diff 用にシグネチャだけ確保して未使用。リストは `Signal<IReadOnlyList<T>>` + イミュータブル更新規約 (`todos.Value = [.. todos.Value, item]`) — in-place 変更は setter の参照比較を素通りして通知されない。子構築の過追跡防止 (`Untrack`) は Show と同じ。再構築は**新世代を全部構築してから旧世代を破棄** (render が途中で throw したら旧世代は無傷のまま残り、作りかけの新世代は解放)、パネルへの投入は `Children.AddRange` で一括 (逐次 Add だと子の数だけレイアウト無効化が走る)。構築中に render がリスト Signal を書く再入は `InvalidOperationException` で fail-fast。
- `ScrollViewer.cs` — 単一 child の `Component` を受け取ってスクロール可能にするラッパー。第 2 / 第 3 引数の `verticalScrollBar` / `horizontalScrollBar` (自前の `ScrollBarVisibility`) でスクロールバー表示を制御する。デフォルトは Avalonia と同じ縦 `Auto` / 横 `Disabled`。`Dispose()` は content へ伝播。
- `Font.cs` — `Component.WithFont` に渡す `Font` record (`Family` / `Size` / `Weight` / `Style`、全メンバーがオプションで null は「指定なし」= テーマ/外側の値のまま) と、自前列挙型 `FontWeight` / `FontStyle` + internal `ToAvalonia()`。`Family` は Avalonia の FontFamily 記法の文字列 (`"Consolas"` / `"Inter, Arial"` 等) で `FontFamily.Parse` に委譲。
- `Orientation.cs` / `HorizontalAlignment.cs` / `ScrollBarVisibility.cs` / `Dock.cs` — 公開 API 用の自前列挙型。ライブラリ利用者が Avalonia の型に触らずに済むよう、公開コンストラクタ引数では Avalonia の列挙型 (`Avalonia.Layout.Orientation` 等) を露出せずこちらを受け取る。各ファイル内の internal 拡張メソッド `ToAvalonia()` で Avalonia 側の列挙型へ変換する (キャストではなく switch 式で明示マッピング — 両者の値順序に依存しない)。

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

## 既知のゴミ

- `SolidCS/Class1.cs` と `SolidCS.Tests/UnitTest1.cs` はテンプレートの残骸。触らなくてよいが、新規ファイル追加のサンプルとして参照しないこと。

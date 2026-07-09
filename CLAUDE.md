# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

SolidJS 風の Signal / Effect ベースのリアクティブプリミティブを C# で実装した実験プロジェクト。`SolidCS` 本体は UI フレームワーク非依存のリアクティブコア、`SolidCS.Avalonia` がそのコアの上に Avalonia 用 UI コンポーネントをラップして提供する小さな GUI フレームワーク、`examples/Counter` がそれらを利用するリファレンス実装、という三層構成。

## ソリューション構成

- `SolidCS/` — コアライブラリ (net10.0, nullable enable)。`Signal<T>`、`Context`、`Effect` の 3 ファイルで構成される。UI 非依存。
- `SolidCS.Avalonia/` — Avalonia 11.3.18 ラッパーライブラリ (net10.0)。`Component` 抽象基底と `Label` / `Button` / `StackPanel` などのリアクティブ UI コンポーネントを `SolidCS.Avalonia` namespace に公開する。型名は Avalonia 側 (`Avalonia.Controls.Label` 等) と素朴に被るので、ラッパー実装の内側では Avalonia 型を `global::Avalonia.Controls.X` で完全修飾している。`SolidCS` と `Avalonia` パッケージを ProjectReference / PackageReference。
- `SolidCS.Tests/` — xUnit テスト (net10.0)。`SolidCS` を ProjectReference。
- `examples/Counter/` — Avalonia 11.3.18 製サンプル (`PublishAot=true`, `WinExe`)。`Label` / `Button` / `StackPanel` を使う最小デモ。
- `examples/Form/` — 同じく Avalonia 11.3.18 製サンプル。入力系コントロール (`TextBox` / `CheckBox`) と表示系 (`TextBlock` / `Label`) の双方向バインドおよびリアクティブ表示の動作確認用。
- `SolidCS.sln` — 上記 5 プロジェクトを束ねる。`examples` は solution folder 配下。

## よく使うコマンド

すべてリポジトリルートから実行する想定:

```pwsh
dotnet build SolidCS.sln                                   # ソリューション全体をビルド
dotnet test SolidCS.Tests                                  # 全テスト実行
dotnet test SolidCS.Tests --filter "FullyQualifiedName~SignalTests.SubscribeTest"   # 単一テスト
dotnet run --project examples/Counter                      # Counter サンプル起動
dotnet run --project examples/Form                         # Form サンプル起動 (TextBox/CheckBox の双方向バインド確認)
dotnet publish examples/Counter -c Release                 # サンプルを AOT で発行
```

## コアアーキテクチャ

リアクティビティは `SolidCS/Signal.cs` / `Context.cs` / `Effect.cs` の 3 ファイルで成立しており、依存関係はないので全部読めば把握できる。要点は以下:

- `Context` はプロセス全体で 1 つの **singleton** (`Context.Instance`)。`AsyncLocal<Stack<Effect>> Subscribers` に「現在登録中の effect」を積み、`Signal<T>.Value` の getter がそのスタックの先頭を `TryPeek` して自分の購読者リストに追加する。これが依存追跡の本体。
- `Context.RegisterEffect(Action)` は `Effect` を 1 つ作って `effect.Run()` を呼ぶだけ。Push/Pop と依存の張り替えは `Effect.Run()` の責務。
- `Effect.Run()` は (1) 前回の購読を `Untrack()` で全部解除、(2) 自分を Subscribers に Push、(3) コールバックを実行、(4) finally で Pop、という 1 セット。これにより条件分岐で読まれる Signal が変わっても依存が再追跡される。`_disposed` 中はノーオペ。
- `Signal<T>.Value` の getter は `Context.Instance.Subscribers.TryPeek` で現在の Effect を取り、`_subscribers` (HashSet) に追加。同時に effect 側に「自分を購読者リストから外す」cleanup を仕込んで、次の `Run()` 冒頭の `Untrack()` で解除されるようにする。
- `Signal<T>.Value` の setter は `EqualityComparer<T>.Default` で前値と一致した場合は通知をスキップ。購読者がいる場合は `_subscribers.ToArray()` でスナップショットを取ってから反復する (通知中に effect が _subscribers を変更してもコレクション変更例外にならない)。

### Subscribers スタックは AsyncLocal で隔離される

`Context.Subscribers` は `AsyncLocal<Stack<Effect>>` で保持されていて、論理フロー (UI スレッド / 一つの async chain) ごとに別インスタンスを持つ。これにより effect 登録時の外側ロックは不要 — Subscribers スタックが他の論理フローと共有されないので Push/Pop が衝突しない。前提として **Signal の読み書きは単一の論理フロー内で完結させる** こと (Avalonia の UI スレッド制約とそのまま噛み合う)。別スレッドから Signal を書きたい場合は `Dispatcher.UIThread.Post` 等でマーシャリングする。

履歴メモ: 以前は `Context.Lock` を公開して呼び元が `lock(ctx.Lock) { ... }` で囲う規約だった (コミット `cea228e`)。AsyncLocal 化でその規約は撤廃されている。

### SolidCS.Avalonia の構成

`SolidCS.Avalonia` は以下のファイルで成立している:

- `Component.cs` — `abstract class Component : IDisposable`。`GetControl()` で内部の `Avalonia.Controls.Control` を返す。すべてのラッパーはこれを継承する。
- `Label.cs` — `Func<string> contentFunc` を受け取って `Context.Instance.RegisterEffect(() => _label.Content = contentFunc())` を呼ぶ。effect 内で読まれた Signal が変わると Label が自動更新される。`Dispose()` で effect を解放。第 2 引数で `HorizontalAlignment` を渡せる (デフォルト `Center`)。
- `TextBlock.cs` — Label の TextBlock 版。表示専用テキストでは Label の `Content` (object) より TextBlock の `Text` (string) の方が軽量で素直なのでこちらを使う。API は Label と同じ。
- `Button.cs` — `Func<string> contentFunc` と `Action onClick` を受け取り、Content は Label と同じく effect でリアクティブに更新する。第 3 引数で `HorizontalAlignment` を渡せる (デフォルト `Center`)。
- `TextBox.cs` — `Signal<string> value` を受け取り、**双方向バインド**する。effect で Signal → UI、`TextChanged` ハンドラで UI → Signal。Signal の setter の equality check で循環が止まるので effect 経由の代入が引き起こす TextChanged のエコーは無限ループにならない。デフォルト `HorizontalAlignment` は `Stretch` (入力欄は幅いっぱい使う想定)。
- `CheckBox.cs` — `Signal<bool> value` と `Func<string> contentFunc` を受け取り、value は双方向バインド、content はリアクティブ表示。`IsChecked` は `bool?` (Indeterminate あり) だが Signal<bool> に丸めるため `?? false`。イベントは `ToggleButton.IsCheckedChanged`。
- `StackPanel.cs` — `Component` のコンテナ。`IEnumerable<Component>` 実装 + `Add(Component)` メソッドにより collection initializer 構文 (`new StackPanel { childA, childB }`) で子要素を渡せる。コンストラクタ引数で `Orientation` を渡せる (デフォルト `Vertical`)。`Dispose()` は全ての子に伝播する。
- `Orientation.cs` / `HorizontalAlignment.cs` — 公開 API 用の自前列挙型。ライブラリ利用者が Avalonia の型に触らずに済むよう、公開コンストラクタ引数では Avalonia の列挙型 (`Avalonia.Layout.Orientation` 等) を露出せずこちらを受け取る。各ファイル内の internal 拡張メソッド `ToAvalonia()` で Avalonia 側の列挙型へ変換する (キャストではなく switch 式で明示マッピング — 両者の値順序に依存しない)。

双方向バインドコンポーネント (`TextBox`, `CheckBox`) を新しく足すときは以下のパターンを踏襲:

1. コンストラクタで `Signal<T> value` を受け取る。
2. `Context.Instance.RegisterEffect(() => _control.Prop = value.Value)` で Signal → UI を貼る。
3. Avalonia 側の changed イベント (`TextChanged`, `IsCheckedChanged` 等) で `value.Value = _control.Prop` を書く。
4. 無限ループ防止は `Signal<T>.Value` setter の `EqualityComparer<T>.Default.Equals` に頼る (effect 経由で同値が書かれた場合 setter は何もしない)。Avalonia 側もたいてい同値代入では change event を発火しないので二重に守られる。

props 化方針: **状態系** (Content など値が時間で変わるもの) は `Func<T>` でリアクティブに受ける。**構造系** (Orientation, HorizontalAlignment など滅多に切り替わらないもの) は素の型のオプション引数で静的に受ける。**見た目系** (Foreground, FontSize, Background など) は現状ハードコードだが、将来は Avalonia の Style system / 専用 Style プロパティでまとめて当てる方向で考える (個別 props 化は API がふくらむので避ける)。構造系 props の型が Avalonia の列挙型の場合は、そのまま露出せず `SolidCS.Avalonia` 側に対応する列挙型を新設して受ける (上記 `Orientation` / `HorizontalAlignment` のパターン)。

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

ルートで作った `Component` の `GetControl()` を `Window.Content` に渡し、`Window.Closed` で `root.Dispose()` を呼ぶ — これで全 effect が解放される (Dispose は子コンポへ再帰的に伝播する)。

利用側のサンプル (Counter) は Avalonia の `Window` / `Application.Run` extension を生で触る関係で `using Avalonia.Controls;` を書くと `Label` / `Button` / `StackPanel` が `SolidCS.Avalonia` 側と曖昧参照になる。回避策として Counter サンプルでは:

- `Window` などの Avalonia 型を `global::Avalonia.Controls.Window` 形式で完全修飾する
- `app.Run(win)` を解決するために `using static Avalonia.Controls.DesktopApplicationExtensions;` を入れる (型は import されないので衝突しない)

将来 `SolidCS.Avalonia.Window` も用意したらこの完全修飾は消える想定。

### リアクティブ UI コンポーネントを新しく追加するパターン

新しいラッパー (例えば `STextBox` や `SCheckBox`) を `SolidCS.Avalonia` に足すときは以下を踏襲する:

1. `Component` を継承する。
2. コンストラクタ内で Avalonia の Control を生成。
3. リアクティブに更新したいプロパティごとに `Context.Instance.RegisterEffect(() => _control.Prop = func())` を呼び、戻り値の `IDisposable` をフィールドに保持する。
4. `GetControl()` で生成した Control を返す。
5. `Dispose()` ですべての effect を解放する (コンテナ系の場合はさらに子へ Dispose を伝播)。

## 既知のゴミ

- `SolidCS/Class1.cs` と `SolidCS.Tests/UnitTest1.cs` はテンプレートの残骸。触らなくてよいが、新規ファイル追加のサンプルとして参照しないこと。

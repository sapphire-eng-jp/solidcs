# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

SolidJS 風の Signal / Effect ベースのリアクティブプリミティブを C# で実装した実験プロジェクト。`SolidCS` 本体は UI フレームワーク非依存で、`examples/Counter` で Avalonia と組み合わせて使う方法を示している。

## ソリューション構成

- `SolidCS/` — コアライブラリ (net9.0, nullable enable)。`Signal<T>`、`Context`、`Effect` の 3 ファイルで構成される。
- `SolidCS.Tests/` — xUnit テスト (net9.0)。`SolidCS` を ProjectReference。
- `examples/Counter/` — Avalonia 11.3.2 製サンプル (`PublishAot=true`, `WinExe`)。`SolidCS` の使い方のリファレンス実装。
- `SolidCS.sln` — 上記 3 プロジェクトを束ねる。`examples` は solution folder 配下。

## よく使うコマンド

すべてリポジトリルートから実行する想定:

```pwsh
dotnet build SolidCS.sln                                   # ソリューション全体をビルド
dotnet test SolidCS.Tests                                  # 全テスト実行
dotnet test SolidCS.Tests --filter "FullyQualifiedName~SignalTests.SubscribeTest"   # 単一テスト
dotnet run --project examples/Counter                      # Avalonia サンプル起動
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

### Avalonia 連携の流れ (Counter サンプル)

`examples/Counter/Program.cs` には 2 種類のコンポーネント定義スタイルが同居している:

- **クラス継承スタイル** (`class Counter : Component`) — 従来型。コンストラクタで子要素を組み立てる。
- **関数ファクトリスタイル** (`static class Counter2 { static Component New() { ... } }`, コミット `d76f627`) — `Component` を返す static メソッドで宣言する SolidJS 風。`AppMain` ではこちらを使用中 (旧スタイルはコメントアウトで残してある)。

リアクティブな UI 更新は `SLabel` のコンストラクタが `Context.Instance.RegisterEffect(() => _label.Content = ContentFunc())` を呼ぶことで実現している — effect 内で `count.Value` を読むので、count が変わるたびに同じラムダが再実行され Label が更新される。新しいリアクティブ UI コントロールを足すときは、このパターン (コンストラクタで `RegisterEffect` を呼んで自身のプロパティを Signal から計算する) を踏襲する。

## 既知のゴミ

- `SolidCS/Class1.cs` と `SolidCS.Tests/UnitTest1.cs` はテンプレートの残骸。触らなくてよいが、新規ファイル追加のサンプルとして参照しないこと。

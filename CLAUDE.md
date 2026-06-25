# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

SolidJS 風の Signal / Effect ベースのリアクティブプリミティブを C# で実装した実験プロジェクト。`SolidCS` 本体は UI フレームワーク非依存で、`examples/Counter` で Avalonia と組み合わせて使う方法を示している。

## ソリューション構成

- `SolidCS/` — コアライブラリ (net9.0, nullable enable)。`Signal<T>` と `Context` のみで構成される。
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

リアクティビティは `SolidCS/Signal.cs` と `SolidCS/Context.cs` の 2 ファイルだけで成立しており、依存関係はないので両方読めば全体を把握できる。要点は以下:

- `Context` はプロセス全体で 1 つの **singleton** (`Context.Instance`)。`Stack<Action> Subscribers` に「現在登録中の effect」を積み、`Signal<T>.Value` の getter がそのスタックの先頭を覗いて自分の購読者リストに追加する。これが依存追跡の本体。
- `Context.RegisterEffect(Action)` は `Subscribers.Push → 実行 → Pop` の 1 セット。effect 内で読まれた Signal だけが追跡される。
- `Signal<T>.Value` の getter は `try/catch` で例外を握りつぶす。これは「Subscribers スタックが空 (= effect 外で読まれている)」場合に `Peek()` が投げるのを許容する意図。`catch` を消すと壊れる。
- `Signal<T>.Value` の setter は `EqualityComparer<T>.Default` で前値と一致した場合は通知をスキップする。

### Context.Lock を必ず取るという規約

`Context.Instance.Lock` が公開されているのは、`Subscribers` スタックがシングルトンの可変状態なので、UI 構築や effect 登録のような **Subscribers を Push/Pop する処理は外側で `lock(Context.Instance.Lock)` する** という前提があるため (コミット `cea228e` で導入)。これは Signal.cs 内のコメント (#16–18) で明文化されている:

> UI 構築処理全体を Lock する。UI スレッドはシングル前提で良いらしいのでこのままで。

破ると複数スレッドからの effect 登録時に Subscribers スタックが壊れる。テスト (`SolidCS.Tests/SignalTests.cs`, `ContextTests.cs`) も `examples/Counter/Program.cs` の `AppMain` も、`Subscribers` に触る前に必ず `lock(ctx.Lock) { ... }` で囲んでいる — 同じパターンを踏襲すること。

### Avalonia 連携の流れ (Counter サンプル)

`examples/Counter/Program.cs` には 2 種類のコンポーネント定義スタイルが同居している:

- **クラス継承スタイル** (`class Counter : Component`) — 従来型。コンストラクタで子要素を組み立てる。
- **関数ファクトリスタイル** (`static class Counter2 { static Component New() { ... } }`, コミット `d76f627`) — `Component` を返す static メソッドで宣言する SolidJS 風。`AppMain` ではこちらを使用中 (旧スタイルはコメントアウトで残してある)。

リアクティブな UI 更新は `SLabel` のコンストラクタが `Context.Instance.RegisterEffect(() => _label.Content = ContentFunc())` を呼ぶことで実現している — effect 内で `count.Value` を読むので、count が変わるたびに同じラムダが再実行され Label が更新される。新しいリアクティブ UI コントロールを足すときは、このパターン (コンストラクタで `RegisterEffect` を呼んで自身のプロパティを Signal から計算する) を踏襲する。

## 既知のゴミ

- `SolidCS/Class1.cs` と `SolidCS.Tests/UnitTest1.cs` はテンプレートの残骸。触らなくてよいが、新規ファイル追加のサンプルとして参照しないこと。

# 動的 children (For / Show) 設計案

Status: **ドラフト** (2026-07-10 時点の検討まとめ。未決事項あり)

SolidJS の `<For>` / `<Show>` 相当を SolidCS.Avalonia に導入するための仕様案。
現状のコンテナ (StackPanel / Grid 等) は構築時に子が固定で、「Signal の変化で
子が増減する」手段がない。これはリアクティブ UI フレームワークとしての本丸機能。

## API 案 (全体像)

```csharp
var todos = new Signal<IReadOnlyList<Todo>>([]);
var showDone = new Signal<bool>(true);

new StackPanel
{
    // For: リストの各要素を Component に写像。リスト Signal の変化で子が増減する
    For.Each(() => todos.Value, todo => new TodoRow(todo)),

    // Show: 条件付き表示。when が偽なら fallback (省略可)
    new Show(() => showDone.Value,
        () => new Label(() => $"done: {todos.Value.Count(t => t.Done.Value)}")),
};

// 更新は「新しいリストを代入」(イミュータブル更新規約)
todos.Value = [.. todos.Value, newTodo];
```

## 論点と方針

### 1. 過追跡 (over-tracking) — コアに Untrack を追加する【必須・先行実装可】

For の effect 内で子 Component を構築すると、子のコンストラクタで読まれた
Signal が For の effect にも購読される (Subscribers スタックの先頭が For の
effect のため)。放置するとアイテム内部の Signal の変化 1 つでリスト全体が
再構築される。

**方針**: SolidCS コアの `Context` に `Untrack(Action)` / `Untrack<T>(Func<T>)`
を追加する。実装は「Subscribers スタックに番兵 (null 相当) を積んで実行し、
finally で降ろす」または「スタックを一時退避して空にする」。For / Show の
子構築はすべて `Untrack` の中で行う。コア変更なので SolidCS.Tests で単体
テスト可能 — 他の論点に先行して実装してよい。

### 2. リスト変更の表現 — Signal<IReadOnlyList<T>> + イミュータブル更新規約

`Signal<List<T>>` の in-place 変更 (`list.Add(...)`) は setter の同値チェック
(参照比較) を素通りして **通知が飛ばない**。

**方針 (a 案・採用)**: `Signal<IReadOnlyList<T>>` を使い、更新は毎回新しい
リストを代入する規約とする (`todos.Value = [.. todos.Value, item]`)。
SolidJS のイミュータブル更新と同じ思想で、コア変更が不要。

**却下 (b 案)**: 増減イベントを細粒度発行する専用 `SignalList<T>` の新設。
効率は良いが設計が重い。差分更新 (論点 3 の keyed diff) が必要になった
ときの将来課題として保留。

### 3. 再構築戦略 — v1 は全破棄、API に key の口だけ確保

リスト変化時に全子を Dispose して作り直すか、キー対応付けで生き残りを
再利用するか。全破棄は単純だが **アイテム内の状態 (TextBox の入力途中・
フォーカス・アイテム内 Signal) が毎回消える**。

**方針**: v1 は全破棄で実装する。ただし API には最初から
`Func<T, object>? key = null` を用意しておき、v2 で keyed diff (key 一致の
アイテムは Component を再利用、並べ替えは Children の移動で対応) に差し
替えられるようにする。シグネチャ変更なしで内部だけ進化させる。

### 4. For の実体 — 自前パネル方式で割り切る

Component : Control が 1:1 のモデルなので、For 自身が子を並べるパネル
(内部 StackPanel、`Orientation` オプション引数) を持つのが素直。

**方針**: v1 は「For = パネル」で割り切る。親コンテナが For の子を自分の
Children に splice する方式 (SolidJS に忠実) は、全コンテナにインデックス
管理が入り複雑さが跳ねるため採らない。制約として「Grid のセル 1 個ずつに
For の子を直接配置する」ことはできない (For ごと 1 セルに入る) — 必要に
なったら再検討。

### 5. Show の生存戦略 — unmount をデフォルト、keepAlive オプション

条件が偽になったとき子をどうするか:

- **unmount (デフォルト)**: Dispose して破棄。SolidJS の Show と同じ。
  effect が確実に解放され、再表示時は状態リセット。
- **keepAlive (オプション)**: 構築済みの子を保持し `IsVisible` だけ切り替え。
  状態保持・再表示が高速。`new Show(when, child, fallback, keepAlive: true)`。

実装は ContentControl の Content 差し替え。fallback は `Func<Component>?`
(省略時は空)。

### 6. API シェイプ — static ファクトリで型推論を効かせる

C# はコンストラクタでジェネリック型推論が効かない
(`new For<Todo>(...)` と書かされる) ため、For は static ファクトリにする:

```csharp
public static class For
{
    public static ForComponent<T> Each<T>(
        Func<IReadOnlyList<T>> items,
        Func<T, Component> render,
        Func<T, object>? key = null,          // v1 では未使用 (口だけ確保)
        Orientation orientation = Orientation.Vertical);
}
```

Show は非ジェネリックなので `new Show(when, child, fallback)` のまま。
命名は SolidJS に合わせて `For` / `Show` とする (`Each` / `When` 案は却下)。

### 7. 通知中の再入・Dispose 済み effect への通知【要テスト】

For の再構築は Signal 通知の最中に「子の Dispose (購読解除) + 新規構築
(購読追加)」を行う。`Signal` は通知前に `_subscribers.ToArray()` で
スナップショットを取るため列挙中変更では死なないが、**同一通知パス内で
Dispose された effect に後から通知が届くケース**が起こりうる。
`Effect._disposed` ガードでノーオペになるはず — この挙動をコアのテストで
固定する (リスト Signal とアイテム Signal を同時購読する effect のケース等)。

### 8. テストと example

- コア追加分 (`Untrack`、論点 7 の再入ケース) は SolidCS.Tests でユニット
  テストを書く。
- For / Show 本体は Avalonia 層。v1 は手動 GUI 確認で済ませ、必要になったら
  `Avalonia.Headless` のテストプロジェクト導入を検討する。
- example は **`examples/Todo`** を新設。TODO リスト (追加・削除・完了チェック・
  完了数の Show) で For + Show + 状態保持の論点が全部露出する。
  論点 3 の「全破棄で TextBox の状態が消える」制約もこのデモで観察できる。

## 実装ステップ案

1. コア: `Context.Untrack` + テスト (論点 1)
2. コア: 再入・Dispose 済み通知のテスト固め (論点 7)
3. Avalonia: `Show` (unmount デフォルト + keepAlive) — For より単純で先に出せる
4. Avalonia: `For.Each` (全破棄方式、key 引数は口だけ)
5. example: `examples/Todo` 新設 + GUI 確認
6. 将来: keyed diff (論点 3 v2)、SignalList (論点 2 b 案)、splice 方式 (論点 4)

## 未決事項

- `Show` の fallback 引数の形 (`Func<Component>?` で確定でよいか)
- For の内部パネルに WrapPanel / Grid を選べるようにするか (v1 は StackPanel 固定でよい?)
- `Untrack` の公開範囲 (ライブラリユーザーにも公開するか、internal に留めるか)

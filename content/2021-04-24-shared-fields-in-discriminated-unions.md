---
title: 判別共用体が共通フィールドを持てたら嬉しい
type: "post"
date: 2021-04-24
url: 2021-04-24/shared-fields-in-discriminated-unions
tags:
  - エッセイ
  - 言語設計
  - FSharp
  - TypeScript
---

判別共用体のすべてのバリアントに同じデータを持たせたいことがあるが、F# だとめんどい。
TypeScriptだと楽にできて嬉しい。
判別共用体はすでに共通フィールドを持っているので、追加で持たせるのは問題ないはず。
おそらく構文上の課題がある。
何にせよ実用的なので、もっと実装されてほしい。

<!--more-->

## 共通フィールドの例

よくあるのが構文木のフィールドに位置情報をもたせること。
エラーが起こった場所を報告するときに使う。

TypeScriptだと簡単にできて、次のように書く。

```ts
/** ソースファイルの位置情報 */
type Location = {
    file: string
    row: number
    column: number
}

/** 式を表す構文木のノード */
type Expr = {
    location: Location
} & ({
    kind: "number"
    value: number
} | {
    kind: "add"
    left: Expr
    right: Expr
})
```

F# だと「すべてのバリアントに持たせる」か「タプルで外側に持つ」かのどちらかを選ぶことになる。
「すべてのバリアントに持たせる」には次のように書く。

```fsharp
type Location =
  { File: string
    Row: int
    Column: int }

type Expr =
  | NumberExpr of int * Location
  | AddExpr of Expr * Expr * Location
```

`Expr` 型の値から位置情報を取るには、こういう補助関数を使う。
(メタプログラミングやコードアクションで自動化できる可能性はある。)

```fsharp
let exprToLocation (expr: Expr): Location =
  match expr with
  | NumberExpr (_, location)
  | AddExpr (_, _, location) -> location
```

「タプルで外側に持つ」ときは次のように書く。
位置情報なしの `ExprT` と位置情報付きの `Expr` という2つの型がある。

```fsharp
type Location = (* 略 *)

type ExprT = // Exprではない
  | NumberExpr of int
  | AddExpr of Expr * Expr  // 部分式はExpr

type Expr = ExprT * Location
```

位置情報を取るのが簡単にできて嬉しい。

```fsharp
let exprToLocation (_, location) = location
```

こっちのほうがいいようにみえるが、`Expr` の値を構築するときにごちゃごちゃする。
感覚的な話だから説明はできないけど。

```fsharp
    AddExpr ((NumberExpr l, lLoc), (NumberExpr r, rLoc)), addLoc
```

## 判別共用体のdiscriminantはすでに共通フィールド

判別共用体の値の実行時の表現を考える。
判別共用体はすでに「共通のフィールド」として、その値が「どのバリアント」かを示す値 (discriminant) を持っている、ということがよくある。
判別共用体に追加の共通フィールドを持たせる場合、それと同じように実現できると思う。

例えばメモリレイアウトの一例をC言語で書くとこんな感じ。

```c
struct NumberExpr {
    int value;
    struct Location location;
};

struct AddExpr {
    struct Expr *left;
    struct Expr *right;
    struct Location location;
};

struct Expr {
    int discriminant;

    union {
        struct NumberExpr number;
        struct AddExpr add;
    };
};
```

`struct Expr` のフィールドとして共通フィールドを足せばいい。

## 共通フィールドの具象構文

型の宣言時に共通フィールドをどう書くか、という具象構文の問題がある。
TypeScriptでは交差型 (`&`) を使っているが、オブジェクトの交差型を備える言語は多くない。

機能的には判別共用体とレコードの交差なので、レコードの宣言時の構文と類似性を持っていてほしい。
また、共通フィールドもバリアントの中身の一部なので、そのように感じられる構文であってほしい。
例えば F# だとこんな感じ？

```fsharp
type Expr =
  { Location: Location }
  | NumberExpr of int
  | AddExpr of Expr * Expr
```

バリアントの構築時は、共通フィールドとバリアント固有のフィールドを混ぜて書けるほうが嬉しい。
例えば F# だとこんな感じで、オブジェクトをインスタンス化するときのロパティ初期化子の構文を真似るとよさそう。

```fsharp
    NumberExpr(n, Location = loc)
```

分解時も同様。

```fsharp
    match expr with
    | NumberExpr (n, Location = loc) ->
        n, loc
```

また、レコードと同様にドット記法でもアクセスできるはず。

```fsharp
let exprToLocation (expr: Expr): Location =
    expr.Location
```

## 共通フィールドを持つ判別共用体の型つけ

構文をいじった場合、型システムも変更が必要かもしれない。
F# は判別共用体がnominalで、列多相的なもの (SRTP) も元から自由ではないので、問題なさそう。
「共有フィールドつきの多相バリアント」とかの型推論は諦めたほうがよさそう。

## 結論

共通フィールドの利用頻度のわりには言語の拡張の量が多い。
TypeScriptのように、交差型の副産物ぐらいの立ち位置で妥当かもしれない。

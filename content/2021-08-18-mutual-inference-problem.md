---
title: "相互再帰関数の型推論で問題になったケース"
type: "post"
date: 2021-08-19
url: 2021-08-19/mutual-inference-problem
tags:
  - FSharp
  - MiloneLang
---

ミローネ言語 (F# のサブセットである自作言語) の型推論が壊れている。
どういうケースで壊れているかを書いておく。

<!--more-->

## 相互再帰

`module rec` 直下の関数は相互再帰的に定義される。つまり上にある関数の本体から、下にある関数を呼べる。

```fsharp
module rec MyProject.Example

let even x = x = 0 || odd (x - 1) // 定義前にoddを使う
let odd x = even (x - 1)
```

型推論により `even: int -> int`, `odd: int -> int` と型がつく。
以下 `module rec` は省く。

## 汎化

汎化 (generalization) がからむとややこしい。

### ジェネリック関数をインスタンス化して使うケース

```fsharp
module rec MyProject.Utils

let fail context = // context: 失敗した原因とか、何らかのデータ
  eprintfn "error: %s" (string (box context))
  exit 1

let div x y =
  if y = 0
  then fail ("div by zero; x=", x)
  else x / y
```

`fail` 関数は何らかのデータを受け取ってエラーに書き出し、異常終了する。
引数の型も結果の型も何でもいい。
つまり、理想的には `fail<'T, 'U> : 'T -> 'U` という型がつく。

その下にdiv関数の定義があって、本体で `fail` を呼んでいる。
divは事前条件をみたさないときに `fail` を呼んで、理由らしきものを書き込んで終了する。
この呼び出しにおける `fail` の型は `fail: string -> int` になっている。

divにおけるfailの使用箇所のせいで、failの定義の型が `string -> int` に単一化されてしまうと困る。
実際、 F# はこの状況でもfailを適切に汎化する。

### 関数の型が後から決まるケース

一方、関数の定義時に汎化を行うと問題が起こるケースがある。

```fsharp
type Pat =
  | WildcardPat

type Expr =
  | IntExpr
  | FunExpr of Pat List * Expr

let checkExprList ctx exprs = List.fold ctx checkExpr

let checkFunExpr ctx (args: Pat list) body =
  let ctx = checkExprList ctx args // バグ
  checkExpr ctx body

let checkExpr ctx expr : Ctx =
  match expr with
  | IntExpr -> ctx
  | FunExpr (args, body) -> checkFunExpr ctx args body
```

checkExprList関数は、式のリストを受け取って順番に型検査を行う。
個別の式の型検査は後方に定義されているcheckExprが行う。

checkExprListの定義の時点では、checkExprの型はまだ推論されていないので分からない。
引数exprsの型は何らかのリストであることだけ分かる。
その後、checkExprの型検査の結果として `checkExpr: Ctx -> Expr -> Ctx` という具体的な型が推論される。
このことから、checkExprListが受け取るのは式のリストに限られることが決まる。

問題は、checkExprListとcheckExprの間にcheckExprListが使用されていること。
checkExprListの型はまだ分からないが、実際にはジェネリック関数ではないので、リストの要素の型をインスタンス化してはいけない。
上の例ではcheckExprListに、式のリストではなくパターンのリストを誤って渡している。
これは型エラーにしなければいけない。

### 問題

`fail` の例では、`fail` の後にある関数の使用箇所において `fail` をジェネリック関数とみなしてインスタンス化する必要があった。
一方 `checkExprList` の例では、`checkExprList` の使用箇所において `checkExprList` はジェネリック関数でないとみなして、インスタンス化しない必要があった。

### 検索

"hindley milner mutually recursive" とかで検索するとこの記事 (回答) が出てきた。

[recursion - Why are functions in OCaml/F# not recursive by default? - Stack Overflow](https://stackoverflow.com/questions/900585/why-are-functions-in-ocaml-f-not-recursive-by-default/904715#904715) (リンク先の回答の投稿日は2009-05-24)

> (snip) One possibility is to simply do a dependency analysis on all the definitions in a scope, and reorder them into the smallest possible groups. (snip)

(スコープ内にある定義の間の依存関係を解析して最小のグループに並び替えるという方法が考えられる。)

上の例に関していえば:

- failは下にある関数を呼び出していないので、定義をみた時点で汎化できる。
- checkExprListは、下にあるcheckExprの型に依存しているので、それの型推論が終わるまで汎化しない。

と考えればよいかもしれない。

ルールとしては「関数の本体で参照しているすべての関数の定義をみた後に汎化する」といえる。

(F# のコンパイラが実際にどうやっているかみたほうがいいかもしれない。)

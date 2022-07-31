---
title: 可視性を使ってnever型もどきを作る
type: "post"
date: 2021-11-03
url: 2021-11-03/poor-mans-never-type
tags:
  - FSharp
---

never型がない言語でも、可視性の制御を使うことでそれっぽいことができる。

<!--more-->

## 前提

F# において制御を返さない関数は、返り値の型が任意であるような多相関数として表現される。

```fsharp
/// 常に例外を投げる関数
val failwith<'A> : string -> 'A
```

F# の型システムにおいて多相になれるのはnominalな型や関数だけなので、制御を返さない関数を持つフィールドというのは作れない。

```fsharp
type R = { FailWith: string -> (* never型? *) }
```

型抽象(Λ)があったらできた。

```fsharp
// F# ではない。
type R = { FailWith: Λ'A. string -> 'A }
```

なお、型変数をレコード型のほうに持たせると意味が変わってくる。

```fsharp
type R<'A> = { FailWith: string -> 'A }
```

これだと `FailWith` を複数回、異なる返り値型で呼ぶことができない。(Rの型引数が矛盾する。)

## 方法

はじめに適当にNever型を定義する。型は公開するが、型のインスタンスを作る方法は公開しないでおく。

```fsharp
type Never = private | Never
```

この型の値を受け取るnever関数を公開する。

```fsharp
let never<'A> (_: Never) : 'A =
  assert false      // この関数が呼ばれないことをコンパイラに教える
  failwith "never"  // 制御を返さない何らかの式
```

## 使いみち

例えば別のプログラムを実行する `exec` 関数を抽象的に受け取りたいとする。

```fsharp
type PlatformApi = { Exec: string list -> Never }

let run (api: PlatformApi) command : unit =
  match command with
  | Echo msg -> printfn "%s" msg

  | Exec c ->
    api.Exec [ "/bin/sh"; "-c"; c ]
    |> never
//     ^ これ
```

定義する側はいつもどおり。

```fsharp
    let doExec args : int = (* exec関数を呼ぶ *)

    let myExec<'A> (args: string list) : 'A =
        let code = doExec args
        assert (code <> 0)

        // execが失敗したので例外を投げる。
        failwithf "exec failed: %d" code

    let api : PlatformApi = { Exec = myExec }
```

## 抜けみち

リフレクションでインスタンスを作れたらダメ。それを考慮するなら、Never型はコンパイラ側で提供して、リフレクションによるインスタンス生成を禁止する。

----

## 余談: バリアントのない判別共用体

Rustではバリアントのないenum型 (F# でいうところの判別共用体) を作れる。
これは値を持たないので、never型と同等に振る舞う。

- [Infallible in std::convert - Rust](https://doc.rust-lang.org/std/convert/enum.Infallible.html) (never型の代わりに使えるエラー専用の型。バリアントのない判別共用体で定義されている。)
- [never - Rust](https://doc.rust-lang.org/std/primitive.never.html) (`!` と書いてnever型。まだ正式リリースされていない。)

## 余談: 多相でない言語におけるnever

上述の `Never` 型と `never` 関数を言語側に組み込めば、多相関数の概念を持たない言語でもnever型と同様のことができる。
[式指向構文が言語処理系にもたらす複雑性](/blog/2020-09-19/complexity-from-expression-oriented-syntax)に書いた課題をいくらか軽減できるかもしれない。
多相関数のない言語を設計する予定はないので深堀りはしない。

---
title: '[F#][小ネタ] Unreachableアクティブパターン'
tags:
  - FSharp
  - Tips
type: "post"
date: 2022-10-21
url: 2022-10-21/unreachable-active-pattern
---

matchのネストを減らす小ネタ

<!--more-->

まれに判別共用体の値がどのケースか分かっていることがある。
一般論としてはケースが絞り込まれる場所であらかじめケースを剥がしておくほうがよい。

しかしどうしても剥がしたいことはある。
その場合はmatchを使い、その他のパターンを例外送出でごまかす。

```fsharp
type MyValue =
    | Int of int
    | String of string

let onInt (value: MyValue) =
    match value with
    | Int n ->
        doSomething n

    | _ -> failwith "unreachable"
```

この状況で使える小ネタとして、以下のヘルパーを用意しておくと記述量が減る:

```fsharp
let inline private (|Unreachable|) (_: 'T) : 'U = failwith "unreachable"

let onInt (value: MyValue) =
    let (Int n | Unreachable n) = value
    doSomething n
```

ここで `Unreachable` というアクティブパターンを定義している。
必ずマッチに成功するので、`let` の左辺で使っても「パターンが網羅的でない」という警告が出ない。
実行時の挙動は前述の `match` と同じ。

----

## Appendix. サンプルコード

[`try.fsharp.org` のreplで見る](https://try.fsharp.org/#?code=PQgEBEEsDcFMCcDmkB2jQEMDGAXGtQAHDHHBFAOgFgAoAG1h1FTtQMPhhIIAoAfAKop4sbAAsMAIwZ8AlKB4B9AFygA5ABV5qtQNABeUADMMkOgHdIOMaABEAV2GisE6bFu1aOAJ6ECAWW8ANQw6ewJ9WlBo0D5QAEkUJgB7I2YkqJi4gGUcTjRQVNAAZzzURE96RlAAE2Ts5IBbRjFyhRRVVBx5Qw4uoxQ7LtAAWgA+UABSGttQFEqwEaXlldowRpIXNeBRlb3KvqSBu3tigg2cF2UPGloGFJREnABGBWhQ8NVAkLDYHszohcXKB3r9QJZrADYgkknNRmMoTFavUmi02vNbjQkXFFPDjKYLFYbA4nOIpAwbrRko8kq8eE9QM95CB9BNhuNGZU8t4odSnnTcvl0LZbLIoRhimd4EwTHQzrQITZcRzDjhjrZSkK8fBTGcZgsdnt9jQwPdtrsjUsDvk1YMHGdQPdrpV7oUaTgAExvD6wL7BH3-LExV302GDOJCERktxzHogn1QuoNZrWdGVPlJL2hpge5nAVnpJgcj1c+A8oPRDOehSCtoisUVzCShAy0LymiK0DKiaq9WatocnWQPU3IA&html=DwCwLgtgNgfAsAKAAQqaApgQwCb2ag4CdMTJcMABwFp0BHAVwEsA3AXgCIBhAewDsw6AdQAqAT0roOSAMb9BAzoIAeYAPThoAbhkhMAJwDOJNgzAAzagA4OeQhqy5EhAEY9sYu6mBq3HvD6asEA&css=Q)

```fsharp
/// Diverging active pattern.
let inline private (|Unreachable|) (_: 'T) : 'U = failwith "unreachable"

type MyValue =
    | Int of int
    | String of string

let doSomething (n: int) = printfn "int -> %d" n

// ----------
// match
// ----------

printfn "use match:"

let onInt1 (value: MyValue) =
    match value with
    | Int n ->
        doSomething n

    | _ -> failwith "unreachable"

onInt1 (Int 1) //=> int -> 1

try
    onInt1 (String "")
    assert false
with _ -> printfn "string -> raised"

// ----------
// let
// ----------

printfn "use let:"

let onInt2 (value: MyValue) =
    let (Int n | Unreachable n) = value
    doSomething n

onInt2 (Int 2) //=> int -> 2

try
    onInt2 (String "")
    assert false
with _ -> printfn "string -> raised"

```

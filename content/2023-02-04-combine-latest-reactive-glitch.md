---
title: 'CombineLatestのリアクティブグリッチ'
tags:
  - FSharp
  - Reactive
type: "post"
date: 2023-02-04
url: 2023-02-04/combine-latest-reactive-glitch
---

Rx.NETのCombineLatestオペレーターによってリアクティブグリッチ (一時的な非一貫性) が生じる例

<!--more-->

この記事の目的: Reactive Glitch という概念を知ろう

## 環境構築

(.NET 7.0.102, Ubuntu 20.04)

プロジェクトを作る:

```sh
dotnet new console -lang F# -o RxGlitch

cd RxGlitch

# (Rx.NET)
dotnet add package System.Reactive --version 5.0.0
```

コードを書く:

```fsharp
// file: Program.fs
module Program

open System
open System.Reactive
open System.Reactive.Linq

let _ =
  let s = new Subjects.BehaviorSubject<_>(0)
  let u = s.Select(fun x -> x + 1)
  let v = s.Select(fun x -> x + 2)
  let w = u.CombineLatest(v)

  printfn "s = 0"
  w.Subscribe(fun (u, v) ->
    printfn "(%d, %d)" u v
  ) |> ignore

  printfn "s <- 2"
  s.OnNext(2)
```

実行:

```sh
dotnet run
```

出力:

```
s = 0
(1, 2)
s <- 2
(3, 2)
(3, 4)
```

## 構造

まず定義されるストリーム (observable) を再掲する:

```fsharp
  let s = new Subjects.BehaviorSubject<_>(0)
  let u = s.Select(fun x -> x + 1)
  let v = s.Select(fun x -> x + 2)
  let w = u.CombineLatest(v)
```

- `s` は初期値0で、後で2に切り替えることで全体のストリームに変化を起こす
- `u` は `s + 1` の値を持つストリームとして定義される
- `v` は `s + 2` として定義される
- `w` は `u`, `v` を合流させて得られるストリームである
    - 一見、`w` は (`s + 1, s + 2`) を値に持つような気がするが、実際にはそうならない

ストリームの流れは以下のような構造になっている:

```
    s +-----> u ------+--> w
       \             /
        +---> v ----+
```

この状態で `w` の値を `Subscribe` で購読し、標準出力にログとして書き出す

## 挙動

内部的な挙動について:

- ストリーム:
    - ストリームは内部的に購読者リスト (コールバックの配列) を持っている
        - 「上流のストリームを購読する」 = ストリームの購読者リストにコールバックを追加する
        - 「下流のストリームに値を流す」 = 自身の購読者リストにあるコールバック関数をすべて呼ぶ
    - オペレーターが上流のストリームを購読するタイミングは種類による
        - Select, CombineLatestはどちらも「自身が購読されたとき」に上流を購読する
        - ↑ この性質を *Cold* という
        - Coldなストリームを組み合わせると最後のストリームを購読したタイミングで下から上に連鎖的に購読が起こる
- 購読:
    - `w.Subscribe()` のとき、`w` は `u`, `v` 両方を購読する
    - (CombineLatest): 流れてきた新しい値と、他方のストリームの最後の値を組み合わせて、下流のストリームにペアを流す
    - `u`, `v` がそれぞれ `s` を購読する
    - (Select): 流れてきた新しい値を使って計算を行い、下流のストリームに新しい値を流す
    - `s` は購読された瞬間に値を流すので、`u` → `w` に値が流れて、`w` にとってuの最後の値が決まる (u=1)
    - また `v` → `w` にも値が流れて (v=w)、`w` から `1, 2` が流れる
- 変更:
    - `s` の値を変更する (1 → 2)
    - `s` → `u` → `w` に新しい値 (3) が流れる
        - この時点で、`w` にとって v=2 が最後の値なので `3, 2` が出力される
    - `s` → `v` → `w` に新しい値 (4) が流れる
        - これは `3, 4` になる

## 誤解

ここで構築したストリームは一見、数学的な「従属変数」を定義したようにみえるが、実際はそうでない

CombineLatestは「両方のストリームの最新の値の組み合わせ」ではなく「**片方の** ストリームの最新の値と他方の過去の値の組み合わせ」である。
前述の挙動はオペレーターの仕様から定まる通りだが、気づきづらいと思うので注意しよう

## グリッチ

前述のように最新の値と過去の値の組み合わせが観測される挙動は **リアクティブグリッチ** (Reactive Glitch) と呼ばれる。
筆者の経験でいえば、最新の値の組み合わせ同士では発生しえない組み合わせにより、たまにアサーションが破られることがあった

## 別案: Flux

状態を持つ `s` と導出されたストリームを分けないことでグリッチを除去できる。
`s` の値から導出される値をすべて計算した値を状態として持つ

```fsharp
// file: Program.fs
module Program

open System
open System.Reactive
open System.Reactive.Linq

type State =
  private
    { S: int
      // U = S + 1
      U: int
      // V = S + 2
      V: int
      // W = (S + 1, S + 2)
      W: int * int }

let newState (s: int) : State =
  let u = s + 1
  let v = s + 2

  { S = s
    U = u
    V = v
    W = u, v }

let _ =
  let s = new Subjects.BehaviorSubject<_>(newState 0)
  // let u = s.Select(fun s -> s.U)
  // let v = s.Select(fun s -> s.V)
  let w = s.Select(fun s -> s.W)

  printfn "s = 0"
  w.Subscribe(fun (u, v) ->
    printfn "(%d, %d)" u v
  ) |> ignore

  printfn "s <- 2"
  s.OnNext(newState 2)
```

`s` に新しい状態を送り込む代わりに変更を送り込み、新しい値を導出させるようにすれば「Flux」と呼ばれる構造になる

注意点は導出されたストリームが異なるコンポーネントに含まれていることもあるということ。
関心の分離に反することになったり、いずれ `State` が巨大化して手に負えなくなるかもしれない

参考:

- [なぜ MVVM + FRP は Elm Architecture に勝てないのか - dely Tech Blog](https://tech.dely.jp/entry/2020/12/25/102838) (2020) (RxSwiftを使って同様のグリッチについて解説し、Elmアーキテクチャと比較している)

## 別案: トポロジカルソート

ライブラリのレベルでは、ストリームの依存関係で更新順序を整列させるという案もあるらしい。
(詳しくは見ていないが一応言及しておく)

参考:

- [Deprecating the Observer Pattern](https://infoscience.epfl.ch/record/148043?ln=en) (2010) (Observerの代案としてリアクティブプログラミングを紹介するというペーパー)
- [Deprecating the Observer Pattern with Scala.React](https://infoscience.epfl.ch/record/176887?ln=en)(2012) (依存関係を動的に解決する機能を持つリアクティブプログラミングのライブラリを紹介するというペーパー)

## おまけ: 非同期によるグリッチ

非同期的なオペレーターが間に挟まることで、ストリームが構成する全体としての一貫性が保たれないという、別の種類のグリッチもある。
すなわち、上流の値に対して何らかの非同期処理を行い、下流のストリームに値を流す場合、その処理中は下流のストリームが過去の状態のままになってしまうということ

次の記事は本稿とは趣旨が異なるかもしれないが、全体としての非一貫性に関する図解があって分かりやすいのでリンクしておきたい:

- [RecoilとRxJSってどう違うの？　共通点は？　調べてみました！](https://zenn.dev/uhyo/articles/recoil-vs-rxjs) (2023)

----

この記事に関連して何か知っていることがあったら教えてください。
(サンプルコードの提案、他の解決案、別種のリアクティブグリッチなど)

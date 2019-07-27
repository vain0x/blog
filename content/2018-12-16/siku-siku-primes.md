---
title: シクシク素数列を F# でやってみた
tags:
- F#
type: "post"
date: 2018-12-16 10:34:43
permalink: siku-siku-primes
---

シクシク素数列アドベントカレンダーという、単一の問題を毎日異なる言語で解く催しがあるみたいです。F# での参加者はいないみたいなので、カレンダー不参加ですがやってみます。

<!--more-->

[シクシク素数列 Advent Calendar 2018 - Qiita](https://qiita.com/advent-calendar/2018/4949prime-series)

## 問題

> - 数値に4か9を含む素数をシクシク素数と呼ぶことにします
>     - 19とか41とか149とか。
> - 標準入力として正の整数 N を与えたら N 番目までのシクシク素数を半角カンマ区切りで標準出力してください
>     - 例 N = 9 の場合、 19,29,41,43,47,59,79,89,97
> - N は最大で 100 とします

## 考察

10進数展開に4か9を含む整数を **シクシク数** と呼ぶことにします。こうするとシクシク素数の条件は

- シクシク数であること
- 素数であること

の2つに分割できて、シンプルに実装できそうです。

## 実装

なるべく読みやすい実装を心がけて書いていきましょう！

実装方法はたくさんありますが、今回はやんごとなき理由から seq を中心に使っています。

### 実装: 素数判定

はじめに素数判定の関数を作ります。よくあるやりかたですが、 2 以上 √n 以下の数で割ってみて、いずれも割り切れなければ素数、と判定します。

```fsharp
let isPrime n =
  // n が2未満のケースは除外
  if n < 2 then false else

  // √n の整数部分
  let r = n |> float |> sqrt |> int

  seq { 2..r } |> Seq.forall (fun m -> n % m <> 0)
```

`seq { 2..r }` は 2 から r 以下の数からなる数列を表しています。この中の値をそれぞれ m として、 `n % m <> 0` がすべてについて成り立てばOK。

`seq` と `Seq.` ですでにシクシク感が出ていますが、シクシク数の判定は次です。

### 実装: シクシク数の判定

シクシク数かどうかの判定では、 n の10進展開を計算します。割り算を使うことで n を1桁目とそれ以外の部分に分解できるというのがポイント。

- `n / 10` は、n から1桁目を取り除いたもの
- `n % 10` は n の1桁目

そのため `n / 10` を10進展開したものに `n % 10` を付け足せば、n の各桁の数からなる列を構成できます。

```fsharp
let rec digits n =
  seq {
    if n > 0 then
      yield! digits (n / 10)
      yield n % 10
  }
```

ここでの `seq { .. }` はブロック内で `yield` された値からなる列を表します。他の言語にあるリストの内包表記みたいなもの。`yield!` は複数の値を一挙に `yield` することを表しています。

シクシク数の判定は、これで得られた各桁の数のどれかが 4, 9 に等しいかどうかを判定すればOK。

```fsharp
let isSikuSiku n =
  digits n |> Seq.exists (fun d -> d = 4 || d = 9)
```

### 実装: シクシク素数列挙

これでシクシク素数判定関数のできあがり！

```fsharp
let isSikuSikuPrime n =
  isSikuSiku n && isPrime n
```

非負整数を列挙して、シクシク素数であるものに絞り込むことで、シクシク素数を昇順に列挙します。

```fsharp
let sikuSikuPrimes =
  seq {
    for n in 0..Int32.MaxValue do
      if isSikuSikuPrime n then
        yield n
  }
```

`seq { .. }` の中身は必要に応じて計算されるので、実際には全整数についてのループが回るわけではないので安心してください。

`Seq.take N` を使うと、列の前から N 個の要素が列挙された時点で計算を打ち切るようにできます。

```fsharp
sikuSikuPrimes |> Seq.take N
```

### 実装: main

最後に標準入出力を書きます。

```fsharp
open System

let N = Console.ReadLine() |> int

sikuSikuPrimes
|> Seq.take N
|> Seq.map string
|> String.concat ","
|> printfn "%s"
```

[.NET Fiddle で試す](https://dotnetfiddle.net/mIJ1qY)

## おまけ

```fsharp
open System

let isPrime n =
  if n < 2 then false else

  let r = n |> float |> sqrt |> int
  seq { 2..r } |> Seq.forall (fun m -> n % m <> 0)

let testPrime () =
  assert (isPrime 2)
  assert (isPrime 19)
  assert (isPrime 4 |> not)

let rec digits n =
  seq {
    if n > 0 then
      yield! digits (n / 10)
      yield n % 10
  }

let testDigits () =
  assert (digits 0 |> Seq.isEmpty)
  assert (digits 2019 |> Seq.toList = [2; 0; 1; 9])

testDigits ()

let isSikuSiku n =
  digits n |> Seq.exists (fun d -> d = 4 || d = 9)

let testIsSikuSiku () =
  assert (isSikuSiku 42)
  assert (isSikuSiku 2019)
  assert (isSikuSiku 12356780 |> not)

let isSikuSikuPrime n =
  isSikuSiku n && isPrime n

let sikuSikuPrimes =
  seq {
    for n in 0..Int32.MaxValue do
      if isSikuSikuPrime n then
        yield n
  }

let N = Console.ReadLine() |> int
sikuSikuPrimes
|> Seq.take N
|> Seq.map string
|> String.concat ","
|> printfn "%s"
```

出力例:

```
$ fsharpi ./Program.fsx
100
19,29,41,43,47,59,79,89,97,109,139,149,179,191,193,197,199,229,239,241,269,293,347,349,359,379,389,397,401,409,419,421,431,433,439,443,449,457,461,463,467,479,487,491,499,509,541,547,569,593,599,619,641,643,647,659,691,709,719,739,743,769,797,809,829,839,859,907,911,919,929,937,941,947,953,967,971,977,983,991,997,1009,1019,1039,1049,1069,1091,1093,1097,1109,1129,1193,1229,1249,1259,1279,1289,1291,1297,1319
```

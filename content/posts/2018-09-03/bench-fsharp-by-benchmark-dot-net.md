---
title: 'F#でベンチマークをとる'
tags:
  - 'F#'
  - チュートリアル
type: "post"
date: 2018-09-03 23:59:34
permalink: bench-fsharp-by-benchmark-dot-net
---


ベンチマークをとるのは難しい作業です。それらの作業を担う便利なライブラリーとして [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) があります。本稿では、これを使ってベンチマークをとる例をやっていき、 minimum viable introduction (実用最低限の導入) となることを目指します。

## 1. 要約

- 書き出し
    - ベンチマークは意外と大変という話について軽く触れる
- 導入
    - 本稿の構成
    - 開発環境の明記
    - BenchmarkDotNet のサンプル
- 例
    - 一例として「素数判定」を実装する
    - 素数判定の簡単なベンチマークを書く
    - ベンチマークを実行する
- おまけ
    - 軽くて速いベンチマークに設定する
    - ベンチマークを watch する

## 2. 開発環境

F# の開発環境は Getting Started を参照: [Ionide - Crossplatform F# Editor Tools](http://ionide.io/#getting-started)

今回は次を使ってやっていきます。 (執筆日: 2018年8月9日)

- Windows 10
- .NET Core Cli Tools 2.1
- F# 4.1
- Visual Studio Code
    - ionide-fsharp

## 3. BenchmarkDotNet にあるサンプルコード

F# 用のサンプルをみるとだいたいのイメージはつかめます。

[BenchmarkDotNet/samples/BenchmarkDotNet.Samples.FSharp at v0.11.0 · dotnet/BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet/tree/v0.11.0/samples/BenchmarkDotNet.Samples.FSharp)

## 4. サンプルプロジェクトでやってみる

例として「素数判定」のベンチマークをやってみましょう。

まずサンプルプロジェクトを普通のクラスライブラリーとして作ります。

```sh
dotnet new classlib -lang F# --name PrimeNum
```

そしてがんばって実装を書く:

```fsharp
// PrieNum/Library.fs
module PrimeNum

// 2 以上 p 未満の整数で割り切れなければ素数、と判定する。
// 最大 p - 2 回のループ
let isPrimeBruteForce (p: int): bool =
    if p < 2 then
        false
    else
        // go m ⇔ p が m 以上 p 未満の整数で割り切れない
        let rec go m = m >= p || p % m <> 0 && go (m + 1)
        go 2

// 2 以上 √p 以下の整数で割り切れなければ素数、と判定する。(証明略)
// 最大 √p - 1 回のループなので速いはず
let isPrime (p: int): bool =
    if p < 2 then
        false
    else
        // √p
        let r = p |> float |> sqrt |> int
        // go m ⇔ p が m 以上 r 以下の整数で割り切れない
        let rec go m = m > r || p % m <> 0 && go (m + 1)
        go 2
```

で、ベンチマークです。「実行するとベンチマーク処理を行うようなコンソールアプリ」を F# で作るという形になります。(ユニットテストでいうと expecto 方式)

コンソールアプリのプロジェクトを作って、そこに BenchmarkDotNet をインストールします。(最新バージョンは [NuGet] で確認しよう。)

[NuGet]: https://www.nuget.org/packages/BenchmarkDotNet/

```sh
# ベンチマークするためのプロジェクトはコンソールアプリとして作る。
dotnet new console -lang F# --name PrimeNumBench

# PrimeNumBench が PrimeNum を参照するようにする。
dotnet add PrimeNumBench reference PrimeNum

# BenchmarkDotNet をインストールする。 (※やや時間がかかる)
dotnet add PrimeNumBench package BenchmarkDotNet --version 0.11.0

# インテリセンスが効くように、ここで一度ビルドしておく (※やや時間がかかる)
dotnet build PrimeNumBench
```

試しに大きめの素数 10000019 (≒100万) が素数かどうかの判定にかかる時間を測定してみましょう。

- 測定したい計算をクラスのメソッドとして定義する。
    - モジュールではなく。このあたりは C#-er の気持ちになる。
- メソッドがベンチマーク対象であると分かるように `BenchmarkAttribute` をつける。
- `main` でベンチマークを実行する関数を呼ぶ。

```fsharp
// PrimeNumBench/Program.fs
module Program

open BenchmarkDotNet
open BenchmarkDotNet.Attributes

type Benchmarks() =
    [<Benchmark>]
    member this.IsPrimeBruteForceBench() =
        PrimeNum.isPrimeBruteForce 10_000_019

    [<Benchmark>]
    member this.IsPrimeBench() =
        PrimeNum.isPrime 10_000_019

[<EntryPoint>]
let main _ =
    let _summary = Running.BenchmarkRunner.Run<Benchmarks>()
    0
```

ベンチマークアプリを **Release モードでビルドして** 実行します。

```sh
dotnet run -p PrimeNumBench -c Release
```

1分ぐらいかかるので待つと、結果が *(ログの海に溺れて)* 出てきます。

```
                 Method |         Mean |         Error |        StdDev |
----------------------- |-------------:|--------------:|--------------:|
 IsPrimeBruteForceBench | 69,125.13 us | 1,118.0498 us | 1,045.8244 us |
           IsPrimeBench |     21.40 us |     0.1367 us |     0.1279 us |
```

平方根を取るだけでかなり良い最適化になってるっぽい。なお 1秒 = 1000 ms (ミリ秒) = 10万 us (マイクロ秒) です。

## 5. ベンチマークを軽量にする

試行錯誤している段階ではもうちょっと早く結果がほしいので、ベンチマークの設定を変えて計測精度を下げる代わりに、ベンチマークにかかる時間を短くしてみます。

ベンチマークをどのように実行するかの設定は Jobs にあるようです。参照: [Jobs | BenchmarkDotNet](https://benchmarkdotnet.org/articles/configs/jobs.html)

ウォームアップや反復の回数を固定するより、パラメーターをいじってアルゴリズムに任せたほうがいいらしいです。具体的にどうするか分からないので、デフォルトの設定でそういうのないかなと思ったんですが、よさげなプルリクが出ているので参考にします:

[Accuracy based job attributes by Zhentar · Pull Request #825 · dotnet/BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet/pull/825/files)

相対誤差の上限を増やせばよさそう。

```fsharp
// PrimeNumBench/Program.fs
module Program

open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs

type Benchmarks() =
    [<Benchmark>]
    member this.IsPrimeBruteForceBench() =
        PrimeNum.isPrimeBruteForce 10_000_019

    [<Benchmark>]
    member this.IsPrimeBench() =
        PrimeNum.isPrime 10_000_019

[<EntryPoint>]
let main _ =
    let config =
        let rough = AccuracyMode(MaxRelativeError = 0.1)
        let quickRoughJob = Job("QuickRough", rough, RunMode.Short)

        let c = ManualConfig()
        c.Add(quickRoughJob)

        // その他の設定をデフォルトから継承する。
        ManualConfig.Union(DefaultConfig.Instance, c)

    let _summary = Running.BenchmarkRunner.Run<Benchmarks>(config)
    0
```

ビルドしてからベンチマークの完了まで20秒ぐらいになりました。

## 6. ベンチマークを自動実行する

.NET Core 2.1 から標準入りした `dotnet-watch` ツールを使うと、ソースコードを更新するたびにベンチマークを自動実行できます。

```sh
dotnet watch -p ./PrimeNumBench -- run -c Release
```

## 7. レポートを公開する

結果を公開するには、 `BenchmarkDotNetArtifacts/results/*.md` にマークダウン(GFM)形式で出力されているのを貼っつける。環境の情報が自動で載るので楽です。(Qiita だと微妙に手直しが必要)

```
BenchmarkDotNet=v0.11.0, OS=Windows 10.0.16299.371 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i3-2310M CPU 2.10GHz (Sandy Bridge), 1 CPU, 4 logical and 2 physical cores
Frequency=2046136 Hz, Resolution=488.7261 ns, Timer=TSC
.NET Core SDK=2.1.302
  [Host]     : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT DEBUG
  DefaultJob : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT
```

|                 Method |         Mean |         Error |        StdDev |
|----------------------- |-------------:|--------------:|--------------:|
| IsPrimeBruteForceBench | 69,125.13 us | 1,118.0498 us | 1,045.8244 us |
|           IsPrimeBench |     21.40 us |     0.1367 us |     0.1279 us |

## 8. おわりに

よい計測ライフを！

## A. その他

- Params 属性に言及したほうがよかったかも

    [Parameterization | BenchmarkDotNet](https://benchmarkdotnet.org/articles/features/parameterization.html)

- Baesline 属性に言及したほうがよかったかも

    [Benchmark and Job Baselines | BenchmarkDotNet](https://benchmarkdotnet.org/articles/features/baselines.html)

- サンプルは速さが非自明なもののほうがよかったかも

    option vs voption とか

## B. 関連リンク

- [Showtime | BenchmarkDotNet](https://benchmarkdotnet.org/)

    公式サイト

- [BenchmarkDotNetを使ってみる｡ - Qiita](https://qiita.com/NetSeed/items/30d8a76163622a4b5be1)

    紹介記事 (C#)

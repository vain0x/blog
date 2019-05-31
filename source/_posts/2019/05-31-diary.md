---
title: 近況 2019-05-31
date: 2019-05-31 23:59:59
permalink: diary
tags:
  - 日記
---

今月の活動のまとめ

- 前月分 <https://vain0x.github.io/blog/2019-04-30/diary>

## ミローネ言語

https://github.com/vain0x/milone-lang/commits?author=vain0x&since=2019-04-30&until=2019-05-31

- セルフホスティングを目指している F# サブセットのコンパイラ

### ミローネ: 多相関数の単相化

[Monomorphizing.fs](https://github.com/vain0x/milone-lang/blob/628adf3b38c27a79d3c58936e2b9ca1d01d99693/boot/MiloneLang/Monomorphizing.fs)

- 多相関数の単相化を実装した
- C++ のテンプレートのように型引数ごとにコード複製する
- コードのコメントにおおよそ解説を書いた
- 例えば以下の flip は任意のペアに作用するジェネリック関数であり、そのままではC言語にコンパイルできない

```fsharp
    let flip (x, y) = (y, x)
```

- 以下のような呼び出しがあるとする

```fsharp
    (1, "one") |> flip
```

- この式における flip には `(int, string) -> (string, int)` という単相型がついている
- この型を使用箇所の型 (use-site type) と呼ぶ
- flip を複製して、この単相型を持たせたものを用意する (単相化インスタンスと呼ぶ)
- flip の呼び出しを単相化インスタンスの呼び出しに変換する

```fsharp
    let flipForIntStringPair (x: int, y: string) = (y, x)

    (1, "one") |> flipForIntStringPair
```

- この変換をひたすら繰り返す
- このアルゴリズムは基本的なケースではたぶん動く
- 効率が悪いし、そもそも停止性を証明できてない
- ひとまず開発当初からの懸念だった多相関数のコード生成ができて安心した

### ミローネ: モジュール内の相互再帰

https://github.com/vain0x/milone-lang/commit/8eb8b30b1b90b842a2062bdfe2b14125ac9f4d42

- F# には module rec という機能があって、rec キーワードつきで定義したモジュールに配置された関数は相互参照できる
- 構文解析や型推論をはじめとして、コンパイラの各ステージは巨大な相互再帰として実装されている
- そのためこの機能は重要。(必須ではない。`let .. and ..` 構文を使うという手もある。関数オブジェクトを使うという手もある)
- アルゴリズムは単純なトポロジカルソートと接ぎ木

手順:

- 最初のモジュール (`Foo/Foo.milone`) をパースする
- `open Foo.Bar` のような open 文を収集しておく
- open されているモジュール (`Foo/Bar.milone` など) を探してパースする
- これを再帰的に繰り返すとトポロジカルオーダーでモジュールが並ぶ
- あとは各モジュールの抽象構文木を接合する

```fsharp
// Bar
let bar () = .. in ()

// Foo
let main _ = .. in ()

// ↓↓↓

let bar () = .. in
let main _ = .. in ()
```

- これだと open していないモジュールに含まれる関数が見えてしまったりするが、そのへんは厳密にしてない
- セルフホスティングという目的の障害にならない問題は積極的に無視していく

### ミローネ: 再帰的な判別共用体

https://github.com/vain0x/milone-lang/commit/d9dbc8e5e8fc71318d2d4a10c7c302c64c32e2a1

- F# の判別共用体は Struct 属性をつけなければヒープに確保されて参照として取り扱われる
- そのため判別共用体がその型自身への参照を持つようなことも可能
- ミローネ言語コンパイラでも抽象構文木の型などにこの機能を利用している

```fsharp
// よくある抽象構文木
type Expr =
    | Int of int
    | Add of Expr * Expr // Expr 自身を再帰的に参照している
```

- 判別共用体はトークンの種類にも使われている

```fsharp
type TokenKind =
    | Int of int
    | Plus
    | ParenL // (
    | ParenR // )
```

- ペイロード (`of int` の部分) を持たないバリアント (Int とか Plus とか) を参照として取り扱うとき、いちいち malloc するのは無駄
- F# では定数として扱われていて、おそらく同一のインスタンスが使いまわされる
- 場合分けが発生するし、ペイロードのないバリアントのリストを事前に列挙してグローバル変数を宣言して……みたいなコードを書くことになってめんどくさい
- ペイロードをポインタ経由で持つ、というルールにした
- 例えば TokenKind はC言語で以下の型に表現する

```c
enum TokenKindTag {
    Int, Plus, ParenL, ParenR,
};

struct TokenKind {
    enum TokenKindTag tag;
    union {
        int *int_; // Int of int のペイロード
    };
};
```

- ペイロードのないバリアントは `(struct TokenKind){.tag = Plus}` のように値で生成される
- 理想的には再帰型のみポインタ経由で参照するという方針にしたい
- あるいは「ペイロードの sizeof が16バイトを超えるとき」みたいな判定でもいいかも
- 再帰型のペイロードならタグ (enum) のサイズがついて 4 + 4 + .. > 16 になる
- そもそも抽象構文木の表現に再帰的な判別共用体を使わないという実装方針もありうる

### ミローネ: ラムダ式

- 抽象構文木を組み替える雑実装
- `fun x -> y` を `let f x = y in f` にする

## 競技プログラミング

週末は AtCoder に参加。ABC が6問構成になって、かなりエキサイティング

- [競プロ参戦記 #45 DivRem Number | diverta 2019](https://qiita.com/vain0x/items/fd9854b5863f1634dd04)
- [競プロ参戦記 #46 XOR Matching | ABC 126](https://qiita.com/vain0x/items/8fcd5bf0704b3011b8fc)
- [競プロ参戦記 #47 Cell Distance | ABC 127](https://qiita.com/vain0x/items/87f227f867749600b5ee)
- 競プロ参戦記 #48 Frog Jump | ABC 128 (TODO: 明日書く)

## picomet-lang

<https://github.com/vain0x/picomet-lang>

- JavaScript をターゲットにコンパイルする作業を開始した
    - 全然できてない

## その他

ゴールデンウィークにやろうとしたことができなかったので、終わってからいろいろやった

> イベントを利用してパーサーと構文木を分離するやつ、具象構文木をラップして抽象構文木を作るやつ、ロスレスな具象構文木を作ってラウンドトリップさせるやつ、とかの理解を深めるつもりだったのに (2019/05/03)

## その他: イベント型のパーサー

https://github.com/vain0x/playground/tree/master/play/2019-05-04-event-based-parser

- [rust-analyzer](https://github.com/rust-analyzer/rust-analyzer) の ra_parser (Rust の構文解析の実装) で採用されている方式の小さい版
- 構文解析の結果としてツリーではなく「イベント」のリストを生成するというもの (リンク先を参照)
- ツリーの設計とパーサーの実装を切り離せる
- パーサーが「トークンのリストに階層構造を作る」という作業しかしなくなる (できなくなる)
    - トークンを増減させたり順番を入れ替えたりということはしない
    - そういうのはイベントを受け取ってツリーを組み立てる部分の責務
- 親子関係の指定を厳密にしなくてよくなる
- エラー耐性が高い、らしいが感覚はつかめていない

> 字句解析はソースコードを区間分割して各区間に種類を割り当てるもの、構文解析はトークンのリストを (順番を変えずに) ツリーにするもの、みたいな直観を得つつある (2019/05/05)

## その他: rowan の TypeScript 移植

https://github.com/vain0x/playground/tree/master/play/2019-05-06-picolyn

- [rowan](https://github.com/rust-analyzer/rowan) というライブラリがある
    - ロスレスな具象構文木を扱う対象言語非依存のライブラリ
    - Roslyn (C#) や Swift コンパイラなどで採用されているらしい Red/Green ツリーの実装
- Rust で書かれているのでメモリ管理が煩雑
- 実装を参考にしながら TypeScript で小さく作り直してる
- これをラップして抽象構文木として操作するあたりの直観がまだ得られていない

## その他: prettier printer の Rust 移植

https://github.com/vain0x/playground/tree/master/play/2019-05-11-prettier-printer-rust-from-scratch

- 空白と改行を自動的に使い分けて最適な文字列描画のレイアウトを構成するアルゴリズム
- 中身を読んで、Haskell で書かれた参考実装を Rust に移植してみた
- (自作言語の) コードフォーマッターを作るときに使えるかもしれない

## その他: C# のコンストラクタ自動生成ツール (for VSCode)

https://github.com/vain0x/csharpextensions

- VSCode 上で動く C# 拡張
- 既存の拡張 (C# extensions) に類似機能があったが、思うように動かないケースがあった
    - sealed class や struct に反応しない
    - (プロパティではなく) フィールドに対応していない
- 開発も停止していたので fork して自分好みに書き換えた
    - 正規表現ベースかつやっつけ改修なので、正常に動かないケースも少なくない
- publish した
    - `vain0x.csharp-gen-ctor`

## その他: クラゲ言語

https://github.com/vain0x/curage-lang

- LSP の動作確認のスクリーンショットをつけた
- [バージョン間の差分](https://github.com/vain0x/curage-lang/compare/v0.1.0...v0.2.0) へのリンクをつけた

## その他: WPF ナビゲーションサンプル

https://github.com/vain0x/wpf-nav-sample

- ページ遷移を含むアプリの構成のサンプル
- 2年ぐらい前に作っていたやつをせっかくなので完成させた
- 要点:
    - DataTemplate を使うとビューモデルからビューへのマッピングを作れる
    - ContentPresenter を使うとビューモデル層で表示するコントロールを切り替えられて、ページ遷移ができる
    - RelativeSource を使うとビューモデル上でのフレームとページの相互参照を回避できる

## その他: Rx

Rx.NET ([dotnet/reactive](https://github.com/dotnet/reactive)) と [ReactiveProperty](https://github.com/runceel/ReactiveProperty) を使った WPF アプリを改修して思ったことのメモ書き

要点:

- WPF と Rx の組み合わせは必然
- Flux は冗長性と引き換えにすべてを解決する
- Rx ストリームの自動停止を有効活用すべし

### Rx: PropertyChanged から Rx への道筋

- WPF のバインディング機能には `INotifyPropertyChanged.PropertyChanged` (プロパティの値が変化したときに起こすイベント) を適切に発火する必要がある
- `string NameSan => Name + "さん"` みたいな get プロパティについても発火する必要がある
- この例では Name が変化したときに NameSan が変化した旨のイベントを発火しなければいけない
- そうしないと NameSan をバインディングしている部分の UI に変更が反映されない
- つまり「Name が NameSan に依存されている」ことを Name が知っている、という記述
- この例では単一オブジェクト内での依存だが、他のオブジェクトに依存されることもある
- 依存関係を逆転して「NameSan が Name に依存している」ように記述したい
- オブザーバーパターンを使う
- C# におけるオブザーバーパターンの実装では event と Rx がよい
- Rx を使う

### Rx: Flux

- ReactiveProperty (new で作ったもの) をあちこちに置くと整合性を維持するのが難しい
    - TODO: これは詳細を書いた方がいい
- イミュータブルなデータ (S) を値とする ReactiveProperty を1個だけ用意する
- 残りはそこから導出する (`state.Select().ToReactiveProperty()` とする)
- データの変更を通知するための `Subject<Func<S, S>>` を用意しておいて、各コンポーネントに渡す
- 例えば導出した RP が TwoWay バインディングで変化したら、変更に基づくデータの更新関数を onNext して渡す
    - `Text.Subscribe(text => subject.OnNext(state => state.SetText(text)))` みたいな

利点:

- 整合性の維持がとても簡単になる
- 変更を集約する subject に ObserveOn(dispatcher) を挟むだけでスレッドセーフになる
    - 別スレッドからも変更操作をしたいときにやる
    - シングルスレッドモデルなのでパフォーマンスは下がる
- おまけ: 根元の ReactiveProperty を Dispose しただけで下流が一気に停止する
    - Subscribe したときに発生する IDisposable を CompositeDisposable に詰めていく作業をしなくていい

欠点:

- コードがやたら冗長
    - CRUD してるだけの部分とかは N 倍になる

注意点:

- ReactiveProperty は Subscribe した瞬間に発火するので、それがイミュータブルなデータに差分を作ってしまうと無駄にループが回る
    - イミュータブルなデータの更新時に、差分がないことを検出して同一インスタンス (this) を返すようにしておく
    - あるいはイミュータブルなデータが構造的同値性を備えるようにしておく

### Rx: 可変コレクションの集計

- 可変コレクション (ObservableCollection) の要素から飛んでくる通知 (IObservable が出力するイベントを通知と呼ぶことにする) を集計する方法で苦労していた
- コレクションに Add/Remove が起こるたびに Merge をやり直すという力技だった
    - パフォーマンスが悪すぎる
- 上記の方法でおおよそ解決した

簡単にかくと:

- イミュータブルなデータには配列 (ImmutableArray) でコレクションの各要素に対応するデータを持つ
- 変化するたび、コレクション側で差分を計算して ObservableCollection に対応する変更を行う
- データが追加されたときは対応する要素を生成する
    - 自分がリストに含まれているかを TakeWhile でチェックしておく
    - 要素の生成時に、要素内の変更を購読して上述の subject に送るようにしておく
- データが削除されたときは対応する要素を削除する
    - TakeWhile のおかげで要素は自動で停止するので、特に Dispose などをする必要はない
- 結局、この可変コレクションがしているのは差分を計算して同期するというだけで、IObservable/IDisposable のわずらわしさからは解放されている

課題:

- 差分計算が難しい
    - 現状は1要素の Add/Remove しかないから簡単
    - Move が入ると大変

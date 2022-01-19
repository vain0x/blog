---
title: Rustで競プロするときのプラクティス [2018秋]
type: "post"
date: 2018-10-07 20:00:00
url: 2018-10-07/rust-procon
tags:
    - 古い記事
#   - Rust
#   - 競技プログラミング
---

<!--more-->

競プロで Rust を使い始めて半年が過ぎました。いまの私のプラクティスを羅列的に書いていきます。

## 筆者

- AtCoder 水色
- [競プロ参戦記](https://note.mu/vain0x/n/ndcec1623a167) やってます

## フレームワーク

[自作フレームワークはこちら](https://github.com/vain0x/procon/blob/f016133b83c42196837e1b2490ecb5e57ce1ff40/rust/src/main.rs) 。いつもこれの main 関数の中に解答を書いて提出しています。

## 入力のパース

標準機能だけで入力をパースしようとするとだるいです。Qiita にも、すでにこの問題を解決しようという記事がいくつか上がっています。

私は上述のフレームワークに含まれている [`read!` マクロ](https://github.com/vain0x/procon/blob/f016133b83c42196837e1b2490ecb5e57ce1ff40/rust/src/main.rs#L25-L47) を使っています。実装が短い (23行) のと、見た目が関数っぽくて好み。

```rust
    //! グラフのパースの例
    let (N, M) = read!(usize, usize);
    let weightd_edges = read![[usize, usize, i64]; M];
```

パフォーマンスは若干悪くて、C++ で std::cin を使うより2倍ぐらい遅いです。それでも 10^6 個の整数を読むのに 100 ms 未満なので問題はないはず。ベンチマーク: [scan-bench](https://github.com/vain0x/scan-bench)

## デバッグ用のマクロ

[LLDB を使うとデバッグ実行できるらしい](https://qiita.com/yamoridon/items/3be3f0515a79567a0588) です。私はやってなくて、いつも print デバッグしています。

### debug!: 条件コンパイル

デバッグ出力の消し忘れで WA とかは避けたいです。 [条件コンパイル](https://doc.rust-jp.rs/the-rust-programming-language-ja/1.9/book/conditional-compilation.html) を使って、ローカルではデバッグ出力が出る、ジャッジ時は出ない、というふうにしています。

Debug/Release での分岐は [debug_assertions を使えばできる](https://users.rust-lang.org/t/conditional-compilation-for-debug-release/1098/3) そうです。

```rust
// デバッグビルドではこっちの定義が使われる。
#[cfg(debug_assertions)]
macro_rules! debug {
    // 略
}

// リリースビルドではこっちの定義が使われて、 debug!(..) が無になる。
#[cfg(not(debug_assertions))]
macro_rules! debug {
    ($($arg:expr),*) => {};
}
```

### debug!: 定義の省略

ジャッジに不要なコードを提出に含めるのは *なんとなく* 抵抗があります。そこで、定義はローカルのファイルに書いておき、手元でのデバッグ実行時だけ `include!` で定義を取り込むという方法があります。

```rust
// デバッグ時はローカルのファイルから定義を読み込む
#[cfg(debug_assertions)]
include!{"./procon/debug.rs"}

// リリース時に debug!(..) を消す
#[cfg(not(debug_assertions))]
macro_rules! debug {
    ($($arg:expr),*) => {};
}
```

これで debug! マクロを高機能化しても提出コードが膨れ上がることはなくなります。

### 余談: dbg!

ちなみに、将来のバージョンでは公式に `dbg!` という print デバッグ用途のマクロが入るそうです。参考：[rfcs/2361-dbg-macro.md at master · rust-lang/rfcs](https://github.com/rust-lang/rfcs/blob/master/text/2361-dbg-macro.md)

## 数値型

数値型は [プリミティブ型](https://doc.rust-jp.rs/the-rust-programming-language-ja/1.9/book/primitive-types.html) に載っているとおりたくさんありますが、よく使うのは4つ:

|||
|:-----|:----|
| i64 | 10^18 ぐらいまで扱える |
| usize | スライスや Vec の添字に使う |
| f64 | 浮動小数点数 |
| char | 文字 (厳密には Unicode scalar value) |

### 数値型: usize

usize を int の感覚で書いてると微妙にハマります。マイナスにオーバーフローしたとき、Debug モードでは実行時エラーになります。Release モードではエラーになりませんが、符号なし型なのでマイナスにはならないことに注意です。

例えば次のコード (x, y: usize) はおかしくて、`x < y` のとき Debug モードではエラーになり、Release モードでも `max` は機能してません。

```rust
    max(0, x - y) // ✘ ダメ
```

オーバーフローしないようにするか、i64 を経由すれば動きます:

```rust
    // monus
    x - min(x, y)

    // または、i64 を経由する
    max(0, x as i64 - y as i64) as usize
```

## 文字列

Rust の標準的な文字列型である String (と str) は utf-8 なので、ランダムアクセスできません。そういうときは文字の列 `Vec<char>` で持つようにします。

```rust
    let s: String

    // 文字の列に変換
    let s = s.chars().collect::<Vec<_>>();
```

これは若干パフォーマンスが悪いです。一時期は `Vec<u8>` を使っていたんですが、デバッグ出力時に u8 が数字で出るのが不便だったのでやめました……

## Vec

イテレータを Vec に変換する `.collect::<Vec<_>>` は頻出ですが、タイプがつらいので略記を用意してます。

自作トレイトをすべてのイテレータに実装させることで、 C# の拡張メソッドのようなことができます。

```rust
trait IteratorExt: Iterator + Sized {
    fn vec(self) -> Vec<Self::Item> {
        self.collect()
    }
}

impl<T: Iterator> IteratorExt for T {}
```

```rust
// e.g.
let xs = (0..N).map(|i| i + 1).vec();
```

## 再帰

ローカル変数を利用する再帰関数について、7月に「 [Rustのクロージャで再帰してみた](https://qiita.com/vain0x/items/90c9580aa34926160ac1) 」という記事を書きました。クロージャは、mut な変数を書き換えないなら簡単に再帰にできるという結論です。

mut な変数を書き換えたいなら RefCell とかを使う、と記事では書きました。struct を定義したほうがいいこともあります。いずれにせよ手間なので、マクロで簡略化を図りたいと思っています。

## 参考

- [公式のドキュメントの和訳](https://doc.rust-jp.rs/the-rust-programming-language-ja/1.9/book/)
    - 必読
- [Rustで競技プログラミング スターターキット - Qiita](https://qiita.com/hatoo@github/items/fa14ad36a1b568d14f3e)
    - コンパイラバージョンの固定 (rustup override set)
    - BinaryHeap を昇順で使う方法 (Reverse)
    - 浮動小数点で sort などを使う方法 (impl Ord)
    - 深い再帰でスタックオーバーフローしないようにする方法 (std::thread)
    - などについてはこの記事を読んでください

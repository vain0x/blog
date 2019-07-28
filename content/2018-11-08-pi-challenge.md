---
title: 円周率チャレンジにアルゴリズムでチャレンジ
tags:
  - Rust
  - アルゴリズム
type: "post"
date: 2018-11-08 02:01:00+09:00
url: 2018-11-08/pi-challenge
---

[円周率チャレンジ](https://rirosi.net/plus2/) というゲームが流行中です！

プログラムを使って良い解法を探索してみたので、考えたことを書いていきます。

<!--more-->

## 円周率チャレンジとは

> ## 円周率チャレンジ
>
> 「円周率チャレンジ」は，「ルートをとる(sqrt)」「2を足す(+2)」の２種類のボタンを使って，
> 数字を円周率に近づけていくゲームです。できるだけ少ない回数でハイスコアを狙いましょう！

例えば +2, √, +2 という手順で操作を行うと、数値は:

0 → 2 → 1.41421.. → 3.41421..

と変化して、円周率 π = 3.141592.. に近い数値になります。

もっと手数を増やせば、 **さらに円周率に近づけられるはず** 。その最良手順を探すゲームです。

## 解法1: ビット全探索

やはり最初に試すのは全探索でしょう！

手数 N の最良手順を探すことを考えます。ここで手順とは「ルートをとる」と「+2 を足す」の2種類の要素からなる長さ N の列です。手順をすべて列挙して、計算結果と円周率を実際に比べてみれば、何が最良か分かるはずです。

2種類の要素を 0, 1 で表せば、手順はビット列として扱えて、配列を使うより経済的に全探索できます。参考: [ビット演算 (bit 演算) の使い方を総特集！ 〜 マスクビットから bit DP まで 〜 - Qiita](https://qiita.com/drken/items/7c6ff2aa4d8fce1c9361#bit-%E5%85%A8%E6%8E%A2%E7%B4%A2)

操作列の「実行」は次のようなコードです。下位ビットから順番にみていき、 0 なら +2 の操作を、1 ならルートをとる操作を実行して、最終結果を得ます。

```rust
fn eval(ops: i64, n: usize) -> f64 {
    let mut value = 0.0_f64;

    for i in 0..n {
        if (ops & (1 << i)) == 0 {
            value += 2.0;
        } else {
            value = value.sqrt();
        }
    }

    value
}
```

これをすべての操作列に適用して、円周率の差が最小のものを探します。抜粋:

```rust
    //..
    let mut best_diff = 1e9; // 最小の差
    let mut best_ops = 0; // 最小の差を達成した操作列

    for ops in 0..1 << n { // 操作列の全列挙
        let value = eval(ops, n); // 操作の実行
        let diff = (value - PI).abs(); // 円周率との差

        if best_diff > diff { // 最小値の更新
            best_diff = diff;
            best_ops = ops;
        }
    }
    //..
```

[コード全体はこちら](https://play.rust-lang.org/?version=stable&mode=release&edition=2015&gist=61011280947f9a6641333f63bc0b90cb)

時間がかかるのでリリースモードを使って (`cargo run --release`) 実行すると、こんな感じで手数ごとの良い操作列を出します:

```
手数 数値                差                 操作
#00 0.0000000000000000 3.1415926535897931
#01 2.0000000000000000 1.1415926535897931 +
..
#27 3.1415927103795092 0.0000000567897160 +/++ +/// //// //// ++// +/++ ++/
#28 3.1415926624518788 0.0000000088620857 +//+ ++// //// //// /++/ /+/+ +++/
```

これで手数 N=28 ぐらいまでの最良(?)手順が求まりました。それ以上は時間がかかりすぎるので、別の方法で探索したほうがよさそうです。

## 解法2: 半分全列挙

全探索では 0 から π に近づけましたが、逆に π から 0 に近づける操作列も考えられます。つまり、

> π から始めて、「2を引く」と「2乗する」を組み合わせて 0 に近づけるゲーム

をやってもいいです。このことから、0 と π の **両端から良い手順を探して繋ぐ** という解法が出てきます。いわゆる半分全列挙です。

具体的には:

- 手数 N/2 の操作 F をすべて列挙して、その結果の数値を A とするとき、(A, F) というペアをすべて記録しておく。
- 次に、手数 N/2 の操作 G をすべて列挙して、それを π から逆算したときの数値を B とする。
- メモから (B, F) に最も近いものを探して操作 F+G を作れば、これは π に近い数になる。

```
        F            G
    0   →   A ≒ B   →   π
```

計算量はどうでしょう。

- メモ ((A, F) の配列) を A についてソートしておけば、メモから探すのは二分探索ができます。
- ソートを工夫しなければ、全体として O(2^(N/2) log N) です。
    - これなら **N = 50 ぐらいまでいけそう** 。

[コード全体はこちら](https://play.rust-lang.org/?version=stable&mode=release&edition=2015&gist=17cf3535110ee76685581525e57cad15)

しばらく待つと、53手のかなり強い手順が得られます。小数点以下15桁まで一致。

## 終わりに

執筆時点で1位の (手数 53, スコア Infinity) には及びませんでした。おそらく二分探索の段階で近似的になりすぎてしまうからかなと思います。ヒューリスティックなアルゴリズムや数学的なアプローチの余地がまだまだありそうです。

なにはともあれ、半分全列挙のよい練習になりました。

## 参考

- [プログラミングコンテストチャレンジブック \[第2版\]【委託】 - 達人出版会](https://tatsu-zine.com/books/procon-challenge)
    - 通称蟻本。半分全列挙が載っていました

## Appendix. ソースコード

<details>
    <summary>ビット全探索</summary>

```rust
use std::f64::consts::PI;

/// 操作列 ops の下位ビットから r 桁を実行して、結果の数値を得る。
fn eval(ops: i64, n: usize) -> f64 {
    let mut value = 0.0_f64;

    for i in 0..n {
        if (ops & (1 << i)) == 0 {
            value += 2.0;
        } else {
            value = value.sqrt();
        }
    }

    value
}

/// 操作列を読みやすい文字列にする。
fn how(ops: i64, n: usize) -> String {
    let mut acc = String::new();

    for i in 0..n {
        if i > 0 && i % 4 == 0 {
            acc.push(' ');
        }
        if (ops & (1 << i)) == 0 {
            acc.push('+');
        } else {
            acc.push('/');
        }
    }

    acc
}

fn main() {
    // n: 手数
    for n in 0..29 {
        let mut best_diff = 1e9;
        let mut best_ops = 0;

        for ops in 0..1 << n {
            let value = eval(ops, n);
            let diff = (value - PI).abs();

            if best_diff > diff {
                best_diff = diff;
                best_ops = ops;
            }
        }

        let value = eval(best_ops, n);
        let diff = (value - PI).abs();
        let how = how(best_ops, n);
        println!("#{:>02} {:>.16} {:>0.16} {}", n, value, diff, how);
    }
}
```

</details>

<details>
    <summary>半分全列挙</summary>

```rust
use std::f64::consts::PI;

pub fn lower_bound<T: PartialOrd>(xs: &[T], y: &T) -> usize {
    let mut l = 0;
    let mut r = xs.len() + 1;

    while r - l > 1 {
        let m = l + (r - l) / 2;
        if &xs[m - 1] < y {
            l = m;
        } else {
            r = m;
        }
    }

    l
}

/// 操作列 ops の下位ビットから n 桁を実行して、結果の数値を得る。
fn eval(ops: i64, n: usize) -> f64 {
    let mut value = 0.0_f64;

    for i in 0..n {
        if (ops & (1 << i)) == 0 {
            value += 2.0;
        } else {
            value = value.sqrt();
        }
    }

    value
}

/// 操作列 ops の上位ビットから n 桁の逆操作を実行して、結果の数値を得る。
fn eval_inv(ops: i64, n: usize) -> f64 {
    let mut value = PI;

    for i in (0..n).rev() {
        if (ops & (1 << i)) == 0 {
            value -= 2.0;
        } else {
            value *= value;
        }
    }

    value
}

/// 操作列を読みやすい文字列にする。
fn how(ops: i64, n: usize) -> String {
    let mut acc = String::new();

    for i in 0..n {
        if i > 0 && i % 4 == 0 {
            acc.push(' ');
        }
        if (ops & (1 << i)) == 0 {
            acc.push('+');
        } else {
            acc.push('/');
        }
    }

    acc
}

/// 手数 n の操作の結果をすべて計算する。
fn enumerate(n: usize) -> Vec<(f64, i64)> {
    let mut memo = vec![];

    for ops in 0..1 << n {
        let value = eval(ops, n);
        memo.push((value, ops));
    }

    memo.sort_by(|(lx, _), (rx, _)| lx.partial_cmp(rx).unwrap());
    memo
}

fn main() {
    // n: 手数
    for n in 0..54 {
        let ln = n / 2;
        let rn = n - ln;

        let memo = enumerate(ln);

        let mut best_diff = 1e9;
        let mut best_ops = 0;

        for r_ops in 0..1 << rn {
            let mid = eval_inv(r_ops, rn);

            let i = lower_bound(&memo, &(mid, r_ops));
            if !(i < memo.len()) {
                continue;
            }
            let (_, l_ops) = memo[i];

            let ops = (r_ops << ln) | l_ops;
            let value = eval(ops, n);

            let diff = (value - PI).abs();
            if best_diff > diff {
                best_diff = diff;
                best_ops = ops;
            }
        }

        let value = eval(best_ops, n);
        let diff = (value - PI).abs();
        let how = how(best_ops, n);
        println!("#{:>02} {:>.16} {:>.16} {}", n, value, diff, how);
    }
}
```

</details>

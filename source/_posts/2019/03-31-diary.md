---
title: 近況 2019-03-31
date: 2019-03-31 23:59:59
permalink: diary
tags:
  - 日記
---

今月の活動のまとめ

- 前月分 <https://vain0x.github.io/blog/2019-02-27/diary>

## picomet-lang

<https://github.com/vain0x/picomet-lang>

- 自作のプログラミング言語処理系
- curage-lang　を参考に、hover などの機能を持つ LSP サーバーを用意した
- 相互再帰関数や HM 型推論を実装した
- malloc 的なもので `[int]` (int の配列) を動的確保できるようになったので、たいていの問題は解けるようになった
    - 現実的には動的配列やハッシュテーブルがないと辛い
- バックエンドよりフロントエンドに力を入れて、最小実用製品にしたほうがよさそうな気がしてきた
- Docker を使って手軽にビルドできるようにしたが、やめた
    - Windows でも Ubuntu でもビルドしづらくなっただけだった
    - ビルドするための bash スクリプトを用意しつつ、Windows 向けには別途インストラクションを README に書くということにした

## ネギ言語

<https://github.com/vain0x/negi-lang>

- C言語に移植してみた
- 初めてC言語でまとまったコードを書いた
- 動的配列と enum class がほしかった

## Tsunotoshi

<https://github.com/vain0x/tsunotoshi>

- オンラインブックマークアプリ
- 実験的に、サーバーサイドレンダリングを行うような仕組みに変更しているが、迷走中

## 競技プログラミング

週末の AtCoder に加えて Codeforces の問題もいくつか解いた。成績は芳しくない。

- [競プロ参戦記 #35 Decayed Bridges | ABC 120](https://qiita.com/vain0x/items/b869075e3df587e2ecde)
- [競プロ参戦記 #36 XOR World | ABC 121](https://qiita.com/vain0x/items/9faf89f843f96d8c46cd)
- [競プロ参戦記 #37 Skyscrapers | CR #545 Div.2](https://qiita.com/vain0x/items/cd075fd5229fa6ec5ef5)
- [競プロ参戦記 #38 Reversi | AGC 31](https://qiita.com/vain0x/items/9adda58a3fc6d31fff3e)
- [競プロ参戦記 #39 Minimum Triangulation | エデュフォ 62 Div.2](https://qiita.com/vain0x/items/671ebca10c08b161d1bf)

## ガルパ (リズムゲーム)

- EX トライマスターを取得した (2回目)
- 2nd Season 最高だった

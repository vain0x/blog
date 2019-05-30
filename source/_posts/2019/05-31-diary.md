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

https://github.com/vain0x/milone-lang

- セルフホスティングを目指している F# サブセットのコンパイラ
- 多相関数の単相化

- モジュール内の相互再帰

- 再帰的な判別共用体
    - ペイロードをポインタ経由で持つようにした

- ラムダ式
    - 抽象構文木を組み替える雑実装
    - `fun x -> y` を `let f x = y in f` にする

## 競技プログラミング

週末は AtCoder に参加。ABC が6問構成になって、かなりエキサイティング。

- [競プロ参戦記 #45 DivRem Number | diverta 2019](https://qiita.com/vain0x/items/fd9854b5863f1634dd04)
- [競プロ参戦記 #46 XOR Matching | ABC 126](https://qiita.com/vain0x/items/8fcd5bf0704b3011b8fc)
- [競プロ参戦記 #47 Cell Distance | ABC 127](https://qiita.com/vain0x/items/87f227f867749600b5ee)
- 競プロ参戦記 #48 Frog Jump | ABC 128 (TODO: 明日書く)

## picomet-lang

<https://github.com/vain0x/picomet-lang>

- JavaScript をターゲットにコンパイルする作業を開始した
    - 全然できてない

## その他

ゴールデンウィークにやろうとしたことができなかったので終わってからいろいろやった

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

## その他: WPF Nav サンプル

https://github.com/vain0x/wpf-nav-sample

- ページ遷移を含むアプリの構成のサンプル
- 2年ぐらい前に作っていたやつをせっかくなので完成させた
- 要点:
    - DataTemplate を使うとビューモデルからビューへのマッピングを作れる
    - ContentPresenter を使うとビューモデル層で表示するコントロールを切り替えられて、ページ遷移ができる
    - RelativeSource を使うとビューモデル上でのフレームとページの相互参照を回避できる

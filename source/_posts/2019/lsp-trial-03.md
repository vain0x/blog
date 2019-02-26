---
title: "LSP学習記 #3 シンボルとハイライト"
tags:
- LanguageServerProtocol
- TypeScript
- 言語処理系
- LSP学習記
date: 2019-01-10 22:12:00
permalink: lsp-trial-03
---

自作言語の LSP サーバーを作るプロジェクトの第3回です。今回はシンボルテーブルを作ってシンボルのハイライト機能を実装します。

- 初回: [LSP学習記 #1](https://vain0x.github.io/blog/2019-01-05/lsp-trial-01/)
- 前回: [LSP学習記 #2 クラゲ言語の構文解析](https://vain0x.github.io/blog/2019-01-07/lsp-trial-02/)
- 今回のソースコード: [curage-lang v0.4.0](https://github.com/vain0x/curage-lang/tree/v0.4.0)

## let文とシャドーイング

前回構文を定義したように、 **クラゲ言語** (curage-lang) のプログラムは `let` 文の繰り返しです。

クラゲ言語の `let` はローカル変数を作るものですが、TypeScript の `let` とやや異なる性質を持つように定めます。その性質とは、「シャドーイング」です。例えば、次のコードを実行すると `y` の値は `2` になります。

```curage
let x be 1
let x be 2
let y be x
```

このコードは `x` という名前の変数を2つ定義します。2つ目の `x` が定義された時点で、1つ目の `x` を使える範囲が終了します。TypeScript でいうと次のような感じです。

```ts
{
    // ...
    const x = 1;
    {
        const x = 2;
        const y = x;
        // ...
    }
}
```

シャドーイングがあるといろいろ便利なんですが、今回はひとまず、連載の進行をスムーズにするために入れます。

## シンボルとハイライト

上記のクラゲ言語のコードで、`x` という名前は3回出現します。1回目と2回目が別の変数を指す一方で、2回目と3回目は同じ変数を指します。

「同じ変数を指しているかどうか」でグループ分けすると便利です。同じ変数を指す名前は、同じ **シンボル** であると呼ぶことにします。

どれとどれが同じシンボルなのか、分かりやすく表示されると嬉しいです。

おそらくこの用途を想定して、エディターにソースコードの一部をハイライトしてもらえる機能が LSP にあります。今回の主役、 [`textDocument/documentHighlight` リクエスト](https://microsoft.github.io/language-server-protocol/specification#textDocument_documentHighlight)  です。

LSP クライアントは、サーバーに `documentHighlight` リクエストを送ってハイライトすべき場所を尋ねます。LSP の上ではカーソルという概念は出現しませんが、少なくとも VSCode や Sublime Text はカーソルの位置を指定してこのリクエストを送ってくれるようです。

- 注: `textDocument/documentHighlight` リクエストが来るのは LSP サーバーがこの機能に対応している場合のみです。`initialize` レスポンスに指定する `capabilities` に設定が必要です。(詳細は実際のコードを参照)

言葉で説明してもアレなので、今回の動作例を先に貼ります:

![](https://qiita-image-store.s3.amazonaws.com/0/74340/d9da9328-5a47-49b1-0a39-784fab38a091.png)

(カーソル上にある `x` と、同じ変数を指す `x` がすべてハイライトされているが、他の変数はハイライトされていない、という状況)

カーソル位置にある名前が指しているシンボルと同一のシンボルをハイライトさせる。これが今回の目標です。

## 実装

そういうわけで、ソースコード上の名前がどのシンボルに対応するのかを調べる処理を書きます。

クラゲ言語の構文をとてもシンプルにしているおかげで、実装もシンプルに済みます。

前回の構文解析で得られた `let` 文のリストを順にみていき、文中に出現している「名前」(トークン)の情報を記録していく、というのが大まかな流れです。

```ts
  for (const statement of statements) {
    if (statement.type === "let") {
      const { init, name } = statement

      if (init.type === "name") {
        referName(init)
      }

      if (name.type === "name") {
        defineName(name)
      }
    } else {
      throw new Error("NEVER")
    }
  }
```

ある名前が変数を参照している (= 式として出現している) のか、変数を定義している (= `be` の左辺に出現している) のか、というのを文脈から判別しています。

- 余談: いまのクラゲ言語は非現実的なほど簡素ですが、仮にループ構文や足し算などがあったとしても、実装の基本的な考えは変わらないはずです。

### 実装: シンボルテーブルと環境

シンボルが持つべき情報は何でしょうか。それがどこで定義されたのかと、どこで使われたのか、です。これをほぼそのまま型定義にしたのが、次の `SymbolDefinition` です:

```ts
interface SymbolDefinition {
  /** このシンボルを定義したトークン */
  definition: Token,
  /** シンボルを参照するトークンの集まり */
  references: Token[],
  /** このシンボルの種類。いまは変数だけ */
  type: "var",
}
```

もう1つ必要なものがあって、名前からシンボル定義へのマップ `environment` です。

```ts
  const environment = new Map<string, SymbolDefinition>()
```

何らかの変数を参照している名前をみつけたときに、それが実際に指しているシンボルを特定するのに使います。例えば次の処理、「式」として名前が出現したときの処理です:

```ts
  const referName = (nameToken: Token) => {
    const symbolDefinition = environment.get(nameToken.value)
    if (!symbolDefinition) return // ここで未定義変数の警告を出してもいい

    symbolDefinition.references.push(nameToken)
  }
```

一方で、変数として定義される名前をみつけたときは、環境に名前を追加します。ここで、同名の変数がすでに環境にあるときは「上書き」されますが、それがまさに冒頭に書いた「シャドーイング」の挙動なのでOKです。

```ts
  const defineName = (nameToken: Token) => {
    const definition: SymbolDefinition = {
      type: "var",
      definition: nameToken, // 定義位置を記録
      references: [],
    }

    symbolDefinitions.push(definition) // 新しいシンボル
    environment.set(nameToken.value, definition) // 同名の変数は上書き
  }
```

そして解析が完了したあと、最終的に環境は捨てて、シンボル定義のリスト (シンボルテーブル) を解析結果とします。解析結果は繰り返し使うので、 `SemanticModel` という名前のインターフェイスを定義しました。

```ts
  return { statements, symbolDefinitions, diagnostics } as SemanticModel
```

- [変更点まとめ](https://github.com/vain0x/curage-lang/commit/8dda6a9798fefd6cb4507615ae6f5fe05ff76068)

- 余談: 環境をマップとして定義するのではなく、単に新しいシンボルから順番に名前を調べて探す実装にしたほうが話が早かった気もします。

### 実装: 位置とヒットテスト

`textDocument/documentHighlight` リクエストは、カーソルがある位置の変数の名前ではなく、カーソルの位置 (ソースコード上の位置) しか教えてくれません。その位置に何があるかはサーバー側で調べる必要があります。

カーソル上の位置にあるシンボルを調べる処理を、シンボルのヒットテストと呼ぶことにします。これはトークンの位置情報を使うと可能です。シンボル定義の `definition` や `references` のトークンのどれかがカーソルにかすっていたら、カーソル上にそのシンボルがあるということです。

```ts
const hitTestSymbol = (semanticModel: SemanticModel, position: Position) => {
  // 範囲が指定位置にかすってるかどうか
  const touch = (range: Range) =>
    comparePositions(range.start, position) <= 0
    && comparePositions(position, range.end) <= 0

  for (const symbolDefinition of semanticModel.symbolDefinitions) {
    if (touch(symbolDefinition.definition.range)) {
      return symbolDefinition
    }

    for (const r of symbolDefinition.references) {
      if (touch(r.range)) {
        return symbolDefinition
      }
    }
  }

  return undefined
}
```

`touch` 関数で使っている、位置の大小関係 (前後関係) の比較関数は次のとおりです。もし2つの位置の行番号が違えば、行番号の大小関係がそのまま前後関係です。逆に行番号が同じなら、列の大小関係が前後関係になります。要するに辞書式順序。

```ts
const comparePositions = (l: Position, r: Position) => {
  if (l.line !== r.line) {
    return Math.sign(l.line - r.line)
  }
  return Math.sign(l.character - r.character)
}
```

- [変更点まとめ](https://github.com/vain0x/curage-lang/commit/1e7cc2d5e9e0431525fa50d60def089424a7e882)

### 実装: 解析結果の保存

`textDocument/documentHighlight` リクエストは、解析対象のドキュメントを URI で指定します。ソースコード本体は、 `textDocument/didOpen` や `didChange` で通知されたときのものを記録して、参照することになります。

それらのタイミングで構文解析や上述の解析を行い、その結果をマップか何かに保存しましょう。

- [変更点まとめ](https://github.com/vain0x/curage-lang/commit/2c72aa786af0f0e649319a111ac1d17c927d6a33)
- 注: ファイルが閉じられたときの `didClose` イベントをフックして、マップからエントリーを削除することで、メモリーリークを防ぎます。

### 実装: ハイライトの生成

最後に `textDocument/documentHighlight` へのレスポンスを生成します。ドキュメントをハイライトすべき範囲と、そのハイライトの種類 (定義部分なのか参照部分なのか) というのを指定した `DocumentHighlight` のリストを作ればOK。

```ts
interface DocumentHighlight {
    /** ハイライトする範囲 */
    range: Range;
    /** ハイライトの種類 (DocumentHighlightKind) */
    kind?: number;
}
```

これはヒットテストで得られたシンボル定義の `definition` と `references` を適当に変形すればOK。すでに手札は揃っているという感じですね。

要点だけ抜粋するとこんな感じ:

```ts
  const highlights: DocumentHighlight[] = []
  const { definition, references } = symbolDefinition // ヒットしたシンボル定義

  // 定義位置をハイライト
  highlights.push({
    kind: DocumentHighlightKind.Write,
    range: definition.range,
  })

  for (const r of references) {
    // 参照位置をハイライト
    highlights.push({
      kind: DocumentHighlightKind.Read,
      range: r.range,
    })
  }

  return highlights
```

- [変更点まとめ](https://github.com/vain0x/curage-lang/commit/ac63c6b0e0f00c55096a48920e9957821b5a1549)

## 動作確認

冒頭に貼ったスクリーンショットが動作例になります。

## まとめと次回

今回のポイントは以下の3点でした。

- シンボルを静的解析した
- ヒットテストを実装した
- 格好よくハイライトできて嬉しい

次は、用意したシンボルテーブルをさらに活用して、「名前の変更」を実装します。

- 次回: [LSP学習記 #4 シンボルのリネーム](https://vain0x.github.io/blog/2019-01-16/lsp-trial-04/)

## 余談: シンボル参照の検索について

今回のシンボルテーブルを使うことで、 `textDocument/definition` (定義へのジャンプ) と `textDocument/references` (シンボルの検索) は簡単に実装できると思います。やってみましょう！

## 余談: 用語について

- シンボル (symbol) やセマンティックモデル (semantic model) などの用語は Roslyn API (C#コンパイラ) を参考にしています。
- ヒットテストは「マウスカーソルでクリックしたとき、それがボタンに当たったかどうかを判定する」といった状況で使う動詞なので、今回の用途は微妙かもしれません。
- 「環境」や「シンボルテーブル」といった概念は言語処理系の入門書によく出てきます。

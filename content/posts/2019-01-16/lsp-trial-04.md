---
title: "LSP学習記 #4 シンボルのリネーム"
tags:
- LSP学習記
- 言語処理系
- TypeScript
- LanguageServerProtocol
type: "post"
date: 2019-01-16 22:17:00
permalink: lsp-trial-04
---

自作言語の LSP サーバーを作るプロジェクトの第4回です。今回はソースコードを変更する機能の例として、シンボルのリネームを実装してみました。

- 初回: [LSP学習記 #1](https://vain0x.github.io/blog/2019-01-05/lsp-trial-01/)
- 前回: [LSP学習記 #3 シンボルとハイライト](https://vain0x.github.io/blog/2019-01-10/lsp-trial-03/)
- 今回のソースコード: [curage-lang v0.5.0](https://github.com/vain0x/curage-lang/tree/v0.5.0)

## 名前の変更

変数などの名前を変更するとき、単純な文字列置換では「同名だが異なる変数」といったものまで巻き添えにしてしまいます。安全に変更するには、前回のようにシンボル情報を解析しておくほうがよいです。そういうわけで、LSP サーバーの機能に「名前の変更」があります。

具体的には、 LSP クライアントは [textDocument/rename リクエスト](https://microsoft.github.io/language-server-protocol/specification#textDocument_rename) で、どの位置にあるシンボルをどんな名前に変えるべきかという情報をサーバーに送信してくれます。

このとき、具体的なソースコードの変更点を生成してレスポンスすれば、名前の変更ができるようです。

- 注: 前回と同様に、LSP クライアントからのリクエストをもらうには、サーバーの `capabilities` に `textDocument.renameProvider: true` の指定が必要です。

### 名前の変更: 変更操作の表現

ソースコードに対する変更は `TextEdit` インターフェイスで定義されていて、「ある範囲の文字列を別の文字列で置換する」ような形式です。 `Array.splice` 方式。

例えば次の文字列の範囲 ``[0, 4] .. [0, 5]`` (`x` の部分) を文字列 "new_x" で置換する、みたいな感じです。

```
let x be 1
```

```
let new_x be 1
```

いまのクラゲ言語は1つのファイルにしかソースコードを書けませんが、一般には名前の変更は複数のファイルを変更することになります。`rename` レスポンスで返すべきオブジェクトは、 `WorkspaceEdit` というインターフェイスで定義されていて、ファイルの URI から変更操作へのマップのようなものです。

```typescript
interface WorkspaceEdit {
    // URI から変更操作の配列へのマップ
    changes?: { [uri: string]: TextEdit[]; };

    // 以下略
}
```

## 実装

実装は、前回作ったシンボルテーブルを利用すれば簡単です。

前回は「ドキュメントのハイライトする範囲」を計算しましたが、今回はそれを「名前の変更を適用する範囲」として使えばOK。

```typescript
  const { definition, references } = symbolDefinition // ヒットテストで見つけたシンボル

  // 変更操作の配列
  const textEdits: TextEdit[] = []

  // 定義の置換
  textEdits.push({
    range: definition.range,
    newText: newName,
  })

  for (const r of references) {
    // 参照の置換
    textEdits.push({
      range: r.range,
      newText: newName,
    })
  }

  // WorkspaceEdit インターフェイスに合うオブジェクトを作る
  const changes = { [uri]: textEdits }
  return { changes }
```

- 変更点: [feat: Support symbol renaming](https://github.com/vain0x/curage-lang/commit/603b2c52fe19390a667c25710ad1bcf8af78aaba)

## prepareRename

`textDocument/prepareRename` リクエストという、 `rename` の前に送られてくるリクエストがあります。名前の変更ができない位置 (例えば let キーワードの上) では `prepareName` の結果として `null` を返すことで、名前の変更が不可能であることをクライアントに伝えられる……らしいんですが、実装してみても効果が見られなかったので詳細は略。

- 変更点: [feat: Support 'prepareRename'](https://github.com/vain0x/curage-lang/commit/e91697aed1edd1cd56be54a2c701112aed71e504)

## TextDocumentEdit

LSP の仕様をよく読むと `WorkspaceEdit.changes` ではなく `documentChanges` を使ったほうがよいみたいです。

LSP サーバーが処理をしている間にも、ドキュメントはユーザーによって絶え間なく変更されているので、同じドキュメントにも古いバージョンと新しいバージョンがあります。名前の変更がどのバージョンを処理したのかを指定すると、クライアント側が嬉しいらしいです。

`WorkspaceEdit.documentChanges` には `TextDocumentEdit` (の配列) を指定しますが、これはドキュメントの URI だけでなくバージョンも指定した変更操作を表しています。

- 変更点: [feat: Provide document version in response ](https://github.com/vain0x/curage-lang/commit/a666135fe04e3345bcaef09bc80b8b269d24415f)

## 注意: 安全でない変更

今回の実装では、場合によってはコードの意味を変えてしまいます。例えば次のコードの `x` を `y` という名前に変えると、2つ目の `x` が途中に挟まってる `y` を指すものになってしまいます。これは本来ならユーザーに警告したほうがよいです。

```
let x be 1
let y be 2
let _ be x
```

## 次回

次回は未定です。そろそろ簡単な計算のできる言語にしつつ、入力補完やホバーあたりをやっていこうかと考えています。

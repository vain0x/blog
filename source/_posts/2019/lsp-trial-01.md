---
title: "LSP学習記 #1"
tags:
- 言語処理系
- TypeScript
- LanguageServerProtocol
date: 2019-01-05 23:52:00
permalink: lsp-trial-01
---

LSP サーバーの実装の練習をしています。この記事は勉強ノートとして、調査事項をまとめつつ、成果物を作成した手順を解説します。

第1回では LSP の基礎部分に触れ、極小の LSP サーバーを準備し、「エディター上にリアルタイムで警告を表示する」機能を作ります。

## おことわり

読者には Node.js と TypeScript の基本的な知識を前提とします。

若干解説調の文章になってしまっていますが、筆者は詳しいわけではないのでご了承ください。

## LSP とは

LSP = Language Server Protocol

すごく雑に利点をいうと、いわゆるインテリセンス (ソースコードに警告を出したり入力補完をしたりするやつ) の実装をテキストエディターから分離するのに使えます。

もう少し詳しくは、以下の記事を参考:

- [Overview](https://microsoft.github.io/language-server-protocol/overview)
    - 公式の概要 (英語)
- [language server protocolについて (前編) - Qiita](https://qiita.com/atsushieno/items/ce31df9bd88e98eec5c4)
    - 執筆時点で後編はないみたいです
- [LanguageServerProtocol(LSP)のススメ - Qiita](https://qiita.com/himanoa/items/04a105cc9615e85ad420)
    - 動作例の GIF があって分かりやすいです

VSCode などのエディターが「LSP クライアント」です。一方で言語固有の機能を提供するほうが「LSP サーバー」になります。

## 公式のサンプル

Microsoft のリポジトリに VSCode の拡張機能のサンプル集があります。そのなかに LSP がらみのものが3つあります。次の lsp-sample がもっとも単純です。

[Microsoft/vscode-extension-samples/lsp-sample](https://github.com/Microsoft/vscode-extension-samples/tree/515a928615aaab84ae7f66a38e4346db84464fcb/lsp-sample)

このリポジトリに関するチュートリアル (英語) があります:

- [Language Server Extension Guide | Visual Studio Code Extension API](https://code.visualstudio.com/api/language-extensions/language-server-extension-guide)

次の3つのファイルに注目して中身をみていきます。

- `package.json`
    - パッケージの依存関係やビルドスクリプトだけでなく、VSCode の拡張機能に対するさまざまな設定を含んでいる
- `client/src/extension.ts`
    - VSCode の拡張機能のエントリーポイント
- `server/src/server.ts`
    - LSP サーバーの実装

### サンプル: サンプル言語サーバーの機能

lsp-sample の LSP サーバーは、テキストファイルの編集時に2つの機能を提供するものです。

- ドキュメントの検証
    - 大文字で書かれた単語を警告として報告する
- 入力補完
    - "JavaScript" "TypeScript" を入力補完候補に出す

これらの機能の実装は `server.ts` にあって、VSCode (LSP クライアント) から送られてきたメッセージに応答するという流れになっています。

### サンプル: サンプル言語サーバーの起動

VSCode にこの拡張機能をインストールした状態でプレインテキスト (txt など) を開くと、拡張機能が読み込まれます。

ここでプレインテキストに反応しているのは `package.json` の `activationEvents` (拡張機能を開始する基準のイベント) に `onLanguage:plaintext` と書かれているからのようです。

- [Extension Anatomy | Visual Studio Code Extension API](https://code.visualstudio.com/api/get-started/extension-anatomy)

拡張機能が読み込まれると `extension.ts` の `activate` 関数が呼ばれます。これは `vscode-languageclient` パッケージの機能を使って、LSP クライアントを実行しています。ここで LSP サーバー (`server.ts`) が新しいプロセスとして起動されているようです。

これで無事に通信が確立します。ユーザーの操作に応じて、クライアントがサーバーに必要な処理の要求を発行して、サーバーが応答する、というのが繰り返される。めでたし。

## LSP サーバーをイチから書く

さて、いま作っている最小限の LSP サーバー (もどき) がこちらです:

**[curage-lang v0.1.0](https://github.com/vain0x/curage-lang/tree/v0.1.0)**

lsp-sample との違いは主に3点です。

1つ目は、 LSP サーバーの実装に `vscode-languageserver` パッケージを使っていないこと。

このパッケージを使うと手軽に LSP サーバーが作れて便利そうですが、今回は使いません。後々 Node.js を使わずに言語サーバーを実装したいので、その練習のためです。

2つ目は、 LSP クライアントとサーバーの接続に標準入出力を使っていること。

`lsp/src/extension.ts` の一部が異なります。 lsp-sample では、 LSP サーバーが Node.js で動いてることを前提とする設定で、LSP クライアントを起動しているようです。curage-lang lsp は、標準入出力を使ってサーバーと接続する設定に変えています。これにより、サーバーが Node.js 上で動いてなくてもよくなります。

3つ目は、サーバーとクライアントのパッケージを分けていないこと。分かれているとビルドスクリプト等が複雑化するため。

- `package.json` の中身を調査するためにリポジトリ自体もイチから始めてます。いま思うと回り道だった。lsp-sample から始めたほうがいいです。

## 実装

lsp-sample はここまでにして、自作 LSP サーバーに実装する処理をみていきます。参考:

- Language Server Protocol の公式の仕様: [Specification](https://microsoft.github.io/language-server-protocol/specification)
- JSON RPC 2.0 の公式の仕様: [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)

### 実装: メッセージの受け取り

LSP クライアントからサーバーへのメッセージとして、以下のような文字列が送られます。(改行は `\r\n`)

```
Content-Length: 88

{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "shutdown",
    "params": null
}
```

フォーマットは HTTP と似ています。空行までがヘッダーです。 `Content-Length` ヘッダーは必須で、ボディーの長さ (バイト単位) が指定されます。この例では、後半の JSON がボディーです。

この文字列は、今回はサーバーのプロセスの標準入力に送られてきます。標準入力を受け取るには `process.stdin` の `data` イベントを監視して、送られてくるデータをバッファーにためていけばいいです。

- 公式のサンプルを見ると `readable` イベントと `read` メソッドを使ってました: [Process | Node.js v11.6.0 Documentation](https://nodejs.org/api/process.html#process_process_stdin)

バッファーがたまったらメッセージ単位で切り分けます。これは単純な文字列処理なので詳細は略。

- なお LSP のヘッダーは `Content-Length` 以外にもありますが、ここでは未実装にします。また、エラーが起こったときは JSON RPC の仕様にのっとってエラー情報を返送する必要がありますが、これも後回しにします。
    - このあたり curage-lang lsp は未熟なので、まだ LSP サーバー "もどき" を名乗ったほうがいいかも。

サーバーからクライアントに送信するメッセージも同様の形式です。

### 実装: 通信の開始時

通常、クライアントから最初に送られるメッセージは [initialize リクエスト](https://microsoft.github.io/language-server-protocol/specification#initialize) です。

リクエストのパラメーター (params) として、さまざまな情報が渡されます。いまは必要なさそうなので略。

JSON RPC ではリクエストに対してレスポンスを返す必要があります。

initialize へのレスポンスに載せるデータ (result) は `InitializeResult` インターフェイスで定義されています。ここには LSP サーバーがどの機能を実装しているか (capabilities) を指定します。いまは何も実装できてないので `{}` 。

例えば initialize リクエストがこれなら (実際はかなり長い)、

```
Content-Length: 92

{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": { /* 略 */ }
}
```

initialize レスポンスはこう:

```
Content-Length: 75

{
    "jsonrpc": "2.0",
    "id": 1,
    "result": { "capabilities": {} }
}
```

- 注: レスポンスに method を書かなくていいのは id の値から分かるからだと思います。

この次にクライアントから `initialized` 通知が来ます。いまは無視してOK。

通知 (notification) はリクエストとは違って、レスポンスを返さなくていいメッセージです。

### 実装: 通信の終了時

通信を終了するには、クライアントがサーバーに `shutdown` リクエストを送ります。この時点ではまだサーバーは終了しません。クライアントが `exit` 通知をサーバーに送ったら正常終了です。

### 実装: ソースコードの検証

開始して終了するだけのサーバーができました。そろそろ機能を載せます。

ソースコードが次の文字列と一致しなければ、一致しない部分に警告を出す、という検証機能です。ハローワールドしか書けないプログラミング言語！

```
print "hello, world!"
```

- 変更点: [feat: Trivial source code validation](https://github.com/vain0x/curage-lang/commit/2c463d3bc83a8bcff14782d9ae49cf8db4f9acd5)
- [変更後のリポジトリ](https://github.com/vain0x/curage-lang/tree/v0.2.0)

クライアントで編集されているファイルの情報を得るために、 [initialize レスポンス](https://microsoft.github.io/language-server-protocol/specification#initialize) の `capabilities` に `textDocumentSync` オプションを追加しました。これがあると、LSP の仕様として、クライアントから以下の通知を送ってもらえることになってます。(ここでめっちゃ詰まりました。)

- [`textDocument/didOpen` 通知](https://microsoft.github.io/language-server-protocol/specification#textDocument_didOpen): ファイルが開かれた
- `textDocument/didClose` 通知: ファイルが閉じた
- `textDocument/didChange` 通知: ファイルが変更された

- 注: これはすべてのクライアントが実装する機能とされているので、 initialize リクエストの capabilities (クライアントがどの機能に対応しているか) を読まずに使っていいです。
- 注: ファイルの URI が送られてきますが、これを使ってファイルにアクセスするというものではなく、ファイルを区別するための識別子です。

こうしてファイルが開かれたり変更されるたびにファイルの中身が送られてくるようになったので、検証ができます。

LSP では警告やエラーをまとめて diagnostic と呼び、その種類を severity と呼んでます。diagnostic の範囲は、行番号と列番号を使って指定します。例えば 0 行目 i 列目の文字に問題があるときの警告はこんな感じです:

```ts
    ({
        message: `Expected '${expected[i]}'.`,
        severity: DiagnosticSeverity.Warning,
        range: {
            start: { line: 0, character: i },
            end: { line: 0, character: i + 1 },
        },
    })
```

diagnostics の配列を作って、 [textDocumet/publishDiagnostics 通知](https://microsoft.github.io/language-server-protocol/specification#textDocument_publishDiagnostics) を送ります。

- 注: この通知はドキュメント内の diagnostic をすべて置き換えます。

検証処理の実装は、1文字ずつ比較するだけなので略。

## 動作確認

### 動作確認: VSCode

VSCode は開発中の拡張機能をデバッグできます。

- 注: `.vscode/launch.json` に設定があります。詳細は分かりません。

なんにせよ F5 を押すと VSCode が起動して、ちゃんと LSP サーバーが動きます。めでたし。

![](https://qiita-image-store.s3.amazonaws.com/0/74340/38f57697-5ace-02c9-a8b4-e7831b7a0c54.png)

(VSCode で curage-lang lsp v0.2.0 が動いている様子)

### 動作確認: Sublime Text 3

せっかくなので別の LSP クライアントでも試してみましょう。Sublime Text 3 でやってみます。

駆け足で手順だけ書くと:

- [Sublime Text 3 をインストールする](https://www.sublimetext.com/) (無期限に無償で使えます)
- [Package Control をインストールする](https://packagecontrol.io/installation)
- Package Control で "LSP" をインストールする
- LSP Settings で [LSP クライアントの設定を書く](https://github.com/tomv564/LSP#configuration)
    - `out/server.js` が起動されるようにコマンドを指定しておく。

```json
{
    "clients":
    {
        "curage":
        {
            "command":
            [
                "node",
                "<略>/curage-lang/lsp/out/server.js"
            ],
            "enabled": true,
            "scopes": ["text.plain"],
            "syntaxes": [],
            "languageId": "plaintext"
        }
    }
}
```

- 注: この設定だとあらゆるテキストファイルに関して警告を出してしまうので、何か間違っていそう。

動いている様子はだいたい同じです。

## まとめと次回

今回のポイントは以下の3点でした。

- LSP ではクライアントとサーバーがリクエストとレスポンスを投げ合う (ときどき通知)
- クライアントの通知を受け取れた / クライアントに通知を送れた
- 自作 LSP サーバーが任意のエディタで動いてて楽しい

次はもっとプログラミング言語らしいものを検証して、構文エラーの警告を出せるようにします。そのあと、変数のリネームやシンボル参照の検索などをやっていきたいです。

- 次回: [LSP学習記 #2 クラゲ言語の構文解析](https://qiita.com/vain0x/items/490ae33ba3db3c829c13)

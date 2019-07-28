---
title: '[F#][小ネタ] レコードリテラルと型注釈'
tags:
  - FSharp
  - Tips
type: "post"
date: 2018-03-11 13:06:00
url: 2018-03-11/fsharp-record-literal-and-type-annotation
---

F# のレコードリテラルのちょっとした問題とちょっとした解決策。

<!--more-->

## 問題1: フィールド名が重複しているとき

F# のレコード型を構築する構文では、フィールドの名前から型が推測される。複数のレコード型が同一の名前のフィールドを定義しているとき、そのフィールドは最後に定義されたレコード型のフィールドとみなされる。

- 参考: [Records (F#) | Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/records)

例えば REPL (`fsharpi`) で次のようにすると:

```fsharp
$ fsharpi

// Input
type User =
  {
    Id: int64
    Name: string
  }
;;

type Book =
  {
    Id: int64
    Name: string
  }
;;

{ Id = 1L; Name = "Foo" } ;;
```

このレコードは Book 型に推論されて、

```fsharp
// Output
val it : Book = {Id = 1L;
                 Name = "Foo";}
```

となる。

## 解決1: フィールドを型名で修飾する

この状態で `User` をインスタンス化するには、いずれかのフィールド名を修飾付きで指定すればいい。

```fsharp
// Input
{ User.Id = 1L; Name = "Foo" } ;;
```

```fsharp
// Output
val it : User = {Id = 1L;
                 Name = "Foo";}
```

## 問題2: レコード型がスコープにないとき

レコード型がスコープに入っていないとき、つまりそのレコード型が定義されている module や namespace を open していないとき、レコードリテラルの構文はかなり冗長になる。

```fsharp
$ fsharpi

// Input
module Types =
  type User =
    {
      Id: int64
      Name: string
    }

  type Book =
    {
      Id: int64
      Name: string
    }
;;

module T = Types;;

{ T.User.Id = 1L; T.User.Name = "Foo" } ;;
```

注意点は、 ``T.User.Id`` を見た時点でレコードの型が決定されるにもかかわらず `Name` の修飾を省略できないことだ:

```fsharp
// Input
{ T.User.Id = 1L; Name = "Foo" } ;;
```

```fsharp
// Output
  { T.User.Id = 1L; Name = "Foo" } ;;
  ------------------^^^^

error FS0039: The record label 'Name' is not defined.
```

## 解決2: 型注釈をつける

レコードリテラルの型を明示的に指定すると、非修飾でフィールド名を使えるようだ:

```fsharp
// Input
({ Id = 1L; Name = "Foo" }: T.User) ;;
```

```fsharp
// Output
val it : Types.User = {Id = 1L;
                       Name = "Foo";}
```

束縛時に型を指定してもよい。型名が前に来るので、こちらのほうが読みやすい気がする:

```fsharp
// Input
let user: T.User = { Id = 1L; Name = "Foo" }
user ;;
```

```fsharp
// Output
val it : Types.User = {Id = 1L;
                       Name = "Foo";}
```

さらに、次のように `id` を経由すると型名とリテラルの近接性がより明確になる:

```fsharp
// Input
id<T.User> { Id = 1L; Name = "Foo" } ;;
```

```fsharp
// Output
val it : Types.User = {Id = 1L;
                       Name = "Foo";}
```

これはやりすぎかもしれない、というのも初見では `id` がなんのためにあるのか分からないからだ。

## 修飾の強制

レコード型に ``[<RequireQualifiedAccess>]`` をつかうと、レコード型をスコープに入れてもフィールド名はスコープに入らなくなる。つまり、前述の冗長な構文を使う必要がある……とずっと思っていたが、「型注釈」の方法であれば問題ない。

この属性をつけておくと、フィールド名が重複するかどうか気にしなくてよくなる。重複したフィールド名が後ろに追加されることでレコードリテラルの型が変わることもなくなる。

```fsharp
[<RequireQualifiedAccess>]
type User =
  {
    Id: int64
    Name: string
  }
;;

({ Id = 1L; Name = "Foo" }: User) ;;
```

## まとめ

- フィールド名の重複を避けよう。
- フィールド名が重複しているときは ``型名.フィールド名 = ...`` としよう。
- レコード型がスコープにないときは ``({ ... }: 型名)`` としよう。

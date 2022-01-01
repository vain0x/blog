#!/bin/sh
#if run_with_bin_sh
  exec dotnet fsi --exec $0 $*
#endif

open System
open System.IO

let lastOfMonth (d: DateTime) = d.AddMonths(1).AddDays(float (-d.Day))
let baseDate = lastOfMonth(DateTime.Now.AddDays(-3.0))
let prevDate = (baseDate.AddMonths(-1) |> lastOfMonth).ToString("yyyy-MM-dd")
let currentDate = baseDate.ToString("yyyy-MM-dd")

let filename = $"content/{currentDate}-diary.md"

let contents =
    $"""---
title: 近況 {currentDate}
type: "post"
date: {currentDate}
url: {currentDate}/diary
tags:
  - 日記
---

今月の活動 ()

<!--more-->

- 前回 ({prevDate}) <https://vain0x.github.io/blog/{prevDate}/diary/>


"""

printfn "file: %s" filename
if File.Exists(filename) |> not then
    File.WriteAllText(filename, contents)
else
    printfn "already exists"

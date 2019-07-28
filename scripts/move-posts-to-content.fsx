#!/usr/bin/env fsharpi

open System
open System.IO

for subdir in Directory.GetDirectories("content") do
    match Directory.GetFiles(subdir, "*.md") with
    | [|file|] ->
        let newFilePath = subdir + "-" + Path.GetFileName(file)
        // eprintfn "%s -> %s" file newFilePath
        File.Move(file, newFilePath)
    | _ ->
        eprintfn "skip %s" subdir

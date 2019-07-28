#!/usr/bin/env fsharpi

open System
open System.IO

for filePath in Directory.GetFiles("content", "*.md") do
    let content = File.ReadAllText(filePath)
    let permalink = Path.GetFileNameWithoutExtension(filePath).Substring(11)
    let date =
        content.Split([|'\n'|])
        |> Array.filter (fun line -> line.StartsWith "date: ")
        |> Array.map (fun line -> line.Substring("date: ".Length, 10))
        |> Array.exactlyOne
    let content =
        content.Split([|'\n'|])
        |> Array.map (fun line ->
            if line.StartsWith "permalink: " |> not then
                line
            else
                sprintf "url: %s/%s" date permalink
        )
        |> String.concat "\n"
    // eprintfn "%A" (content.Split([|'\n'|]) |> Array.take 5)
    File.WriteAllText(filePath, content)

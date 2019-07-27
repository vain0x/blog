
open System
open System.IO

for subdir in Directory.GetDirectories("content/posts") do
    let files = Directory.GetFiles(subdir, "*.md")
    for file in files do
        let content = File.ReadAllText(file)
        let name = Path.GetFileNameWithoutExtension(file)
        let date =
            content.Split([|'\n'|])
            |> Array.filter (fun line -> line.StartsWith "date: ")
            |> Array.map (fun line -> line.Substring("date: ".Length, 10))
            |> Array.exactlyOne
        let permalink =
            content.Split([|'\n'|])
            |> Array.filter (fun line -> line.StartsWith "permalink: ")
            |> Array.map (fun line -> line.Substring("permalink: ".Length))
            |> Array.exactlyOne
        let content =
            content.Replace("\ndate: ", "\ntype: \"post\"\ndate: ")
        eprintfn "%s %s %A" file name 0

        let dateDir = sprintf "content/posts/%s" date
        Directory.CreateDirectory(dateDir) |> ignore

        let newFilePath = dateDir + "/" + permalink + ".md"
        File.Move(file, newFilePath)

        File.WriteAllText(newFilePath, content)

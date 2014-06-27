#r @"RamlConverter.dll"
#r @"Newtonsoft.Json.dll"

open Json
open System.IO

type JsonType = | Schema | Example

if fsi.CommandLineArgs.Length < 2
then  
    printf "Please provide raml file path"
else
    let source = fsi.CommandLineArgs.[1]

    let files = Directory.EnumerateFiles(source, "*.json", SearchOption.AllDirectories )

    let (|Suffix|_|) (suffix:string) (s : string) = match s.LastIndexOf(suffix) with | -1 -> None | p -> Some (s.Substring(0, p), s)

    let getError = function | Some(Error s) -> s | _ -> []

    files
    |> Seq.choose(function | Suffix ".example.json" s -> Some (fst s,(Example,snd s)) |  Suffix ".schema.json" s -> Some (fst s, (Schema, snd s)) | _-> None )
    |> Seq.groupBy fst
    |> Seq.map (fun (key,s) ->
        let pair = s |> Seq.map snd |> Map.ofSeq 
        let schemaResult = pair.TryFind Schema |> Option.map (fun name -> File.ReadAllText(name) |> parseSchema name) 
        let schema = schemaResult |> Option.bind(function | Validated s -> Some s | _ -> None)
        [
            yield! getError schemaResult
            yield! pair.TryFind Example 
                   |> Option.map (fun name -> File.ReadAllText(name) |> parseJson schema name) 
                   |> getError
        ]
        )
    |> Seq.concat
    |> Seq.iter (printfn "%s\n")

    


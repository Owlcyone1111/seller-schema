#r @"..\tools\RamlConverter.dll"
#r @"..\tools\Newtonsoft.Json.dll"

open Json
open System.IO

let schemaLocation = @"..\set up\communicationSetup.schema.json"
let source = @"..\set up\communicationSetup.example.json"
let name = "setup"

let getError = function | Error s -> s | _ -> []

let schemaResult = File.ReadAllText(schemaLocation) |> parseSchema name
let schema = match schemaResult with | Validated s -> Some s | _ -> None
let lines = 
    [
        yield! getError schemaResult
        yield! File.ReadAllText(name) |> parseJson schema name |> getError
    ]

System.Console.Write(lines |> String.concat (String.replicate 2 System.Environment.NewLine))

    



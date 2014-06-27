module Json

open Newtonsoft.Json.Schema
open Newtonsoft.Json.Linq
open System.IO
 
let Validate name (jobject : JObject) schema =
    let errors = new ResizeArray<_>()
    let collect _ (args :ValidationEventArgs) = 
        errors.Add <| sprintf "Validation error in '%s': %s Path: %s" name args.Message args.Path
    let handler = new ValidationEventHandler(collect)
    jobject.Validate(schema, handler)    
    if errors.Count = 0 
    then Validated <| jobject.ToString()
    else errors |> List.ofSeq |> Error

let parseSchema name str = 
    try
        JsonSchema.Parse str |> Validated
    with
    | e -> [sprintf "Schema %s failed with %s" name e.Message] |> Error

let parseJson schema name str = 
    try
        let result = JObject.Parse str
        match schema with
        | Some s -> Validate name result s
        | None -> Validated <| result.ToString()
    with
    | e -> [sprintf "Example %s failed with %s" name e.Message] |> Error

let loadFile parser ramlPath (name: string)  =
    if not <| name.TrimStart().StartsWith("!include ") 
    then [sprintf "Error in RAML: resource %s reference is expected to start with '!include '" name] |> Error 
    else
        let cleanName = (name.Substring(9)).TrimStart()
        let dir = Path.GetDirectoryName(ramlPath)
        let json = Path.Combine (dir, cleanName) |> File.ReadAllText
        parser cleanName json
#r @"../tools/Marvel.Fsharp.dll"
#r @"../tools/Marvel.Json.Fsharp.dll"
#r @"../tools/RamlConverter.dll"

open Marvel
open FSharp.Data
open FSharp.Data.JsonExtensions

let path = @"C:\Users\Dmitry\Source\Repos\seller-schema\merchant-skus\merchantSkus-get.schema.json"

open System.IO
let schema = File.OpenRead path |> JsonValue.Load
JsonSchema.Parse Map.empty schema 
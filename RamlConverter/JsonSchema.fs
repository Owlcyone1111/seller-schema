namespace FSharp.Data

open Marvel
open FSharp.Data
open FSharp.Data.JsonExtensions

type JsonSchema =
| String of JsonString
| Number of JsonNumber<decimal>
| Integer of JsonNumber<int>
| Boolean of SchemaElement
| Object of JsonObject
| Array of JsonArray
| Null
and SchemaElement = {description : string option; required: bool; ``default`` : string option; format : string option; enum: string list option  }
and JsonString = { element : SchemaElement; maxLength: int option; minLength : int option; pattern: string option }
and JsonNumber<'d when 'd:comparison> = { element : SchemaElement; minimum: 'd option; maximum : 'd option; exclusiveMinimum : 'd option; exclusiveMaximum : 'd option }
and JsonObject = {element : SchemaElement; title : string; properties : list<string * JsonSchema> }
and JsonArray = { element : SchemaElement; minItems: int option; maxItems : int option; items : JsonSchema; uniqueItems : bool option }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JsonSchema =
    let findProperty conv s = Seq.tryFind (fst >> (=) s) >> Option.map (snd >> conv)

    let rec Parse (definitions : Map<string, JsonSchema>) = function
    | JsonValue.Record properties -> 
        let strp s = findProperty JsonExtensions.AsString s properties
        let intp s = findProperty JsonExtensions.AsInteger s properties
        let decp s = findProperty JsonExtensions.AsDecimal s properties
        let boolp s = findProperty JsonExtensions.AsBoolean s properties
        let arrp s = findProperty (JsonExtensions.AsArray >> Seq.map JsonExtensions.AsString >> List.ofSeq)  s properties
        let schemaList defs s = 
            match findProperty id s properties with
            | Some (JsonValue.Record props) -> props |> Seq.map (fun (p,v) -> p, Parse defs v) |> List.ofSeq |> Some
            | Some x -> failwithf "Expected json object, got: %A" x
            | None -> None
        let el = 
            { 
                description = strp "description" 
                required = boolp "required" |> Option.isNull false
                ``default`` = strp "default" 
                format = strp "format" 
                enum = arrp "enum" 
            }  
        let defs = 
            schemaList definitions "definitions" 
            |> Option.map (fun l -> [definitions |> Seq.map (fun (KeyValue(a,b)) -> a,b); l |> Seq.ofList] |> Seq.concat |> Map.ofSeq)
            |> Option.isNull definitions

        match strp "$ref" with
        | Some s ->
            if s.StartsWith("#/definitions/")
            then 
                let schemaName = s.Split('/') |> Seq.last
                match definitions.TryFind schemaName with
                | Some c -> c
                | None -> failwithf "Can't find schema definition for %s" schemaName
            else
                failwith "Invalid $ref" 
        | None ->
            match strp "type" with      
            | Some "string" -> 
                JsonSchema.String 
                    { 
                    element = el
                    maxLength = intp "maxLength"
                    minLength = intp "minLength" 
                    pattern = strp "pattern"
                }
            | Some "boolean" -> JsonSchema.Boolean el
            | Some "number" ->
                JsonSchema.Number 
                    {
                       element = el
                       minimum = decp "minimum"
                       maximum = decp "maximum"
                       exclusiveMaximum = decp "exclusiveMaximum"
                       exclusiveMinimum = decp "exclusiveMinimum"
                    }
            | Some "integer" ->
                JsonSchema.Integer
                    {
                       element = el
                       minimum = intp "minimum"
                       maximum = intp "maximum"
                       exclusiveMaximum = intp "exclusiveMaximum"
                       exclusiveMinimum = intp "exclusiveMinimum"
                    }
            | Some "object" ->        
                JsonSchema.Object
                    {
                       element = el
                       title = strp "title" |> Option.isNullLazy (fun () -> failwith "Title property is required")
                       properties = schemaList defs "properties" |> Option.isNullLazy (fun () -> failwith "properties are required on the 'object' schema")
                    }
            | Some "array" ->
                JsonSchema.Array
                    {
                        element = el
                        minItems = intp "minItems"
                        maxItems = intp "maxItems"
                        uniqueItems = boolp "uniqueItems"
                        items = findProperty (Parse defs) "items" properties |> Option.isNullLazy (fun () -> failwith "items property is required")
                    }
            | Some x -> failwithf "Type '%s' is invalid" x
            | None -> failwith "'type' property is required"
    | x -> failwithf "Schema is expected to be an object, got: %A" x
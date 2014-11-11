namespace FSharp.Data

open Marvel
open FSharp.Data
open FSharp.Data.JsonExtensions

type SchemaElement = 
    {
        description : string option
        required: bool 
        format : string option
        enum: string list option  
    }

type SchemaType =
| String of JsonString
| Number of JsonNumber<decimal>
| Integer of JsonNumber<int>
| Boolean 
| Object of JsonObject
| Array of JsonArray
| Null
and JsonSchema = SchemaElement * SchemaType
and JsonString = 
    { 
        maxLength: int option 
        minLength : int option 
        pattern: string option 
    } 
and JsonNumber<'d when 'd:comparison> = 
    { 
        minimum: 'd option
        maximum : 'd option
        exclusiveMinimum : 'd option
        exclusiveMaximum : 'd option 
    }
    with 
        static member Parse parser = {
            minimum = parser "minimum"
            maximum = parser "maximum"
            exclusiveMaximum = parser "exclusiveMaximum"
            exclusiveMinimum = parser "exclusiveMinimum"
        }
and JsonObject = 
    {
        title : string 
        properties : list<string * JsonSchema> 
    } 
and JsonArray = 
    { 
        minItems: int option
        maxItems : int option 
        items : JsonSchema 
        uniqueItems : bool option 
    } 

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JsonSchema =
    let inline findProperty conv properties s = properties |> Seq.tryFind (fst >> (=) s) |> Option.map (snd >> conv)
    
    let rec parse (definitions : Map<string, JsonSchema>) = function
    | JsonValue.Record properties -> 
        let inline forProps x = findProperty x properties
        let strp = forProps JsonExtensions.AsString 
        let intp = forProps JsonExtensions.AsInteger
        let decp = forProps JsonExtensions.AsDecimal
        let boolp = forProps JsonExtensions.AsBoolean 
        let arrp = forProps (JsonExtensions.AsArray >> Seq.map JsonExtensions.AsString >> List.ofSeq) 
       
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
            { 
                description = strp "description" 
                required = boolp "required" |> Option.isNull false
                format = strp "format" 
                enum = arrp "enum" 
            },
            match strp "type" with      
            | Some "string" -> 
                SchemaType.String 
                    { 
                    maxLength = intp "maxLength"
                    minLength = intp "minLength" 
                    pattern = strp "pattern"
                }
            | Some "boolean" -> SchemaType.Boolean 
            | Some "number" -> JsonNumber<_>.Parse decp |> SchemaType.Number                  
            | Some "integer" -> JsonNumber<_>.Parse intp |> SchemaType.Integer
            | Some "object" ->        
                SchemaType.Object
                    {
                        title = strp "title" |> Option.isNullLazy (fun () -> failwith "Title property is required")
                        properties = schemaList definitions properties "properties" |> Option.isNullLazy (fun () -> failwith "properties are required on the 'object' schema")
                    }
            | Some "array" ->
                SchemaType.Array
                    {
                        minItems = intp "minItems"
                        maxItems = intp "maxItems"
                        uniqueItems = boolp "uniqueItems"
                        items = findProperty (parse definitions) properties "items"  |> Option.isNullLazy (fun () -> failwith "items property is required")
                    }
            | Some x -> failwithf "Type '%s' is invalid" x
            | None -> failwith "'type' property is required"
    | x -> failwithf "Schema is expected to be an object, got: %A" x
    
    and schemaList defs properties s = 
        match findProperty id properties s with
        | Some (JsonValue.Record props) -> props |> Seq.map (fun (p,v) -> p, parse defs v) |> List.ofSeq |> Some
        | Some x -> failwithf "Expected json object, got: %A" x
        | None -> None       
    
    and  Parse = function
    | (JsonValue.Record properties) as schema -> 
        let defs = schemaList Map.empty properties "definitions" |> Option.map Map.ofList |> Option.isNull Map.empty
        parse defs schema
    | x -> failwithf "Schema is expected to be an object, got: %A" x
#r @"../tools/Marvel.Fsharp.dll"
#r @"../tools/Marvel.Json.Fsharp.dll"
#r @"../tools/RamlConverter.dll"

open Marvel
open Marvel.Validation
open FSharp.Data
open FSharp.Data.JsonExtensions

let fvalidate test err e = if not (test e) then fail [err e] else puree ()
let validate test err = fvalidate (konst test) (konst err) ()
let optionValidate x = Option.map x >> Option.isNullLazy puree 
let validateOptional test err = optionValidate (fvalidate test err)
let collectErrors = traverse id >> function Success _ -> puree () | Failure e -> fail e

let rec Validate path (el,schema) json = 
    let err e = sprintf "%s. Path: %s" e path
    fvalidate (fun _ -> el.required) (sprintf "Required property is missing"
    <* match schema, json with
        | SchemaType.String s, JsonValue.String str -> 
            puree ()
            <* validateOptional (Seq.contains str) (String.concat(",") >> sprintf "Invalid value, should be one of [%s]" >> err) el.enum
            
        | SchemaType.Object s, JsonValue.Record properties -> 
            let props = properties |> Map.ofArray
            let propFinder name = props |> Map.tryFind name |> Option.isNull JsonValue.Null 
            s.properties 
            |> Seq.map (fun (propName, s) -> propFinder propName |> Validate (sprintf "%s/%s" path propName) s) 
            |> collectErrors            
        | x, y -> fail [sprintf "Expected %A, got %A" x y]


let json =  JsonValue.Parse """
{
    "foo": { "bar": "foo" }
}
"""

let schema = JsonSchema.Parse  <| JsonValue.Parse """
{
    "type" : "object",
    "title" : "Something",
    "properties" : {
        "foo" : {
            "type" : "object",
            "title" : "Something",
            "properties" : {
                "bar" : {
                    "$ref" : "#/definitions/bar"
                }
            }
        }
    },
    "definitions" : {                
        "bar"  : {
            "type" : "string",
            "enum" : [ "foo", "bar"],
            "required" : true
        }
    }
}
""" 

Validate "" schema json
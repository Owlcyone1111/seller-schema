#r @"../tools/Marvel.Fsharp.dll"
#r @"../tools/Marvel.Json.Fsharp.dll"

open Marvel
open FSharp.Data
open FSharp.Data.JsonSchema
open Marvel.Validation

let json =  JsonValue.Parse """
{
    "foo": { "bar": "foo", "foo" : 3, "arr" : ["foo", "bar"] }
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
                },
                "foo" : {
                    "type" : "integer",
                    "exclusiveMaximum" : 3,
                    "exclusiveMinimum" : 1
                },
                "arr" : {
                    "type" : "array",
                    "required" : true,
                    "minItems" : 1,
                    "maxItems" : 2,
                    "items" : { "$ref" : "#/definitions/bar"}
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

match Validate schema json with
| Success _ -> printfn "Success!"
| Failure e -> e |> Seq.iter (printfn "%s")
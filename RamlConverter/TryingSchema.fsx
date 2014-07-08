#r @"packages\Newtonsoft.Json.6.0.3\lib\net45\Newtonsoft.Json.dll"

open Newtonsoft.Json.Schema
open Newtonsoft.Json.Linq

let schema = JsonSchema.Parse("""
{
    "type": "object",
    "additionalProperties" : false,
    "properties": {
		"a": {
			"type": "number",
			"
		}
    }
}
""")

//let obj = JObject([| JProperty("product_title", String.replicate 501 "a" )  |])
//let obj = JObject([| JProperty("product_title", "a" ), JProperty("foo", "a" )  |])
//let obj = JObject([| JProperty("bullets", JArray([|  "a"; "asdf"; "asd"; "asd"; "asd"; "asdasd" |]) ) |])
let obj = JObject([| JProperty("a", 1) |])
obj.Validate(schema)


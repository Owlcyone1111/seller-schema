#r @"packages\Newtonsoft.Json.6.0.3\lib\net45\Newtonsoft.Json.dll"

open Newtonsoft.Json.Schema
open Newtonsoft.Json.Linq

let schema = JsonSchema.Parse("""
{
    "type": "object",
    "additionalProperties" : false,
    "properties": {
		"bullets": {
			"type": "array",
			"items": {
				"type": "string",
				"maxLength" : 500
			},
			"maxItems" : 5,
			"description": "merchant sku feature description

			#Valid Values

			maximum of 500 characters each

			#array limitation

			maximum 5 elements of the 'bullets' array"
		}
    }
}
""")

//let obj = JObject([| JProperty("product_title", String.replicate 501 "a" )  |])
//let obj = JObject([| JProperty("product_title", "a" ), JProperty("foo", "a" )  |])
//let obj = JObject([| JProperty("bullets", JArray([|  "a"; "asdf"; "asd"; "asd"; "asd"; "asdasd" |]) ) |])

obj.Validate(schema)


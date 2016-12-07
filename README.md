# Partner API Schema and Tools

Partner API schema is a dialect of [Json Schema](http://json-schema.org/). It was implemented as Jet-specific replacement for excellent [Json.NET Schema](http://www.newtonsoft.com/jsonschema)

List of differences from standard and limitations:

* `exclusiveMinimum` and `exclusiveMaximum` are decimals and not booleans, they can be used instead of combination, e.g., `exclusiveMaximum = 5` instead of `exclusiveMaximum = true, maximum = 5`
* Cross-references in `definitions` are not allowed 
* `string` type supports `format` with following values
   * `date-time` - Date is expected to be in ISO 8601 format `yyyy-MM-ddTHH:mm:ss.fffffff-HH:MM`
   * `uri` -  Absolute URI
   * One of the standard codes: `UPC, UPC-E, ISBN-10, ISBN-13, EAN, GTIN-14`
* `string` type supports `validate` field, which might be used by schema validation implementation for XSS sanitizing and such. Note that custom validation will be ignored if either `format` or `enum` is specified
* `enum` is supported only for `string` type
* `additionalProperties` is `false` by default 
* `title` and `properties` are required in `object` definition

Please visit https://developer.jet.com for more information on particular schemas.


[<AutoOpen>]
module Ast

open Newtonsoft.Json.Schema
open Newtonsoft.Json.Linq

type Resource<'a> = | Validated of 'a | Error of string list

type Schema = Resource<JsonSchema> option
type Json = Resource<string> option

type Api = {
    title : string
    description : string
    baseUrl : string
    resources : Resource list
}
and 
    Resource = {
    path : string
    title : string option
    methods : Method list
}
and 
    Method = {
    name : string
    request: Request
    responses: Response list
}
and 
    Request = {
    parameters : Parameter list
    example : Json 
    schema : Schema 
    description : string list            
    }
and 
    Response = {
    status : int
    parameters : Parameter list
    example : Json 
    schema : Schema 
    description : string list
}
and 
    Parameter = {
    name : string
    description : string option
}

let collectErrors api = 
    let getError str = function | Some(Error s) -> s |> List. map (sprintf "%s : %s" str) |> Some | _ -> None
    [
        for r in api.resources do 
        for m in r.methods do
            let descr = sprintf "%s - %s" r.path m.name 
            yield getError m.name m.request.example 
            yield getError m.name m.request.schema 
            for response in m.responses do
                let descr = sprintf "%s(%d)" descr response.status 
                yield getError descr response.example
                yield getError descr response.schema
    
    ] 
    |> List.choose id
    |> List.concat

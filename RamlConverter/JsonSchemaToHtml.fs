module JsonSchemaToHtml

open System
open System.Xml.Linq
open Newtonsoft.Json.Schema

let (|ArrayType|_|) (schema : JsonSchema) =
    if  schema.Type.HasValue && 
        schema.Type.Value = JsonSchemaType.Array && 
        schema.Items <> null && 
        schema.Items.Count > 0 
    then schema.Items.[0] |> Some 
    else None

let otherTypes schema =
    let rec loop (schema : JsonSchema) = 
        seq {
            yield schema
            if schema.Properties <> null then
                for KeyValue(name, prop) in schema.Properties do
                    match prop, prop.Type.Value with
                    | ArrayType typ, _ -> yield! loop typ
                    | _, JsonSchemaType.Object -> 
                        if prop.Title <> null then
                            yield prop
                            yield! loop prop
                    | _ -> ()         
            } 
    loop schema |> Seq.skip 1 |> Seq.distinctBy(fun s -> s.Title) 
    
let removeEmptyLines ss =
    let rec loop acc = function | ""::rest -> loop (acc + 1) rest  | x -> acc,x
    loop 0 ss

let (|EmptyLines|_|)  ss = 
    match removeEmptyLines ss with 
    | 0, _ -> None
    | _, rest -> Some(element "br" [], rest)

let (|SpecialPrefix|_|) what (ss : string list) = 
    match ss with 
    | s::tail when s.StartsWith(what) -> Some (s.Substring(what.Length), tail) 
    | _ -> None

let (|Bullets|_|) ss =     
    let rec listLoop acc = function 
        | SpecialPrefix "*" (line,rest) -> 
            let el = element "li" [text line]
            listLoop (el::acc) rest 
        | EmptyLines (_,rest) -> listLoop acc rest
        | _ as rest -> (acc, rest)
    match listLoop [] ss with
    | [], _ -> None
    | acc, rest -> Some(element "ul" acc, rest) 

let (|Header|_|) ss =
    match ss with
    | SpecialPrefix "#" (header, rest) -> 
        let h = element "h5" [ header.TrimStart('#') |> text ]
        Some (h, removeEmptyLines rest |> snd)
    | _ -> None
    
let convertMarkdown (description : string seq) =     
    let rec loop acc = function
        | EmptyLines (el,rest) -> loop (el::acc) rest 
        | Header (e, rest) -> loop (e::acc) rest 
        | Bullets (lines, rest) -> loop (lines::acc) rest
        | s::tail -> loop ((text s)::acc) tail
        | [] -> acc |> List.rev 

    description
    |> Seq.map (fun s -> s.Trim()) |> List.ofSeq    
    |> loop [] 

let code x = element "code" [text x]

let div cls values =  
    element "div" [
        yield attribute "class" cls
        yield! values
    ]

let row name typ (description : string) =
    div "row" [
        yield attribute "style" "padding-bottom: 25px;"
        yield div "col-md-3" [ code name ]    
        yield div "col-md-1" [ code typ ]    
        if not <| String.IsNullOrWhiteSpace description then
            yield div "col-md-8" [ description.Split('\n') |> convertMarkdown |> element "article" ]
    ]

let parseProperty (KeyValue(name, prop: JsonSchema)) = 
    let typ = match prop with 
              | ArrayType t -> 
                let itemType = 
                    if String.IsNullOrWhiteSpace(t.Title) 
                    then t.Type.ToString() 
                    else t.Title
                sprintf "%s []" itemType
              | _ -> prop.Type.Value.ToString()
    let name = if prop.Required.HasValue && prop.Required.Value then name + "*" else name
    row name typ prop.Description

let parameters values = div "parameters" values

let convertJson (schema : Schema) = 
    match schema with
    | Some (Error _)| None -> []
    | Some (Validated s) ->
        [
            yield element "h4" [text "Body"]
            yield  s.Properties |> Seq.map parseProperty |> parameters
            for typ in otherTypes s do
                if typ.Properties <> null then
                    yield element "h5" [text typ.Title]
                    yield  typ.Properties |> Seq.map parseProperty |> parameters
        ]
    

let article text schema =     
    let innards = [
            yield! convertMarkdown text
            yield! convertJson schema
        ]
    match innards with
    | [] -> None
    | _ -> (element "article" innards).ToString() |> Some

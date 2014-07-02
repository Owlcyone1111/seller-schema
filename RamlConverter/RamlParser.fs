module RamlParser 

open System
open System.Text.RegularExpressions

type token = { num : int; ident : int; name : string; body : string }

let group prefix = 
    Seq.skipWhile (prefix >> not)
    >> Seq.scan(fun (acc,ident) t -> 
            let newIdent = if ident = -1 then t.ident else ident
            if prefix t && t.ident = newIdent then [t],newIdent else t::acc,newIdent 
        ) ([], -1)
    >> Seq.map fst
    >> Seq.filter (List.isEmpty >> not)
    >> Seq.map List.rev
    >> Seq.groupBy List.head
    >> Seq.map (fun (key, s) -> key, s |> Seq.maxBy List.length |> List.tail )
    
let withName name t = t.name.Equals(name, StringComparison.InvariantCultureIgnoreCase)

let unwrapBody t = t.body

let tryFindByName name = List.tryFind (withName name) >> Option.map unwrapBody

let (|TryFind|_|) =  Map.tryFind 

let parseDescription = function
    | TryFind "description" body -> List.map unwrapBody body
    | _ -> []


let matchParams = Regex(@"\{([^/]+)\}")

let urlParams url =     
    matchParams.Matches(url)
    |> Seq.cast<Match>
    |> Seq.map (fun c -> 
        { 
            Parameter.name = c.Groups.[1].Value
            description = None
        })
    |> List.ofSeq

let queryParams = function
    | TryFind "queryParameters" body ->        
        [
            for t, parts in group (fun _ -> true) body do
                yield { 
                        Parameter.name = t.name
                        description = tryFindByName "description" parts
                      }
        ]
    | _ -> []

let parseParameters resourceName parts = 
    (urlParams resourceName) @ queryParams parts   
    
    
let nameInList l t = Seq.exists(fun m -> withName m t) l

let methodParts tokens = 
    let isMethodPart = nameInList ["responses"; "body"; "description"; "queryParameters"] 
    group isMethodPart tokens 
    |> Seq.map (fun (k,s) -> k.name, s) 
    |> Map.ofSeq

let parseBody ramlPath parts =    
    let loadJson name body parser = tryFindByName name body |> Option.map (Json.loadFile parser ramlPath)
    match parts with
    | TryFind "body" body -> 
        let schema = loadJson "schema" body Json.parseSchema
        let example = 
            schema 
            |> Option.bind (function | Validated s -> Some s | _ -> None) 
            |> Json.parseJson 
            |> loadJson "example" body 
        example, schema
    | _ -> None, None

let parseResponses ramlPath resourceName responses = 
    let isResponse t = t.name |> Seq.forall Char.IsDigit
    [
        for r, innards in group isResponse responses do
            let parts = methodParts innards 
            let example, schema =  parseBody ramlPath parts
            yield {
                status = Int32.Parse(r.name)
                parameters = parseParameters resourceName parts
                description = parseDescription parts 
                example = example
                schema = schema
            }
    ]

let parseMethods ramlPath resourceName innards = 
    let isMethod = "get,post,put,head,delete,trace,connect,options".Split(',') |> nameInList 
    [
        for m, innards in group isMethod innards do
            let parts = methodParts innards 
            let example, schema = parseBody ramlPath parts
            match parts with
            | TryFind "responses" responses ->                        
                yield {
                    Method.name = m.name
                    request = 
                        {
                            parameters = parseParameters resourceName parts
                            description = parseDescription parts 
                            example = example
                            schema = schema
                        }
                    responses = parseResponses ramlPath resourceName responses                
                }        
            | _ -> ()
    ]

let isResource t = t.name.StartsWith("/")

let parseResources ramlPath tokens = [
        for resource, innards in group isResource tokens do
            yield {
                path = resource.name
                title = tryFindByName "displayName" innards
                methods = parseMethods ramlPath resource.name innards
            }
    ]

let parse (lines : string []) = 
    let calculateResources l t = 
        let res = if isResource t then 
                    match l |> List.tryFind (fun (p, ident) -> isResource p && ident < t.ident) with 
                    | Some (x, _) -> {t with token.name = x.name + t.name; ident = 0 }
                    | None -> t
                   else t
        (res,t.ident)::l
    let getToken (pos, id, line : string) = 
        let ident = line |> Seq.takeWhile Char.IsWhiteSpace |> Seq.length
        let name = line.Substring(ident, pos - ident)
        { 
            num  = id
            ident = ident
            name = name
            body = line.Substring(pos + 1).TrimStart()                
        }
    lines
    |> Seq.mapi( fun id line -> line.IndexOf(":"), id, line)
    |> Seq.filter( fun (pos,_,_) -> pos > 0 )
    |> Seq.map getToken
    |> Seq.fold calculateResources []
    |> Seq.map fst
    |> List.ofSeq
    |> List.rev

let parseAst ramlPath =    
    let tokens = System.IO.File.ReadAllLines(ramlPath) |> parse
    let root name = 
        match tokens |> List.tryFind (fun t-> t.ident = 0 && withName name t) with 
        | Some t -> t.body 
        | None -> ""
    let ast = {
        Api.title = root "title" 
        description = root "description" 
        baseUrl = root "baseUri" 
        resources = parseResources ramlPath tokens
    }
    match collectErrors ast with
    | [] -> Validated ast
    | errors -> Error errors 


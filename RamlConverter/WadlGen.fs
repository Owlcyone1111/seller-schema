module WadlGen

open System.Xml.Linq
open Ast

let defaultNamespace = XNamespace.Get("http://wadl.dev.java.net/2009/02")

let attribute name value = XAttribute(XName.Get name, value) :> XObject

let element name (children : XObject seq) = XElement(defaultNamespace + name, children) :> XObject

let text value = XText(value:string) :> XObject

let doc description = element "doc" [ text description ]
let docTitle description = element "doc" [ attribute "title" description ]

let representation example = 
    match example with
    | Some(Validated s) ->
        [ 
            element "representation" [
                attribute "mediaType" "application/json"
                doc s
                ]
        ]
    | _ -> []

let getMethod (m: Method) =
    element "method" [
        yield attribute "name" m.name
        yield element "request" [
            let description = JsonSchemaToHtml.article m.request.description m.request.schema
            if description.IsSome then yield doc description.Value
            for p in m.request.parameters do
            yield element "param"[
                yield attribute "name" p.name
                yield attribute "style" "template"
                yield attribute "type" "xs:string"
                yield attribute "required" "true"
                if p.description.IsSome then yield doc p.description.Value
            ]
            yield! representation m.request.example             
        ]
        for response in m.responses do
        yield element "response" [
            yield attribute "status" response.status
            let description = JsonSchemaToHtml.article response.description response.schema 
            if description.IsSome then yield doc description.Value
            yield! representation response.example 
        ]
    ]

let getResource resource = 
    match resource.methods with
    | [] -> None
    | methods ->
        Some <| element "resource" [
                    yield attribute "path" resource.path
                    if resource.title.IsSome then yield docTitle resource.title.Value
                    for m in methods -> getMethod m
                ]

open System.IO

let toXml (api : Api) outputFileName = 
    let xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance")
    let xml = 
        element "application" [
            XAttribute(XNamespace.Xmlns + "xml", "http://www.w3.org/XML/1998/namespace")
            XAttribute(XNamespace.Xmlns + "xs", "http://www.w3.org/2001/XMLSchema")
            XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName)
            XAttribute(xsi + "schemaLocation", "http://wadl.dev.java.net/2009/02 http://www.w3.org/Submission/wadl/wadl.xsd")
            element "doc" [ 
                attribute "title" api.title
                text api.description
                ]
            element "resources" [
                yield attribute "base" api.baseUrl        
                yield! api.resources |> List.choose getResource 
            ]
        ] 
    let doc = XDocument(XDeclaration("1.0", "utf-8", "yes") , box xml)
    if not <| File.Exists outputFileName 
    then File.WriteAllText(outputFileName, "")
    doc.Save(outputFileName)
    

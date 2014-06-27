[<AutoOpen>]
module XmlHelper

open System.Xml.Linq
open System

let element name (children : XObject seq) = XElement(XName.Get name, children) :> XObject

let attribute name (value : string) = XAttribute(XName.Get name, value) :> XObject

let text (value: string) = 
    let v = if String.IsNullOrWhiteSpace(value) then "" else value
    XText(v) :> XObject
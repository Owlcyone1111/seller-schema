#r @"RamlConverter.dll"

open System.IO

let source =
    if fsi.CommandLineArgs.Length < 2
    then  __SOURCE_DIRECTORY__ + @"..\..\..\seller-schema\merchant-skus\merchant-skus.raml"
    else fsi.CommandLineArgs.[1]

let target = 
    if fsi.CommandLineArgs.Length < 3
    then  Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source) + ".wadl")
    else fsi.CommandLineArgs.[2]

match RamlParser.parseAst source with
| Validated api -> 
    printfn "Generating %s" target 
    WadlGen.toXml api target 
| Error errors -> 
    printfn "Errors encountered:" 
    errors |>  Seq.iter (printfn "%s")

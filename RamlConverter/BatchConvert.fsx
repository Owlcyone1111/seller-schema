#r @"bin\Debug\RamlConverter.dll"

open System.IO
open System

let source = if fsi.CommandLineArgs.Length < 2 then __SOURCE_DIRECTORY__ + @"\.." else fsi.CommandLineArgs.[1]

for file in Directory.EnumerateFiles(source, "*.raml", SearchOption.AllDirectories ) do    
    String.replicate 20 "_" |> printfn "%s"
    printfn "Processing %s" file
    let target = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".wadl")
    WadlGen.processRaml file target


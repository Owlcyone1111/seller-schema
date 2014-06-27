#r @"bin\Debug\RamlConverter.dll"

open System.IO
if fsi.CommandLineArgs.Length < 2
then  
    printf "Please provide raml file path"
else    
    let source = fsi.CommandLineArgs.[1]

    let target = 
        if fsi.CommandLineArgs.Length < 3
        then  Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source) + ".wadl")
        else fsi.CommandLineArgs.[2]

    match RamlParser.parseAst source with
    | Validated api -> 
        printfn "Generating %s" target 
        WadlGen.toXml api target 
    | Error errors -> 
        printfn "Can't generate, errors encountered:" 
        errors |>  Seq.iter (printfn "%s")

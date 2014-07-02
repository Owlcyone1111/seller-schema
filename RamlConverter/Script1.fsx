#r @"bin\Debug\RamlConverter.dll"

open System.IO

let ast = RamlParser.parseAst @"C:\Users\Dmitry\Source\Repos\seller-schema\merchant-skus\merchant-skus.raml"
    
match  ast with
| Validated api -> 
    WadlGen.toXml api @"C:\Users\Dmitry\Source\Repos\seller-schema\merchant-skus\merchant-skus.wadl"
| Error errors -> 
    printfn "Can't generate, errors encountered:" 
    errors |>  Seq.iter (printfn "%s")


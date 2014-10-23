#I "../tools/"
#r @"RamlConverter.dll"

open System.IO

let path = @"C:\Users\Dmitry\Source\Repos\seller-schema\settlement\settlement.raml"
let ast = RamlParser.parseAst path
    
match  ast with
| Validated api -> 
    WadlGen.toXml api (path.Replace(".raml", ".wadl"))
| Error errors -> 
    printfn "Can't generate, errors encountered:" 
    errors |>  Seq.iter (printfn "%s")


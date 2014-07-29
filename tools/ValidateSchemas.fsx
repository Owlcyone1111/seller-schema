#r @"..\tools\RamlConverter.dll"
#r @"..\tools\Newtonsoft.Json.dll"

open Json
open System.IO

type JsonType = | Schema | Example

if fsi.CommandLineArgs.Length < 2
then  
    printf "Please provide raml file path"
else
    let lines = fsi.CommandLineArgs.[1] |> Json.VerifyAll
    
    System.Console.Write(lines |> String.concat (String.replicate 2 System.Environment.NewLine))

    File.WriteAllLines("errors.txt", lines)

    


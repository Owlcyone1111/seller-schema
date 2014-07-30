#r @"..\tools\RamlConverter.dll"

open Json
open System.IO

let path = __SOURCE_DIRECTORY__ + @"\..\set up\" 
let lines = Json.VerifyAll path
  

    
System.Console.Write(lines |> String.concat (String.replicate 2 System.Environment.NewLine))

File.WriteAllLines("errors.txt", lines)

    


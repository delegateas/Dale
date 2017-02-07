namespace Dale

open System
open FSharp.Data

module JsonRedactor =

  let redactJson (propertiesToSkip :Set<string>) json =

    let rec redact json =
      match json with
      | JsonValue.String _ -> "REDACTED" |> JsonValue.String
      | JsonValue.Number _ -> "0" |> Decimal.Parse |> JsonValue.Number
      | JsonValue.Float _ -> "0" |> Double.Parse |> JsonValue.Float
      | JsonValue.Boolean _  | JsonValue.Null -> json
      | JsonValue.Record props -> 
          props 
          |> Array.map (fun (key, value) -> 
              key,
              if propertiesToSkip.Contains key then redact value 
              else value)
          |> JsonValue.Record
      | JsonValue.Array array -> 
          array 
          |> Array.map redact 
          |> JsonValue.Array
    
    redact json
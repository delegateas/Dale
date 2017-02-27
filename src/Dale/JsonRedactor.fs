namespace Dale

open System
open FSharp.Data
open System.Text.RegularExpressions

module JsonRedactor =

  let (|RegExp|_|) pattern input =
    if isNull input then None
    else
      let m = Regex.Match(input, pattern, RegexOptions.Compiled)
      if m.Success then Some [for x in m.Groups -> x]
      else None

  let fullRedaction v =
      match v with
      | JsonValue.String _ -> "REDACTED" |> JsonValue.String
      | JsonValue.Number _ -> "0" |> Decimal.Parse |> JsonValue.Number
      | JsonValue.Float _ -> "0" |> Double.Parse |> JsonValue.Float
      | JsonValue.Boolean _  | JsonValue.Null -> v
      | _ -> v


  let partialRedaction v =

    let redact j =
      match j with
      | RegExp @"^(.*)/(.*)\.(.*)$)" [ path; _; extension ] ->
          path.Value + "/REDACTED." + extension.Value |> JsonValue.String
      | RegExp @"^(.*)\.(.*)$" [ _; extension ] ->
          "REDACTED" + extension.Value |> JsonValue.String
      | _ -> j |> JsonValue.String

    match v with
    | JsonValue.String s -> redact s
    | _ -> v


  let redactJson censorfn (propertiesToSkip :Set<string>) json =

    let rec redact fn json =
      match json with
      | JsonValue.Record props ->
          props
          |> Array.map (fun (key, value) ->
              key,
              if propertiesToSkip.Contains key then redact fn value
              else value)
          |> JsonValue.Record
      | JsonValue.Array array ->
          array
          |> Array.map (redact fn)
          |> JsonValue.Array
      | any -> fn any

    redact censorfn json

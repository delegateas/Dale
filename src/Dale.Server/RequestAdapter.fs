namespace Dale.Server

module RequestAdapter =

  open System.Net.Http
  open System.Net.Http.Headers
  open Suave

  let netHeaders (suaveReqHeaders :(string*string) list) (headers :HttpRequestHeaders) =
    suaveReqHeaders |> List.iter headers.Add
    headers

  let httpMethod (suaveHttpMethod : Suave.Http.HttpMethod) =
    match suaveHttpMethod with
    | OTHER _ ->
      None
    | _ ->
      let m : System.String = suaveHttpMethod |> string
      let normalized = m.ToLowerInvariant()
      string(System.Char.ToUpper(normalized.[0])) + normalized.Substring(1)
    |> HttpMethod |> Some

  let httpRequestMessage (suaveReq : HttpRequest) =
    let req = new HttpRequestMessage()
    netHeaders suaveReq.headers req.Headers |> ignore
    req.RequestUri <- suaveReq.url
    req.Content <- new ByteArrayContent(suaveReq.rawForm)
    req.Method <-
     defaultArg (httpMethod suaveReq.method) (new System.Net.Http.HttpMethod("Unknown"))
    req

namespace Dale

module Middleware =
  open System.Net
  open System.Net.Http
  open FSharp.Data
  open Dale.Http

  type Middleware = Handler -> Handler

  let methodAllowed (inner :Handler) :Handler =
    (fun req ->
      match req.Method.ToString() with
        | "POST" -> inner req
        | _ -> new HttpResponseMessage(HttpStatusCode.MethodNotAllowed))

  let isValidation (inner :Handler) :Handler =
    (fun req ->
      match req.Headers.Contains "Webhook-ValidationCode" with
        | true -> new HttpResponseMessage(HttpStatusCode.OK)
        | false -> inner req)

  let isWellFormed (inner :Handler) :Handler =
    (fun req ->
      let body = req.Content.ReadAsStringAsync().Result
      let json = Notifications.Parse body
      let valid =
        json
        |> Seq.map (fun i -> not (isNull i.ContentUri))
        |> Seq.reduce (&&)
      match valid with
        | true -> inner req
        | false -> new HttpResponseMessage(HttpStatusCode.BadRequest))

  let auditHandler (req :HttpRequestMessage) :HttpResponseMessage =
    let res = queueBatches req
    new HttpResponseMessage(HttpStatusCode.OK)

  let wrappedAuditHandler :Handler =
    auditHandler
    |> isWellFormed
    |> isValidation
    |> methodAllowed

  let interopHandler req =
    wrappedAuditHandler req

namespace Dale

module Http =
  open System
  open System.IO
  open System.Net
  open System.Net.Http
  open FSharp.Data
  open FSharp.Data.HttpRequestHeaders
  open Dale.Storage

  type Handler = HttpRequestMessage -> HttpResponseMessage
  exception ExportError of string

  let collectBatches(req :HttpRequestMessage) =
    let body = req.Content.ReadAsStringAsync().Result
    let json = JsonValue.Parse body
    json.AsArray()
    |> Seq.map (fun i -> {Uri = i.GetProperty("contentUri").AsString();
                          Ttl = i.GetProperty("contentExpiration").AsDateTime()})

  let fetchAuthToken =
    let tenant = Environment.GetEnvironmentVariable("Tenant")
    let clientId = Environment.GetEnvironmentVariable("ClientId")
    let clientSecret = Environment.GetEnvironmentVariable("ClientSecret")
    let url = "https://login.microsoftonline.com/" + tenant + "/oauth2/token"
    let resp =
      Http.Request(url, 
                   headers=[Accept "application/json"],
                   body=FormValues ["grant_type", "client_credentials";
                                    "client_id", clientId;
                                    "client_secret", clientSecret;
                                    "resource", "https://manage.office.com"])
    let getRespText body =
      match body with
      | Text t -> t
      | _ -> ""
    let json = JsonValue.Parse (getRespText resp.Body)
    match resp.StatusCode with
    | 200 -> Some (json.GetProperty("access_token").AsString())
    | _ -> None

  let fetchBatch oauthToken url =
    let json =
      Http.RequestString(url, httpMethod = "GET",
                         headers = [Authorization ("Bearer " + oauthToken)])
    JsonValue.Parse json

  let mapToAuditWrites (json) =
    match json with
    | None -> None
    | Some (j :JsonValue) ->
        Some (j.AsArray()
        |> Seq.map (fun e ->
                     let k = e.GetProperty("CreationTime").AsDateTime().ToShortDateString()
                     {UserId = e.GetProperty("UserId").ToString();
                      AuditEvent =
                        {ServiceType = e.GetProperty("Workload").ToString();
                         Id   = e.GetProperty("Id").ToString();
                         Time = e.GetProperty("CreationTime").ToString();
                         Json = e.ToString() }}))

  let queueBatches (req :HttpRequestMessage) =
    let batches = collectBatches req
    queueContentToAzure batches

  let doExport (batch :string) =
    let token = fetchAuthToken
    let fetchBatchWithToken = fun (batch :string) ->
      match token with
      | Some t -> Some (fetchBatch t batch)
      | None -> None 

    batch
    |> fetchBatchWithToken
    |> mapToAuditWrites
    |> writeToAzure

  let doExportWithException batch =
    let res = doExport batch
    match res with
    | Some r -> r 
    | None -> raise (ExportError("Unable to persist Audit Events"))
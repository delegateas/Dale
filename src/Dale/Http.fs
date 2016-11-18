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
    |> Array.map (fun i ->
                   {Uri = i.GetProperty("contentUri").AsString();
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

    let toAuditWrite (e :JsonValue) =
      let dt = e.GetProperty("CreationTime").AsString().Split [|'T'|]
      { UserId = e.GetProperty("UserId").ToString();
        AuditEvent =
         { ServiceType = e.GetProperty("Workload").AsString();
           Id   = e.GetProperty("Id").AsString();
           Date = Seq.head dt;
           Time = Seq.last dt;
           ObjectId = e.GetProperty("ObjectId").AsString();
           Operation = e.GetProperty("Operation").AsString();
           Result = e.GetProperty("ResultStatus").AsString();
           Json = e.ToString() }}

    match json with
    | None -> None
    | Some (j :JsonValue) -> Some (j.AsArray() |> Array.map toAuditWrite)

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

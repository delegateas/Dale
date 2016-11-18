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

  [<Literal>]
  let NotifySchema = """[
    {"tenantId": "{00000000-0000-0000-0000-000000000000}",
      "clientId": "{00000000-0000-0000-0000-000000000000}",
      "contentType": "Audit.SharePoint",
      "contentId": "492638008028$492638008028$f28ab78ad40140608012736e37393...",
      "contentUri": "https://manage.office.com/api/v1.0/...",
      "contentCreated": "2015-05-23T17:35:00Z",
      "contentExpiration": "2015-05-30T17:35:00Z"
    }]"""

  type Notifications = JsonProvider<NotifySchema>

  let collectBatches (req :HttpRequestMessage) =
    let body = req.Content.ReadAsStringAsync().Result
    let json = Notifications.Parse body
    json
    |> Seq.map (fun i -> {Uri = i.ContentUri; Ttl = i.ContentExpiration})

  let fetchAuthToken =
    let tenant = Environment.GetEnvironmentVariable("Tenant")
    let clientId = Environment.GetEnvironmentVariable("ClientId")
    let clientSecret = Environment.GetEnvironmentVariable("ClientSecret")
    let url = "https://login.microsoftonline.com/" + tenant + "/oauth2/token"
    let json =
      Http.RequestString(url, 
                         headers=[Accept "application/json"],
                         body=FormValues ["grant_type", "client_credentials";
                                         "client_id", clientId;
                                         "client_secret", clientSecret;
                                         "resource", "https://manage.office.com"])
    let res = JsonValue.Parse json
    res.GetProperty("access_token").AsString()

  let fetchBatch oauthToken url =
    let json =
      Http.RequestString(url, httpMethod = "GET",
                         headers = [Authorization ("Bearer " + oauthToken)])
    JsonValue.Parse json

  let mapToAuditWrites (json :JsonValue) :seq<AuditWrite> =
    json.AsArray()
    |> Seq.map (fun e ->
                 let k = e.GetProperty("CreationTime").AsDateTime().ToShortDateString()
                 {UserId = e.GetProperty("UserId").ToString();
                  AuditEvent =
                    {ServiceType = e.GetProperty("Workload").ToString();
                     Id   = e.GetProperty("Id").ToString();
                     Time = e.GetProperty("CreationTime").ToString();
                     Json = e.ToString() }})

  let queueBatches (req :HttpRequestMessage) =
    let batches = collectBatches req
    queueContentToAzure batches

  let doExport (batch :string) =
    let token = fetchAuthToken
    let fetchBatchWithToken = fun (batch :string) -> fetchBatch token batch
    let results =
      batch
      |> fetchBatchWithToken
      |> mapToAuditWrites
      |> writeToAzure
    printfn "%A" results

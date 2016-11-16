namespace Dale

module Http =
  open System
  open System.IO
  open System.Net
  open System.Net.Http
  open FSharp.Data
  open FSharp.Data.HttpRequestHeaders
  open Microsoft.IdentityModel.Clients.ActiveDirectory
  open Dale.Storage

  type Handler = HttpRequestMessage -> Async<HttpResponseMessage>

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
    |> Seq.map (fun i -> i.ContentUri)

  let fetchAuthToken =
    let url = "https://login.windows.net/" +
              Environment.GetEnvironmentVariable("Tenant")
    let ctx = new AuthenticationContext(url);
    let cid = Environment.GetEnvironmentVariable("ClientId")
    let secret = Environment.GetEnvironmentVariable("ClientSecret")
    let cred = new ClientCredential(cid, secret)
    let res = ctx.AcquireTokenAsync("https://manage.office.com", cred).Result;
    res.AccessToken

  let fetchBatch oauthToken url =
    let json =
      Http.RequestString(url, httpMethod = "GET")
                         headers = [Accept "application/json"
                                    Authorization ("Bearer " + oauthToken)])
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

  let doExport (batches :seq<string>) =
    async {
      let token = fetchAuthToken
      let fetchBatchWithToken = fun (batch :string) -> fetchBatch token batch
      let results =
        batches
        |> Seq.map fetchBatchWithToken
        |> Seq.map mapToAuditWrites
        |> Seq.map writeToAzure
      printfn "%A" results
    }


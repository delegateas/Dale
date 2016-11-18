#r "./packages/FSharp.Data/lib/portable-net45+netcore45/FSharp.Data.dll"
#r "./packages/FSharp.Data/lib/portable-net45+netcore45/FSharp.Data.DesignTime.dll"

open System
open FSharp.Data
open FSharp.Data.HttpRequestHeaders

let tenant = "contoso.onmicrosoft.com"
let tenantId = "my-tenant-id"
let clientId = "my-client-id"
let clientSecret = "my-client-secret"
let url = "https://my-webhook-app.com/api/webhook"
let authId = "my-auth-id"

let fetchAuthToken =
  let url = "https://login.microsoftonline.com/" + tenant + "/oauth2/token"
  let json =
    Http.RequestString(url, httpMethod = "POST",
                       headers = [Accept "application/json"]
                       body = FormValues ["grant_type", "client_credentials";
                                          "client_id", clientId;
                                          "client_secret", clientSecret;
                                          "resource", "https://manage.office.com"])
  let res = JsonValue.Parse json
  res.GetProperty("access_token")

printfn "%s" "Begin subscription..."

let token = fetchAuthToken

let subscribe contentType =
  let url = "https://manage.office.com/api/v1.0/" + tenantId +
            "/activity/feed/subscriptions/start"
  let reqbody =
    String.Format("""{"webhook": {"address": "{0}", "authId": "{1}, "expiration": ""}}""", url, authId)

  printfn "%s %s %s" "Sending Subscription Request for" contentType "..."
  let status =
    Http.RequestString(url, httpMethod = "POST",
                       headers = [Accept "application/json"
                                  Authorization ("Bearer " + token)]
                       query = ["contentType", contentType]
                       body = reqbody) |> ignore
  printfn "%s" status
  ()

subscribe "Audit.SharePoint"
subscribe "Audit.Exchange"
subscribe "Audit.AzureActiveDirectory"
subscribe "Audit.General"
printfn "%s" "Subscription completed."

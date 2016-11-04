namespace Dale

module Storage =
  open System
  open FSharp.Azure.Storage.Table
  open Microsoft.WindowsAzure.Storage
  open Microsoft.WindowsAzure.Storage.Table

  type UserId = string

  type AuditEvent =
    { [<PartitionKey>] Time :string
      [<RowKey>] Id :string
      Json :string }

  type AuditWrite =
    { UserId :UserId
      AuditEvent :AuditEvent }

  let userTableName (userId :string) :string =
    let cand =
      userId.Split [| '@' |]
      |> Seq.head
      |> Seq.filter (Char.IsLetterOrDigit)
      |> String.Concat
    "Audit" + cand

  let writeToAzure (events :seq<AuditWrite>) =
    async {
      let conn = Environment.GetEnvironmentVariable("AzureConnectionString")
      let account = CloudStorageAccount.Parse conn
      let tableClient = account.CreateCloudTableClient()
      events
      |> Seq.map (fun e ->
                   let tableName = userTableName e.UserId
                   let inUserTable entry = inTable tableClient tableName entry
                   e.AuditEvent |> Insert |> inUserTable) |> ignore
      ()
    }

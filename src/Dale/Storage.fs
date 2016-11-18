namespace Dale

module Storage =
  open System
  open FSharp.Azure.Storage.Table
  open Microsoft.WindowsAzure.Storage
  open Microsoft.WindowsAzure.Storage.Table
  open Microsoft.WindowsAzure.Storage.Queue

  type UserId = string

  type AuditEvent =
    { [<PartitionKey>] Time :string
      [<RowKey>] Id :string
      ServiceType :string
      Json :string }

  type AuditWrite =
    { UserId :UserId
      AuditEvent :AuditEvent }

  type QueuedContent =
    { Uri :string
      Ttl :DateTime }

  let fetchAuditQueue =
    let conn = Environment.GetEnvironmentVariable("AzureConnectionString")
    let account = CloudStorageAccount.Parse conn
    let queueClient = account.CreateCloudQueueClient()
    queueClient.GetQueueReference("dale-auditeventqueue")

  let queueContentToAzure (uris :seq<QueuedContent>) =
    let queue = fetchAuditQueue
    let _ = queue.CreateIfNotExists()
    // Azure Cloud Queue TTL cannot exceed 7 days
    let max = new TimeSpan(7, 0, 0, 0)
    uris
    |> Seq.iter (fun u ->
                  let t = u.Ttl.Ticks - DateTime.UtcNow.Ticks
                  let ttl = Math.Min(t, max.Ticks)
                  let n = new Nullable<TimeSpan>(new TimeSpan(ttl))
                  let msg = new CloudQueueMessage(u.Uri)
                  queue.AddMessage(msg, n))

  let userTableName (userId :string) :string =
    let cand =
      userId.Split [| '@' |]
      |> Seq.head
      |> Seq.filter (Char.IsLetterOrDigit)
      |> String.Concat
    "Audit" + cand

  let writeToAzure (events :seq<AuditWrite>) =
    let conn = Environment.GetEnvironmentVariable("AzureConnectionString")
    let account = CloudStorageAccount.Parse conn
    let tableClient = account.CreateCloudTableClient()
    events
    |> Seq.map (fun e ->
                 let tableName = userTableName e.UserId
                 let table = tableClient.GetTableReference(tableName)
                 table.CreateIfNotExists() |> ignore
                 let inUserTable entry = inTable tableClient tableName entry
                 e.AuditEvent |> Insert |> inUserTable)

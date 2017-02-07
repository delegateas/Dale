namespace Dale

module Storage =
  open System
  open FSharp.Azure.Storage.Table
  open Microsoft.WindowsAzure.Storage
  open Microsoft.WindowsAzure.Storage.Table
  open Microsoft.WindowsAzure.Storage.Queue
  open FSharp.Data

  type UserId = string

  type ContentUri = string

  type Content =
    { ContentUri :ContentUri
      Json :JsonValue }

  type AuditEvent =
    { [<PartitionKey>] Date :string
      [<RowKey>] Id :string
      ServiceType :string
      Operation :string
      Status :string
      Time :string
      ObjectId :string
      Json :string }

  type AuditWrite =
    { UserId :UserId
      AuditEvent :AuditEvent }

  type QueuedContent =
    { Uri :string
      Ttl :DateTime }

  let fetchAccount connString =
    CloudStorageAccount.Parse connString


  let fetchAuditQueue (account :CloudStorageAccount) queueName =
    let queueClient = account.CreateCloudQueueClient()
    queueClient.GetQueueReference(queueName)

  let queueContentToAzure (queue :CloudQueue) uris =
    // Azure Cloud Queue TTL cannot exceed 7 days
    let max = new TimeSpan(7, 0, 0, 0)
    uris
    |> Array.filter (fun e -> e.Ttl > DateTime.UtcNow)
    |> Array.iter (fun u ->
                    let t = Math.Max(0L, u.Ttl.Ticks - DateTime.UtcNow.Ticks)
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

  let writeToAzureTable account events =

    let write (account :CloudStorageAccount) (ev :AuditWrite[]) = 
      let tableClient = account.CreateCloudTableClient()
      ev
      |> Array.map (fun e ->
                     let tableName = userTableName e.UserId
                     let table = tableClient.GetTableReference(tableName)
                     table.CreateIfNotExists() |> ignore
                     let inUserTable entry = inTable tableClient tableName entry
                     e.AuditEvent |> Insert |> inUserTable)

    match events with
    | Some (ev :AuditWrite[]) -> Some (write account  ev) 
    | None -> None

  let dumpBlob (account :CloudStorageAccount) (content :Option<Content>) =

    let azureIO (f :Blob.CloudBlockBlob, s) =
      try
        Some (f.UploadText(s))
      with
      | ex -> printfn "Exception! %s " (ex.Message); None 

    let dump (c :Content) =
      let j = c.Json
      let blobClient = account.CreateCloudBlobClient()
      let container = blobClient.GetContainerReference("audit-blob-dump")
      container.CreateIfNotExists() |> ignore
      let uri = new Uri(c.ContentUri);
      let name = uri.Host + uri.PathAndQuery;
      let file = container.GetBlockBlobReference(name);
      match azureIO(file, j.ToString()) with
      | Some _ -> Some j
      | None -> None

    match content with
    | Some (c :Content) -> dump c
    | None -> None

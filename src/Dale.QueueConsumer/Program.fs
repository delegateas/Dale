namespace Dale.QueueConsumer

module Main =
  open System
  open System.IO
  open Microsoft.Azure.WebJobs
  open Dale

  type Functions() =
    let processQueueMessage ([<QueueTrigger("queue")>] message :string) (log :TextWriter) =
      log.WriteLine("Processing batch : " + message);
      let conf = Config.Create(
                   Environment.GetEnvironmentVariable("APPSETTING_Tenant"),
                   Environment.GetEnvironmentVariable("APPSETTING_ClientId"),
                   Environment.GetEnvironmentVariable("APPSETTING_ClientSecret"),
                   Environment.GetEnvironmentVariable("APPSETTING_AzureConnectionString"),
                   Environment.GetEnvironmentVariable("APPSETTING_AzureQueueName"),
                   Environment.GetEnvironmentVariable("APPSETTING_RedactedFields"),
                   Environment.GetEnvironmentVariable("APPSETTING_PartiallyRedactedFields"))

      let exporter = Exporter(conf)
      exporter.ExportWithException(message)
      |> Seq.map(fun s -> log.WriteLine(s))

  [<EntryPoint>]
  let main argv =
    let config = new JobHostConfiguration()
    if config.IsDevelopment then
        config.UseDevelopmentSettings()
    let host = new JobHost()
    host.RunAndBlock()
    0

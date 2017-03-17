namespace Dale.Server

module Config =
  open System

  let LoadConfigValue v =
    let appsettings = new Configuration.AppSettingsReader()
    let ret =
      match Environment.GetEnvironmentVariable("APPSETTING_" + v) with
      | null -> match Environment.GetEnvironmentVariable(v) with
                | null -> appsettings.GetValue(v, typeof<string>).ToString()
                | s -> s
      | s -> s
    match ret with
    | null -> String.Empty
    | s -> s

  let DaleConf : Dale.Configuration =
    { Tenant = LoadConfigValue "Tenant";
      ClientId = LoadConfigValue "ClientId";
      ClientSecret = LoadConfigValue "ClientSecret";
      AzureConnectionString = LoadConfigValue "AzureConnectionString";
      AzureQueueName = LoadConfigValue "AzureQueueName";
      RedactedFields = (LoadConfigValue "RedactedFields").Split(',')
                       |> Set.ofArray;
      PartiallyRedactedFields = (LoadConfigValue "PartiallyRedactedFields").Split(',')
                                |> Set.ofArray; }

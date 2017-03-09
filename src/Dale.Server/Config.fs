namespace Dale.Server

[<AutoOpen>]
module Config =
  open System
  open FSharp.Configuration

  type Settings = AppSettings<"app.config">
  [<Literal>]
  let private Acs = "AzureConnectionString"
  [<Literal>]
  let private Aqn = "AzureConnectionString"
  [<Literal>]
  let private Tnt = "Tenant"
  [<Literal>]
  let private Cid = "ClientId"
  [<Literal>]
  let private Csc = "ClientSecret"
  [<Literal>]
  let private Rdf = "RedactedFields"
  [<Literal>]
  let private Prf = "PartiallyRedactedFields"

  let private env =
    Environment.GetEnvironmentVariables()
    |> Seq.cast<System.Collections.DictionaryEntry>
    |> Seq.map (fun e -> e.Key :?> string, e.Value :?> string)
    |> dict

  Settings.Tenant <-
    if env.ContainsKey(Tnt) then env.Item(Tnt) else Settings.Tenant
  Settings.AzureConnectionString <-
    if env.ContainsKey(Acs) then env.Item(Acs) else Settings.AzureConnectionString
  Settings.ClientId <-
    if env.ContainsKey(Cid) then env.Item(Cid) else Settings.ClientId
  Settings.ClientSecret <-
    if env.ContainsKey(Csc) then env.Item(Csc) else Settings.ClientSecret
  Settings.AzureQueueName <-
    if env.ContainsKey(Aqn) then env.Item(Aqn) else Settings.AzureQueueName
  Settings.RedactedFields <-
    if env.ContainsKey(Rdf) then env.Item(Rdf) else Settings.RedactedFields
  Settings.PartiallyRedactedFields <-
    if env.ContainsKey(Prf) then env.Item(Prf) else Settings.PartiallyRedactedFields

  let DaleConf : Dale.Configuration =
    { Tenant = Settings.Tenant;
      ClientId = Settings.ClientId;
      ClientSecret = Settings.ClientSecret;
      AzureConnectionString = Settings.AzureConnectionString;
      AzureQueueName = Settings.AzureQueueName;
      RedactedFields = Set.ofArray (Settings.RedactedFields.Split(','));
      PartiallyRedactedFields = Set.ofArray (Settings.PartiallyRedactedFields.Split(',')) }

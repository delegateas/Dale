namespace Dale

type Configuration =
  { Tenant :string
    ClientId :string
    ClientSecret :string
    AzureConnectionString :string
    AzureQueueName :string
    RedactedFields :Set<string>
    PartiallyRedactedFields :Set<string> }

module Config =
  let Create(tenant,clientid,clientsecret,conn,queueName,(fieldsCSV :string),(partialFieldsCSV :string)) =
    { Tenant = tenant;
      ClientId = clientid;
      ClientSecret = clientsecret;
      AzureConnectionString = conn;
      AzureQueueName = queueName;
      RedactedFields = Set.ofArray (fieldsCSV.Split(','));
      PartiallyRedactedFields = Set.ofArray (partialFieldsCSV.Split(',')) }

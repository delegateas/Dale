namespace Dale

module Interop =
  open Dale.Http
  open Dale.Middleware

  let doExportWithException batch =
    let res = doExport batch
    match res with
    | Some r -> (r |> Array.map(sprintf "%A")) 
    | None -> raise (ExportError("Unable to persist Audit Events."))

  let handler req =
    wrappedAuditHandler req
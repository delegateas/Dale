namespace Dale.Server

module Main =
  open System.IO
  open Suave
  open Suave.Logging
  open Suave.Operators
  open Suave.Successful
  open Suave.Filters
  open Suave.RequestErrors
  open RequestAdapter
  open Config

  [<EntryPoint>]
  let main args =

    let port =  System.UInt16.Parse args.[0]
    let ip = System.Net.IPAddress.Parse "127.0.0.1"
    let serverConfig =
      { Web.defaultConfig with
          homeFolder = Some (Path.GetFullPath "./static")
          logger = Targets.create Info [||]
          bindings = [ HttpBinding.create HTTP ip port ] }


    let exporter = new Dale.Exporter(DaleConf)


    let webpart (req :HttpRequest) =
      let wrapper = httpRequestMessage req
      Async.RunSynchronously (async {
        let! resp = exporter.AsyncHandler wrapper
        let content =
          match resp.Content with
          | null -> ""
          | _ -> resp.Content.ReadAsStringAsync()
                 |> Async.AwaitTask
                 |> Async.RunSynchronously
        match resp.IsSuccessStatusCode with
        | true -> return OK ""
        | false -> return BAD_REQUEST content
      })


    let app :WebPart =
      choose [
        path "/" >=> GET >=> Files.file "static/index.html"
        path "/api/notify" >=>
          POST >=> request webpart
        GET >=> Files.browseHome
        NOT_FOUND "Resource not found."
      ]
    startWebServer serverConfig app
    0

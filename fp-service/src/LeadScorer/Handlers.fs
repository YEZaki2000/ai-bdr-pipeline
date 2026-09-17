module LeadScorer.Handlers

open Giraffe
open Microsoft.AspNetCore.Http
open System.Text.Json

// ============================================================
//  HTTP HANDLERS — Giraffe routing
// ============================================================

let private jsonOptions =
    let opts = JsonSerializerOptions()
    opts.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    opts

/// POST /score — hoofdendpoint voor n8n
let scoreHandler : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        try
            let! req = ctx.BindJsonAsync<Validation.LeadRequest>()

            match Validation.processRequest req with
            | Ok response ->
                ctx.SetStatusCode 200
                return! json response next ctx

            | Error errResponse ->
                ctx.SetStatusCode 422  // Unprocessable Entity
                return! json errResponse next ctx

        with ex ->
            let err = {| valid = false; errors = [| $"Parse fout: {ex.Message}" |] |}
            ctx.SetStatusCode 400
            return! json err next ctx
    }

/// GET /health — voor Railway/Fly.io health checks
let healthHandler : HttpHandler =
    fun next ctx -> task {
        let resp = {| status = "ok"; service = "lead-scorer"; version = "1.0.0" |}
        return! json resp next ctx
    }

/// GET / — info endpoint
let infoHandler : HttpHandler =
    fun next ctx -> task {
        let resp = {|
            service     = "AI Lead Scorer — type-safe microservice"
            endpoints   = [| "POST /score"; "GET /health" |]
            description = "Type-safe F# lead scoring service. Valideert en scoort leads voor n8n automation flows."
        |}
        return! json resp next ctx
    }

// ============================================================
//  ROUTER
// ============================================================

let webApp : HttpHandler =
    choose [
        GET  >=> route "/"       >=> infoHandler
        GET  >=> route "/health" >=> healthHandler
        POST >=> route "/score"  >=> scoreHandler
        setStatusCode 404 >=> text "Route niet gevonden"
    ]

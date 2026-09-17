module LeadScorer.Program

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe

// ============================================================
//  STARTUP
// ============================================================

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services
        .AddGiraffe()
        .AddCors(fun opts ->
            opts.AddDefaultPolicy(fun policy ->
                policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                |> ignore))
    |> ignore

    let app = builder.Build()

    app.UseCors()  |> ignore
    app.UseGiraffe Handlers.webApp

    printfn "🚀 Lead Scorer draait op poort 8080"
    app.Run("http://0.0.0.0:8080")
    0

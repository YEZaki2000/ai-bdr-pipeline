module LeadScorer.Validation

open Domain
open System.Text.Json.Serialization

// ============================================================
//  RAW INPUT — wat n8n stuurt (ongevalideerd)
// ============================================================

[<CLIMutable>]
type LeadRequest = {
    [<JsonPropertyName("name")>]    Name    : string
    [<JsonPropertyName("email")>]   Email   : string
    [<JsonPropertyName("company")>] Company : string
    [<JsonPropertyName("budget")>]  Budget  : decimal
    [<JsonPropertyName("message")>] Message : string
}

// ============================================================
//  OUTPUT — wat we terugsturen naar n8n
// ============================================================

type ScoreResponse = {
    [<JsonPropertyName("score")>]      Score      : string
    [<JsonPropertyName("reasoning")>]  Reasoning  : string
    [<JsonPropertyName("confidence")>] Confidence : float
    [<JsonPropertyName("valid")>]      Valid      : bool
}

type ErrorResponse = {
    [<JsonPropertyName("valid")>]   Valid  : bool
    [<JsonPropertyName("errors")>]  Errors : string list
}

// ============================================================
//  VALIDATION PIPELINE
//  Combineert alle validatiefouten in één Result.
//  n8n ziet ofwel een geldig ScoreResponse ofwel
//  een ErrorResponse met alle problemen tegelijk.
// ============================================================

let private scoreToString = function
    | Hot  -> "Hot"
    | Warm -> "Warm"
    | Cold -> "Cold"

/// Valideer een ruwe request naar een Lead domain object
let validate (req: LeadRequest) : Result<Lead, string list> =
    let emailResult   = Email.create   (req.Email   ?? "")
    let budgetResult  = Budget.create  req.Budget
    let messageResult = NonEmptyText.create (req.Message ?? "")

    // Verzamel ALLE fouten tegelijk (niet stoppen bij eerste fout)
    let errors =
        [ match emailResult   with Error e -> yield e | _ -> ()
          match budgetResult  with Error e -> yield e | _ -> ()
          match messageResult with Error e -> yield e | _ -> () ]

    match errors with
    | [] ->
        // Alle validaties geslaagd — bouw het Lead domain object
        Ok {
            Name    = (req.Name    ?? "Onbekend").Trim()
            Email   = emailResult   |> Result.defaultWith (fun _ -> failwith "unreachable")
            Company = (req.Company ?? "").Trim()
            Budget  = budgetResult  |> Result.defaultWith (fun _ -> failwith "unreachable")
            Message = messageResult |> Result.defaultWith (fun _ -> failwith "unreachable")
        }
    | errs -> Error errs

/// Verwerk een volledige request naar een response
let processRequest (req: LeadRequest) : Result<ScoreResponse, ErrorResponse> =
    match validate req with
    | Error errors ->
        Error { Valid = false; Errors = errors }
    | Ok lead ->
        let result = Scoring.scoreLead lead
        Ok {
            Score      = scoreToString result.Score
            Reasoning  = result.Reasoning
            Confidence = System.Math.Round(result.Confidence, 2)
            Valid      = true
        }

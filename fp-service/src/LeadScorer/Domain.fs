module LeadScorer.Domain

// ============================================================
//  DOMAIN TYPES
//  Ongeldige data kan hier niet bestaan — de compiler
//  dwingt correctheid af vóór runtime.
// ============================================================

/// E-mail als aparte type — niet zomaar een string
type Email = private Email of string

module Email =
    let create (raw: string) =
        let trimmed = raw.Trim().ToLowerInvariant()
        if System.String.IsNullOrWhiteSpace(trimmed) then
            Error "E-mail mag niet leeg zijn"
        elif not (trimmed.Contains("@")) then
            Error $"Ongeldig e-mailadres: {trimmed}"
        elif trimmed.Length > 254 then
            Error "E-mail te lang (max 254 tekens)"
        else
            Ok (Email trimmed)

    let value (Email v) = v

/// Budget als positief getal — negatief is onmogelijk
type Budget = private Budget of decimal

module Budget =
    let create (raw: decimal) =
        if raw < 0m then
            Error $"Budget kan niet negatief zijn: {raw}"
        else
            Ok (Budget raw)

    let zero = Budget 0m
    let value (Budget v) = v

/// Bericht dat niet leeg kan zijn
type NonEmptyText = private NonEmptyText of string

module NonEmptyText =
    let create (raw: string) =
        let trimmed = (raw ?? "").Trim()
        if System.String.IsNullOrWhiteSpace(trimmed) then
            Error "Bericht mag niet leeg zijn"
        elif trimmed.Length > 5000 then
            Error "Bericht te lang (max 5000 tekens)"
        else
            Ok (NonEmptyText trimmed)

    let value (NonEmptyText v) = v

// ============================================================
//  LEAD — alleen geldig als alle velden kloppen
// ============================================================

type Lead = {
    Name    : string
    Email   : Email
    Company : string
    Budget  : Budget
    Message : NonEmptyText
}

// ============================================================
//  SCORE — uitkomst van de classificatie
// ============================================================

type LeadScore =
    | Hot
    | Warm
    | Cold

type ScoreResult = {
    Score     : LeadScore
    Reasoning : string
    Confidence: float   // 0.0 – 1.0
}

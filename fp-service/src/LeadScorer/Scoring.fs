module LeadScorer.Scoring

open Domain

// ============================================================
//  SCORING REGELS
//  Pure functies: zelfde input = altijd zelfde output.
//  Makkelijk te testen, makkelijk uit te leggen aan klanten.
// ============================================================

let private urgencyKeywords = [
    "zo snel mogelijk"; "urgent"; "asap"; "deze week"
    "morgen"; "vandaag"; "spoed"; "direct"; "onmiddellijk"
    "deadline"; "kritiek"; "noodgeval"
]

let private hasUrgency (message: string) =
    let lower = message.ToLowerInvariant()
    urgencyKeywords |> List.exists (fun kw -> lower.Contains(kw))

let private hasBigCompanySignal (message: string) (company: string) =
    let combined = (message + " " + company).ToLowerInvariant()
    let signals = ["bv"; "nv"; "groep"; "holding"; "international"; "enterprise"; "corporate"]
    signals |> List.exists (fun s -> combined.Contains(s))

// Bereken een confidence score op basis van hoeveel signalen aanwezig zijn
let private confidence (signals: bool list) : float =
    let trueCount = signals |> List.filter id |> List.length
    float trueCount / float signals.Length

// ============================================================
//  HOOFD-SCORING FUNCTIE
//  Discriminated unions maken alle cases expliciet —
//  je kunt geen case vergeten (compiler waarschuwt).
// ============================================================

let scoreLead (lead: Lead) : ScoreResult =
    let budget  = Budget.value lead.Budget
    let message = NonEmptyText.value lead.Message
    let isUrgent       = hasUrgency message
    let isBigCompany   = hasBigCompanySignal message lead.Company
    let isHighBudget   = budget >= 3000m
    let isMediumBudget = budget >= 500m && budget < 3000m
    let hasSpecificQ   = message.Length > 100

    match isHighBudget, isUrgent, isBigCompany with
    | true, _, _  ->
        { Score      = Hot
          Reasoning  = $"Budget €{budget} overschrijdt drempelwaarde €3000"
          Confidence = confidence [isHighBudget; isUrgent; isBigCompany] }

    | _, true, _ ->
        { Score      = Hot
          Reasoning  = "Urgente taal gedetecteerd in bericht"
          Confidence = confidence [isUrgent; isBigCompany; hasSpecificQ] }

    | _, _, true ->
        { Score      = Hot
          Reasoning  = "Grote organisatie gedetecteerd op basis van bedrijfsnaam/bericht"
          Confidence = confidence [isBigCompany; isMediumBudget; hasSpecificQ] }

    | false, false, false when isMediumBudget ->
        { Score      = Warm
          Reasoning  = $"Budget €{budget} valt binnen Warm-range (€500–3000)"
          Confidence = confidence [isMediumBudget; hasSpecificQ] }

    | false, false, false when hasSpecificQ ->
        { Score      = Warm
          Reasoning  = "Gedetailleerde vraag wijst op serieuze interesse"
          Confidence = confidence [hasSpecificQ; isMediumBudget] }

    | _ ->
        { Score      = Cold
          Reasoning  = "Geen duidelijke budget, urgentie of bedrijfssignalen aanwezig"
          Confidence = 0.6 }


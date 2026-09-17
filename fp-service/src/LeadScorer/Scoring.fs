module LeadScorer.Scoring

open LeadScorer.Domain

let calculateScore (lead: LeadInput) =
    let isUrgent = 
        not (System.String.IsNullOrWhiteSpace(lead.Message)) 
        && lead.Message.ToLower().Contains("urgent")

    if lead.Budget >= 5000m || isUrgent then
        "Hot", 0.95, "Hoog budget of dringende behoefte gedetecteerd."
    elif lead.Budget >= 1000m then
        "Warm", 0.80, "Gemiddeld budget gedetecteerd."
    else
        "Cold", 0.60, "Laag budget zonder urgente vraag."

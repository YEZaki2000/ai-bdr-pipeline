module LeadScorer.Domain

type LeadInput = {
    Name: string
    Email: string
    Company: string
    Budget: decimal
    Message: string
}

type ValidationResult = {
    Valid: bool
    Score: string
    Confidence: float
    Reasoning: string
    Errors: string list
}

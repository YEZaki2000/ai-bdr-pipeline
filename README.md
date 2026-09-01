# AI BDR Pipeline

Een intelligente Business Development Representative automation gebouwd met n8n en een type-safe F# validatieservice.

Deze pipeline automatiseert het volledige BDR-werkproces: lead verrijking, gepersonaliseerde AI-outreach, follow-up sequenties, CRM-logging en dagelijkse samenvattingen — zonder handmatig werk na de installatie.

---

## Wat dit onderscheidt

De meeste n8n-automations stoppen alle bedrijfslogica in JavaScript Code nodes.
Deze pipeline scheidt de verantwoordelijkheden:

- **n8n** orkestreert de flow
- **F# microservice** verwerkt validatie en lead scoring met type-veiligheid
- **OpenAI GPT-4o-mini** schrijft gepersonaliseerde outreach per lead

Ongeldige data (ontbrekend e-mailadres, negatief budget, leeg bericht) wordt op type-niveau afgevangen voordat het ooit je CRM of e-mailverzender bereikt.

---

## Architectuur

```
Nieuwe lead in Google Sheets
        ↓
Lead verrijking (Hunter.io)
        ↓
F# service — type-safe validatie + scoring
        ↓
OpenAI — gepersonaliseerde outreach genereren
        ↓
IF: Hot / Warm / Cold
        ↓
Gmail — eerste e-mail versturen
        ↓
3 dagen wachten
        ↓
Controleren op reactie
        ↓ geen reactie
OpenAI — follow-up e-mail
        ↓
Gmail — follow-up versturen
        ↓
CRM updaten (Airtable)
        ↓
Dagelijkse Slack-samenvatting (gepland op 08:00)
```

---

## Stack

| Laag | Tool | Waarom |
|---|---|---|
| Orkestratie | n8n (self-hosted) | Volledige controle, geen operatielimieten, exporteerbaar als JSON |
| Validatie | F# + Giraffe | Type-safe — ongeldige leads kunnen de e-mailverzender niet bereiken |
| AI | OpenAI GPT-4o-mini | Snel, goedkoop, betrouwbare JSON-output |
| Lead verrijking | Hunter.io | Bedrijfsdata op basis van e-maildomein |
| E-mail | Gmail via OAuth2 | Native n8n-integratie |
| CRM | Airtable | Eenvoudig, visueel, makkelijk over te dragen aan niet-technische klanten |
| Meldingen | Slack | Realtime hot lead-alerts + dagelijkse samenvatting |

---

## Repositorystructuur

```
ai-bdr-pipeline/
├── README.md
├── n8n-flows/
│   ├── bdr-main-flow.json            # Hoofdpipeline — importeer in n8n
│   └── bdr-daily-summary.json        # Geplande dagelijkse rapportage
├── fp-service/
│   ├── src/LeadScorer/
│   │   ├── Domain.fs                 # Type-definities
│   │   ├── Scoring.fs                # Pure scoringfuncties
│   │   ├── Validation.fs             # Validatiepipeline
│   │   ├── Handlers.fs               # HTTP-handlers (Giraffe)
│   │   └── Program.fs                # Opstarten
│   └── Dockerfile
├── prompts/
│   ├── outreach-prompt.md            # Promptsjabloon eerste e-mail
│   └── followup-prompt.md            # Promptsjabloon follow-up
└── docs/
    ├── architecture.md               # Gedetailleerde architectuurkeuzes
    └── setup.md                      # Stapsgewijze installatiehandleiding
```

---

## Installatie

### Vereisten

- n8n self-hosted (Docker)
- .NET 8 SDK (voor F# service)
- OpenAI API-sleutel
- Hunter.io API-sleutel (gratis tier)
- Google-account (Sheets + Gmail)
- Slack-werkruimte

### 1. Repository klonen

```bash
git clone https://github.com/JOUW_GEBRUIKERSNAAM/ai-bdr-pipeline
cd ai-bdr-pipeline
```

### 2. F# service deployen

```bash
cd fp-service
# Deployen op Railway (aanbevolen)
# Verbind je GitHub-repo op railway.app
# Railway detecteert automatisch de Dockerfile
```

Kopieer je Railway deployment-URL — je hebt die nodig in stap 4.

### 3. n8n-workflow importeren

In n8n: Menu → Workflows → Import from file → selecteer `n8n-flows/bdr-main-flow.json`

### 4. Credentials instellen

Open elke node met `VERVANG_MET_JOUW_*` en voeg toe:

| Credential | Waar te vinden |
|---|---|
| OpenAI API-sleutel | platform.openai.com → API keys |
| Hunter.io API-sleutel | hunter.io → Settings → API |
| Google OAuth2 | Google Cloud Console → Credentials |
| Slack Bot Token | api.slack.com → Your Apps → Bot Token |
| F# Service URL | Jouw Railway deployment-URL |

### 5. Google Sheet instellen

Maak een nieuw Google Sheet aan met een tabblad genaamd `Leads` en deze kolommen:

```
Naam | E-mail | Bedrijf | LinkedIn | Status | Laatste contact | Notities
```

Kopieer het Sheet ID uit de URL en werk de Google Sheets-nodes bij.

### 6. Workflow activeren

Zet de workflow op **Actief** in n8n. Voeg een lead toe aan je Google Sheet en bekijk hoe de pipeline loopt.

---

## Prompts

Alle AI-prompts zijn versioned in `/prompts` zodat je ze kan verbeteren zonder de n8n-flow aan te raken.

**Ontwerpprincipes voor outreach-prompts:**
- Verwijs naar specifieke bedrijfsdetails (niet generiek "ik zag je website")
- Één duidelijke call to action per e-mail
- Onder de 150 woorden — drukke mensen lezen geen lange koude e-mails
- JSON-output met aparte velden: `subject`, `opening`, `body`, `cta`

---

## F# service endpoints

| Methode | Pad | Omschrijving |
|---|---|---|
| GET | /health | Health check voor Railway |
| POST | /score | Valideer en scoor een lead |

**POST /score verzoek:**
```json
{
  "name":    "Jan de Vries",
  "email":   "jan@bedrijf.nl",
  "company": "Bedrijf BV",
  "budget":  4500,
  "message": "We moeten zo snel mogelijk onze outreach automatiseren."
}
```

**Antwoord — geldige lead:**
```json
{
  "score":      "Hot",
  "reasoning":  "Budget overschrijdt drempelwaarde van €3000",
  "confidence": 0.85,
  "valid":      true
}
```

**Antwoord — ongeldige lead:**
```json
{
  "valid":  false,
  "errors": ["Ongeldig e-mailadres: jan-geen-apenstaart"]
}
```

---

## Waarom F# voor de validatieservice?

De gemiddelde n8n-freelancer stopt alle logica in een JavaScript Code node.
Dit leidt tot stille fouten: ongeldige e-mails, negatieve budgets en lege berichten
die onopgemerkt doorstromen naar je CRM en e-mailverzender.

F# maakt ongeldige data onmogelijk op type-niveau.
De compiler weigert te compileren als de validatie omzeild wordt.
De ongeldige lead die een JavaScript-pipeline stilletjes corrumpeert
wordt afgevangen voordat die ooit de HTTP-handler bereikt.

---

## Licentie

MIT

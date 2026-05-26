# Trading Assistant - Mémoire de portefeuille & Analyse quotidienne

## Vue d'ensemble

Ce repository sert de **mémoire persistante** pour l'Agent IA. Il contient l'état de mon portefeuille de trading (Swissquote), l'historique de mes positions, et les analyses générées quotidiennement.

**Aucun code applicatif n'est hébergé ici.** Tout repose sur l'Agent IA, orchestré par une tâche planifiée, avec :
- **Recherche Web** : données de marché en temps réel, indicateurs techniques, fondamentaux, news & sentiment (cash.ch, leonteq, swissquote, Yahoo Finance, etc.)
- **`send_email.py`** : envoi du rapport quotidien par email via Resend

## Workflow quotidien

```
Tâche planifiée (cron / Task Scheduler)
  └─> Agent IA pull ce repo
        ├─> Lit MEMORY.md (récupère la mémoire)
        ├─> Lit portfolio.json (état actuel du portefeuille)
        ├─> Interroge le Web (cours, indicateurs, news)
        ├─> Analyse les positions & détecte des opportunités
        ├─> Génère le rapport du jour sur la base du dernier (reports/)
        ├─> Met à jour portfolio.json
        ├─> Commit & push les changements
        └─> Envoie le rapport (en html) par email via Resend
```

## Structure du repository

```
.
├── README.md                   # Ce fichier
├── MEMORY.md                   # Instructions pour l'Agent IA
├── portfolio.json              # État actuel du portefeuille
├── config.json                 # Configuration (profil de risque, watchlist, préférences)
├── report_to_html.py           # Convertit un rapport en .md vers du .html
├── send_email.py               # Envoi d'email via Resend
└── reports/                    # Historique des rapports quotidiens
    └── YYYY-MM-DD.md           # Rapport du jour
```

## portfolio.json

Contient la situation actuelle du portefeuille :
- Solde cash disponible (CHF / USD)
- Positions ouvertes (ticker, quantité, prix d'achat, date d'entrée)
- Historique des transactions (achats/ventes passés)
- Performance globale depuis le départ

## config.json

Paramètres de l'assistant :
- Profil de risque (conservateur / modéré / agressif)
- Watchlist : tickers à surveiller au-delà des positions ouvertes
- Devise de référence (CHF)
- Préférences de rapport (indicateurs favoris, horizons d'analyse)

### Email (send_email.py)

L'envoi d'email utilise Resend avec une clé API.

**Variables d'environnement requises :**

| Variable | Description |
|---|---|
| `GMAIL_ADDRESS` | Adresse Gmail de l'expéditeur |
| `RESEND_API_KEY` | Clé API Resend
| `GMAIL_RECIPIENT` | Destinataire (optionnel, par défaut = `GMAIL_ADDRESS`) |

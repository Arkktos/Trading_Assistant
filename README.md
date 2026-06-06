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
        ├─> S'assurer que tout est sur la branche main du remote (PR + merge si nécessaire)
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

## Instructions pour l'Agent IA

### Git workflow

- Travailler directement sur la branche `main`.
- Après chaque rapport généré :
  1. **Commit** les changements avec un message descriptif.
  2. **Push** directement sur `main`.
- Utiliser le token Github contenu dans `GITHUB_PAT`.
- Pour éviter les erreurs d'authentification lors du push, utiliser l'URL du remote incluant le token : `https://x-access-token:<TOKEN>@github.com/user/repo.git` ou s'assurer que la CLI `gh` est authentifiée avec le PAT.

### Source de données

- Utiliser le **Web** (cash.ch, leonteq, swissquote, Yahoo Finance, etc.) comme source de prix et d'analyse pour tous les instruments.

### Envoi d'email

- L'envoi d'email se fait via **`send_email.py`** (API Resend).
- Variables d'environnement :
  - `RESEND_API_KEY` : clé API Resend (requis)
  - `GMAIL_RECIPIENT` : adresse du destinataire (optionnel, fallback sur `GMAIL_ADDRESS`)
- L'installation de resend est requise en amont.

### Format du rapprt

- Lire le dernier rapport et formater le nouveau de la même manière (exactement la même structure, les mêmes chapitres!!)

### Cleanup

- Après chaque commit (donc chaque nouveau rapport, normalement): TOUT DOIT ETRE SUR LA BRANCHE MAIN DU REMOTE. A l'agent de se débrouiller pour pousser sur le remote, créer et merger un PR si besoin. 

# Trading Assistant - Mémoire de portefeuille & Analyse quotidienne

## Vue d'ensemble

Ce repository sert de **mémoire persistante** pour l'Agent IA. Il contient l'état de mon portefeuille de trading (Swissquote), l'historique de mes positions, et les analyses générées quotidiennement.

**Aucun code applicatif n'est hébergé ici.** Tout repose sur l'Agent IA, orchestré par une tâche planifiée, avec :
- **Recherche Web** : données de marché en temps réel, indicateurs techniques, fondamentaux, news & sentiment (cash.ch, leonteq, swissquote, Yahoo Finance, etc.)
- **`send_email_node.js`** : envoi du rapport quotidien par email via nodemailer

## Workflow quotidien

```
Tâche planifiée (cron / Task Scheduler)
  └─> Agent IA pull ce repo
        ├─> Lit tous les fichiers à la racine et le dernier rapport dans reports/
        ├─> Interroge le Web (cours, indicateurs, news, etc.)
        ├─> Analyse les positions & détecte des opportunités
        ├─> Génère le rapport du jour sur la base du dernier (même format)
        ├─> Met à jour portfolio.json et éventuellement config.json
        ├─> Commit & push les changements
        ├─> S'assurer que tout est sur la branche main du remote (PR + merge si nécessaire)
        └─> Envoie le rapport (en html) par email
```

## Structure du repository

```
.
├── README.md                   # Ce fichier
├── portfolio.json              # État actuel du portefeuille
├── config.json                 # Configuration (profil de risque, watchlist, préférences)
├── report_to_html_node.js      # Convertit un rapport en .md vers du .html
├── send_email_node.js          # Envoi d'email via nodemailer
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
  1. **Commit** les changements. Le message de commit **doit** avoir le format exact : `Daily Report dd-mm-yyyy` (ex. `Daily Report 16-10-2026`). Pas de texte additionnel, pas de majuscules superflues.
  2. **Push** directement sur `main`.
- Utiliser le token Github contenu dans `GH_TOKEN` si nécessaire.
- Pour éviter les erreurs d'authentification lors du push, utiliser l'URL du remote incluant le token : `https://x-access-token:<TOKEN>@github.com/user/repo.git` ou s'assurer que la CLI `gh` est authentifiée avec le PAT.

### Envoi d'email

- L'envoi d'email se fait via **`send_email_node.js`** (Gmail SMTP avec App Password).
- Variables d'environnement :
  - `GMAIL_ADDRESS` : adresse Gmail de l'expéditeur (requis)
  - `GMAIL_APP_PASSWORD` : mot de passe d'application Gmail (requis)
- L'installation de la dépendance `nodemailer` est requise via `npm install`.

### Format du rapport

- Rapport **concis**. Suivre exactement ce squelette (4 chapitres, rien d'autre) :
  1. **Résumé du jour** — l'essentiel en quelques paragraphes/points, pas d'analyse exhaustive point par point.
  2. **Positions ouvertes** — tableau (instrument, quantité, prix d'achat moyen, cours actuel, P&L latent, poids, note courte).
  3. **Synthèse portefeuille** — tableau (cash CHF/USD, valeur des positions, total, performance).
  4. **Recommandations** — actions concrètes & points de vigilance, en liste courte.
- Pas de chapitre "P&L réalisé cumulé" ni de section "Contexte macro & stratégie" séparée dans le rapport — le détail macro va dans le Résumé du jour si pertinent, en restant bref. Le P&L réalisé et les retraits de capital restent suivis dans `portfolio.json` (champs `closed_positions` / `withdrawals`), pas affichés dans le rapport.

### Style des recommandations (profil AGRESSIF)

Le profil de risque est **agressif** (`config.json`) — les recommandations doivent le refléter, pas se réfugier dans un "tenir / ne rien faire" par défaut.

- **Concret et actionnable.** Chaque reco = une action précise avec niveaux chiffrés : ticker, sens (achat/renfort/allègement/vente/prise de profit), **zone de prix d'entrée ou de sortie**, taille indicative (en CHF ou % du cash), et le déclencheur. Éviter les formulations vagues ("rester patient", "surveiller", "garder les munitions sèches") comme conclusion — si attente il y a, dire **quoi attendre et à quel niveau agir**.
- **Assumer le risque.** Le cash lourd n'est pas une fin en soi : viser le **redéploiement** du capital disponible vers des idées à conviction. Proposer activement des entrées quand le risque/rendement est favorable, y compris en pré-événement si la thèse le justifie — sans tomber dans l'imprudence (pas de all-in, pas de levier non demandé).
- **Prioriser.** Classer les recos par conviction/urgence ; distinguer clairement les actions **à faire maintenant** des **conditionnelles** (si niveau X atteint).

### Scan & diversification (élargir au-delà de la tech)

Le portefeuille est trop concentré sur la tech/semi. Chaque rapport doit **scanner plus large** et proposer des idées hors du cœur tech actuel.

- **Balayage multi-secteurs** dans le Résumé ou les Recommandations : au-delà des positions ouvertes et de la watchlist tech, passer en revue d'autres secteurs/classes d'actifs (énergie, santé/pharma, finance/banques, industrie/défense, matières premières & métaux précieux, obligations/taux, crypto, ETF sectoriels ou géographiques hors-US). Faire remonter **les secteurs qui surperforment ou présentent un setup** du moment.
- **Au moins une idée de diversification concrète par rapport** quand le marché en offre une : un instrument hors-tech avec thèse courte + zone d'entrée, pour réduire la corrélation du portefeuille. Ne pas forcer une entrée si rien n'est convaincant, mais le signaler explicitement.
- Mettre à jour la `watchlist` de `config.json` quand une idée hors-tech mérite un suivi récurrent.
# Trading Assistant - Mémoire de portefeuille & Analyse quotidienne

## Vue d'ensemble

Ce repository sert de **mémoire persistante** pour Claude Code. Il contient l'état de mon portefeuille de trading (Swissquote), l'historique de mes positions, et les analyses générées quotidiennement.

**Aucun code applicatif n'est hébergé ici.** Tout repose sur Claude Code, orchestré par une tâche planifiée, avec deux connecteurs MCP :
- **AlphaVantage** : données de marché en temps réel, indicateurs techniques, fondamentaux, news & sentiment
- **Gmail** : envoi du rapport quotidien par email

## Workflow quotidien

```
Tâche planifiée (cron / Task Scheduler)
  └─> Claude Code pull ce repo (récupère la mémoire)
        ├─> Lit portfolio.json (état actuel du portefeuille)
        ├─> Interroge AlphaVantage (cours, indicateurs, news)
        ├─> Analyse les positions & détecte des opportunités
        ├─> Met à jour l'historique dans reports/
        ├─> Commit & push les changements
        └─> Envoie le rapport par email via Gmail
```

## Structure du repository

```
.
├── README.md                   # Ce fichier
├── portfolio.json              # État actuel du portefeuille
├── config.json                 # Configuration (profil de risque, watchlist, préférences)
└── reports/                    # Historique des rapports quotidiens
    └── YYYY-MM-DD.md           # Rapport du jour
```

## portfolio.json

Contient la situation actuelle du portefeuille :
- Solde cash disponible (CHF)
- Positions ouvertes (ticker, quantité, prix d'achat, date d'entrée)
- Historique des transactions (achats/ventes passés)
- Performance globale depuis le départ

## config.json

Paramètres de l'assistant :
- Profil de risque (conservateur / modéré / agressif)
- Watchlist : tickers à surveiller au-delà des positions ouvertes
- Devise de référence (CHF)
- Préférences de rapport (indicateurs favoris, horizons d'analyse)

## Connecteurs MCP disponibles

### AlphaVantage
- Séries temporelles (intraday, daily, weekly, monthly)
- Cotations en temps réel & bulk quotes
- Indicateurs techniques (SMA, EMA, RSI, MACD, BBANDS, etc.)
- Fondamentaux (income statement, balance sheet, cash flow, earnings)
- Profils d'entreprise & ETF
- News & sentiment de marché
- Données macro (GDP, CPI, taux, chômage)
- Commodities (or, pétrole, gaz, métaux)
- Forex & crypto

### Gmail
- Envoi du rapport quotidien par email

## Pour démarrer

1. Me fournir le solde initial du compte Swissquote
2. Me fournir les positions actuelles (ticker, quantité, prix d'achat)
3. Définir le profil de risque et la watchlist souhaitée

Je créerai alors les fichiers `portfolio.json` et `config.json` initiaux.

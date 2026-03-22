# Trading Assistant - Mémoire de portefeuille & Analyse quotidienne

## Vue d'ensemble

Ce repository sert de **mémoire persistante** pour Claude Code. Il contient l'état de mon portefeuille de trading (Swissquote), l'historique de mes positions, et les analyses générées quotidiennement.

**Aucun code applicatif n'est hébergé ici.** Tout repose sur Claude Code, orchestré par une tâche planifiée, avec deux connecteurs MCP :
- **AlphaVantage** : données de marché en temps réel, indicateurs techniques, fondamentaux, news & sentiment
- **Gmail** : envoi du rapport quotidien par email

## Portefeuille

### Positions ouvertes

| Ticker | Nom | Type | Qté | Prix d'achat (CHF) | Date d'achat | Source de prix |
|--------|-----|------|-----|---------------------|--------------|----------------|
| AUCHAH | UBS ETF Gold hedged CHF | ETF | 20 | 165.72 | 20.02.2026 | [cash.ch](https://www.cash.ch/fonds/ubs-etf-ch-ubs-gold-hchf-etf-10602712/swx/chf) |
| FIVETQ | Swissquote 5G Revolution Index (Leonteq) | Produit structuré | 19 | 256.84 | 18.02.2026 | [cash.ch](https://www.cash.ch/derivate/leoz-open-46772041/qmh/usd) |

**Capital investi** : (20 × 165.72) + (19 × 256.84) = 3'314.40 + 4'879.96 = **8'194.36 CHF**
**Cash restant** : 642.35 CHF
**Capital initial** : 8'836.71 CHF

### Détails des instruments

#### AUCHAH — UBS ETF (CH) Gold hedged CHF
- **ISIN** : CH0106027128
- **Valor** : 10602712
- **Bourse** : SIX Swiss Exchange (SWX)
- **Devise** : CHF
- **Type** : ETF répliquant le cours de l'or, couvert contre le risque de change CHF/USD
- **API** : Non disponible sur AlphaVantage ni Yahoo Finance
- **Suivi des cours** : [cash.ch](https://www.cash.ch/fonds/ubs-etf-ch-ubs-gold-hchf-etf-10602712/swx/chf)

#### FIVETQ — Tracker-Zertifikat sur Swissquote 5G Revolution Index
- **ISIN** : CH0467720410
- **Valor** : 46772041
- **Émetteur** : Leonteq Securities AG
- **Bourse** : SIX Swiss Exchange (QMH)
- **Devise** : USD (coté en USD, acheté en CHF via Swissquote)
- **Type** : Certificat tracker sur un panier d'entreprises mondiales liées à la 5G
- **API** : Non disponible sur AlphaVantage ni Yahoo Finance
- **Suivi des cours** : [cash.ch](https://www.cash.ch/derivate/leoz-open-46772041/qmh/usd) | [Leonteq](https://structuredproducts-ch.leonteq.com/isin/CH0467720410)

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

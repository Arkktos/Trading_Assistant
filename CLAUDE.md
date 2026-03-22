# Instructions pour les agents Claude Code

## Git workflow

- **Ne jamais créer de nouvelles branches.** Travailler directement sur `main`.
- Pull, commit et push directement sur `main`.
- Ne pas créer de PR sauf demande explicite.

## Source de données

- Les cours de AUCHAH et FIVETQ ne sont pas disponibles via API (AlphaVantage, Yahoo Finance).
- Utiliser **cash.ch** comme source de prix pour les deux instruments.
  - AUCHAH : https://www.cash.ch/fonds/ubs-etf-ch-ubs-gold-hchf-etf-10602712/swx/chf
  - FIVETQ : https://www.cash.ch/derivate/leoz-open-46772041/qmh/usd

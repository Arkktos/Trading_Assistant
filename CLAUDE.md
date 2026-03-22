# Instructions pour les agents Claude Code

## Git workflow

- Travailler sur la branche assignée par le système (ex. `claude/...`).
- Après chaque rapport généré :
  1. **Commit** les changements avec un message descriptif.
  2. **Push** sur la branche courante.
  3. **Créer un PR** vers `main`.
  4. **Activer l'auto-merge** du PR avec l'option **rebase** : `gh pr merge --auto --rebase`.

## Source de données

- Les cours de AUCHAH et FIVETQ ne sont pas disponibles via API (AlphaVantage, Yahoo Finance).
- Utiliser **cash.ch** comme source de prix pour les deux instruments.
  - AUCHAH : https://www.cash.ch/fonds/ubs-etf-ch-ubs-gold-hchf-etf-10602712/swx/chf
  - FIVETQ : https://www.cash.ch/derivate/leoz-open-46772041/qmh/usd

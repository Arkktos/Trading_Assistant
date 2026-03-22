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

## Envoi d'email

- L'envoi d'email se fait via **`send_email.py`** (SMTP Gmail avec app password).
- **Ne pas utiliser de MCP Gmail ni d'API Google Cloud.**
- Trois variables d'environnement sont requises :
  - `GMAIL_ADDRESS` : adresse Gmail de l'expéditeur
  - `GMAIL_APP_PASSWORD` : mot de passe d'application Gmail (pas le mot de passe principal)
  - `GMAIL_RECIPIENT` : adresse du destinataire (optionnel, par défaut = `GMAIL_ADDRESS`)
- Utilisation dans un script Python :
  ```python
  from send_email import send_email
  send_email("Sujet", "Corps du message")
  ```
- Utilisation en CLI :
  ```bash
  python send_email.py "Sujet" chemin/vers/fichier.md [destinataire]
  ```
- Pour envoyer le rapport quotidien après génération :
  ```python
  from send_email import send_email
  with open(f"reports/{date}.md") as f:
      body = f.read()
  send_email(f"Rapport Trading {date}", body)
  ```

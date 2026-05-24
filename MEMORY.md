# Instructions pour l'Agent IA

## Git workflow

- Travailler directement sur la branche `main`.
- Après chaque rapport généré :
  1. **Commit** les changements avec un message descriptif.
  2. **Push** directement sur `main`.

## Source de données

- Utiliser le **Web** (cash.ch, leonteq, swissquote, Yahoo Finance, etc.) comme source de prix et d'analyse pour tous les instruments.
  - AUCHAH : https://www.cash.ch/fonds/ubs-etf-ch-ubs-gold-hchf-etf-10602712/swx/chf
  - FIVETQ : https://www.cash.ch/derivate/leoz-open-46772041/qmh/usd

## Envoi d'email

- L'envoi d'email se fait via **`send_email.py`** (API Resend).
- **Ne pas utiliser de MCP Gmail ni d'API Google Cloud.**
- Variables d'environnement :
  - `RESEND_API_KEY` : clé API Resend (requis)
  - `GMAIL_RECIPIENT` : adresse du destinataire (optionnel, fallback sur `GMAIL_ADDRESS`)
- Utilisation dans un script Python :
  ```python
  from send_email import send_email
  send_email("Sujet", "Corps du message")
  ```
- Utilisation en CLI :
  ```bash
  python send_email.py "Sujet" chemin/vers/fichier.md [destinataire]
  ```
- Pour envoyer le rapport quotidien après génération (auto-converti en HTML) :
  ```python
  from send_email import send_email
  from report_to_html import md_to_html
  with open(f"reports/{date}.md") as f:
      body = md_to_html(f.read())
  send_email(f"Rapport Trading {date}", body, html=True)
  ```
- En CLI, les fichiers `.md` sont automatiquement convertis en HTML :
  ```bash
  python send_email.py "Rapport Trading 2026-03-23" reports/2026-03-23.md
  ```

## Format du rapprt

- Lire le dernier rapport et formater le nouveau de la même manière (exactement la même structure, les mêmes chapitres!!)

# Instructions pour l'Agent IA

## Git workflow

- Travailler directement sur la branche `main`.
- Après chaque rapport généré :
  1. **Commit** les changements avec un message descriptif.
  2. **Push** directement sur `main`.
- Utiliser le token Github contenu dans `GITHUB_PAT`.
- Pour éviter les erreurs d'authentification lors du push, utiliser l'URL du remote incluant le token : `https://x-access-token:<TOKEN>@github.com/user/repo.git` ou s'assurer que la CLI `gh` est authentifiée avec le PAT.

## Source de données

- Utiliser le **Web** (cash.ch, leonteq, swissquote, Yahoo Finance, etc.) comme source de prix et d'analyse pour tous les instruments.

## Envoi d'email

- L'envoi d'email se fait via **`send_email.py`** (API Resend).
- Variables d'environnement :
  - `RESEND_API_KEY` : clé API Resend (requis)
  - `GMAIL_RECIPIENT` : adresse du destinataire (optionnel, fallback sur `GMAIL_ADDRESS`)
- L'installation de resend est requise en amont.

## Format du rapprt

- Lire le dernier rapport et formater le nouveau de la même manière (exactement la même structure, les mêmes chapitres!!)

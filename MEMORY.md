# Instructions pour l'Agent IA

## Git workflow

- Travailler sur la branche assignée par le système (ex. `agent/...`).
- Après chaque rapport généré :
  1. **Commit** les changements avec un message descriptif.
  2. **Push** sur la branche courante.
  3. **Créer un PR** vers `main`.
  4. **Merger le PR** avec rebase.

### Problèmes connus et solutions

Le remote git pointe vers un proxy local (`127.0.0.1`), pas directement vers `github.com`. Cela empêche les commandes `gh pr` et `gh pr merge` de fonctionner. Utiliser l'API GitHub (`gh api`) à la place.

**Créer un PR** (au lieu de `gh pr create`) :
```bash
gh api repos/Arkktos/Trading_Assistant/pulls \
  -f title="Titre du PR" \
  -f head="claude/nom-de-branche" \
  -f base="main" \
  -f body="Description du PR"
```
> Récupérer le `node_id` et le `number` dans la réponse JSON pour les étapes suivantes.

**Activer l'auto-merge avec rebase** (au lieu de `gh pr merge --auto --rebase`) :
```bash
gh api graphql -f query='
mutation {
  enablePullRequestAutoMerge(input: {
    pullRequestId: "<node_id du PR>",
    mergeMethod: REBASE
  }) {
    pullRequest { autoMergeRequest { enabledAt mergeMethod } }
  }
}'
```
> ⚠️ L'auto-merge nécessite des branch protection rules sur `main`. Si elles ne sont pas configurées, merger directement.

**Merger directement un PR** (fallback si l'auto-merge échoue) :
```bash
gh api -X PUT repos/Arkktos/Trading_Assistant/pulls/<number>/merge \
  -f merge_method=rebase
```

**Push** : ne pas pusher directement sur `main` (403). Toujours passer par une branche `agent/...` + PR.

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

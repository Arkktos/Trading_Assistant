"""
Envoi d'emails via Resend.

Variables d'environnement requises :
  RESEND_API_KEY  - clé API Resend
  GMAIL_RECIPIENT - adresse du destinataire
"""

import os
import sys

import resend


def send_email(subject: str, body: str, recipient: str | None = None, html: bool = False) -> None:
    api_key = os.environ.get("RESEND_API_KEY")
    if not api_key:
        raise RuntimeError("RESEND_API_KEY doit être défini")

    resend.api_key = api_key

    recipient = recipient or os.environ.get("GMAIL_RECIPIENT") or os.environ.get("GMAIL_ADDRESS")
    if not recipient:
        raise RuntimeError("Un destinataire doit être fourni (argument, GMAIL_RECIPIENT ou GMAIL_ADDRESS)")

    params: resend.Emails.SendParams = {
        "from": "Trading Assistant <onboarding@resend.dev>",
        "to": [recipient],
        "subject": subject,
    }

    if html:
        params["html"] = body
    else:
        params["text"] = body

    email = resend.Emails.send(params)
    print(f"Email envoyé à {recipient} (id: {email['id']})")


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python send_email.py <sujet> <fichier_body> [destinataire]")
        sys.exit(1)

    subject = sys.argv[1]
    body_path = sys.argv[2]
    recipient = sys.argv[3] if len(sys.argv) > 3 else None

    with open(body_path, encoding="utf-8") as f:
        body = f.read()

    is_html = body_path.endswith(".html")

    # Auto-convert .md files to styled HTML for better email readability
    if body_path.endswith(".md"):
        from report_to_html import md_to_html
        body = md_to_html(body)
        is_html = True

    send_email(subject, body, recipient, html=is_html)

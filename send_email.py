"""
Envoi d'emails via Gmail SMTP avec app password.

Variables d'environnement requises :
  GMAIL_ADDRESS   - adresse Gmail (ex: user@gmail.com)
  GMAIL_APP_PASSWORD - mot de passe d'application Gmail
  GMAIL_RECIPIENT - adresse du destinataire (par défaut = GMAIL_ADDRESS)
"""

import os
import smtplib
import sys
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText

SMTP_SERVER = "smtp.gmail.com"
SMTP_PORT = 587


def send_email(subject: str, body: str, recipient: str | None = None, html: bool = False) -> None:
    address = os.environ.get("GMAIL_ADDRESS")
    password = os.environ.get("GMAIL_APP_PASSWORD")

    if not address or not password:
        raise RuntimeError("GMAIL_ADDRESS et GMAIL_APP_PASSWORD doivent être définis")

    recipient = recipient or os.environ.get("GMAIL_RECIPIENT", address)

    msg = MIMEMultipart("alternative")
    msg["From"] = address
    msg["To"] = recipient
    msg["Subject"] = subject

    content_type = "html" if html else "plain"
    msg.attach(MIMEText(body, content_type, "utf-8"))

    with smtplib.SMTP(SMTP_SERVER, SMTP_PORT) as server:
        server.starttls()
        server.login(address, password)
        server.sendmail(address, recipient, msg.as_string())

    print(f"Email envoyé à {recipient}")


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
    send_email(subject, body, recipient, html=is_html)

"""
Convertit un rapport Markdown en HTML stylisé pour email.

Usage:
    python report_to_html.py reports/2026-03-23.md
    # Génère reports/2026-03-23.html

En tant que module:
    from report_to_html import md_to_html
    html = md_to_html(markdown_text)
"""

import re
import sys
from pathlib import Path

CSS = """
body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    max-width: 700px;
    margin: 0 auto;
    padding: 20px;
    color: #1a1a1a;
    background: #f8f9fa;
    line-height: 1.6;
}
h1 {
    color: #0d47a1;
    border-bottom: 3px solid #0d47a1;
    padding-bottom: 10px;
    font-size: 22px;
}
h2 {
    color: #1565c0;
    border-bottom: 1px solid #ccc;
    padding-bottom: 6px;
    margin-top: 28px;
    font-size: 18px;
}
h3 {
    color: #333;
    font-size: 15px;
    margin-top: 20px;
}
table {
    border-collapse: collapse;
    width: 100%;
    margin: 12px 0;
    font-size: 14px;
}
th, td {
    border: 1px solid #ddd;
    padding: 8px 12px;
    text-align: left;
}
th {
    background: #e3f2fd;
    font-weight: 600;
    color: #0d47a1;
}
tr:nth-child(even) { background: #fafafa; }
tr:hover { background: #e8f0fe; }
hr {
    border: none;
    border-top: 1px solid #ddd;
    margin: 24px 0;
}
.positive { color: #2e7d32; font-weight: 600; }
.negative { color: #c62828; font-weight: 600; }
.warning {
    background: #fff3e0;
    border-left: 4px solid #ff9800;
    padding: 8px 12px;
    margin: 10px 0;
    border-radius: 0 4px 4px 0;
}
.alert {
    background: #fce4ec;
    border-left: 4px solid #c62828;
    padding: 8px 12px;
    margin: 10px 0;
    border-radius: 0 4px 4px 0;
}
strong { color: #1a1a1a; }
em { color: #555; }
ul, ol { padding-left: 22px; }
li { margin-bottom: 4px; }
.footer {
    margin-top: 30px;
    padding-top: 12px;
    border-top: 1px solid #ddd;
    font-size: 12px;
    color: #888;
    font-style: italic;
}
"""


def _escape(text: str) -> str:
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def _inline(text: str) -> str:
    """Process inline markdown: bold, italic, links."""
    # Bold
    text = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", text)
    # Italic
    text = re.sub(r"\*(.+?)\*", r"<em>\1</em>", text)
    # Colorize positive/negative percentages and amounts
    text = re.sub(
        r"(<strong>)(\+[\d',.\s]+(?:CHF|USD|%))(</strong>)",
        r'<strong class="positive">\2</strong>',
        text,
    )
    text = re.sub(
        r"(<strong>)(-[\d',.\s]+(?:CHF|USD|%)(?:\s*\([^)]*\))?)(</strong>)",
        r'<strong class="negative">\2</strong>',
        text,
    )
    return text


def _parse_table(lines: list[str]) -> str:
    """Convert markdown table lines to HTML table."""
    if len(lines) < 2:
        return ""
    headers = [c.strip() for c in lines[0].strip("|").split("|")]
    rows = []
    for line in lines[2:]:  # skip separator
        cells = [c.strip() for c in line.strip("|").split("|")]
        rows.append(cells)

    html = "<table>\n"
    if any(h for h in headers):
        html += "<thead><tr>"
        for h in headers:
            html += f"<th>{_inline(h)}</th>"
        html += "</tr></thead>\n"
    html += "<tbody>\n"
    for row in rows:
        html += "<tr>"
        for cell in row:
            html += f"<td>{_inline(cell)}</td>"
        html += "</tr>\n"
    html += "</tbody></table>\n"
    return html


def md_to_html(md: str) -> str:
    """Convert a trading report markdown string to styled HTML."""
    lines = md.split("\n")
    body_parts: list[str] = []
    i = 0
    footer_lines: list[str] = []

    while i < len(lines):
        line = lines[i]

        # Horizontal rule
        if re.match(r"^---+\s*$", line):
            body_parts.append("<hr>")
            i += 1
            continue

        # Headers
        if line.startswith("### "):
            body_parts.append(f"<h3>{_inline(line[4:])}</h3>")
            i += 1
            continue
        if line.startswith("## "):
            body_parts.append(f"<h2>{_inline(line[3:])}</h2>")
            i += 1
            continue
        if line.startswith("# "):
            body_parts.append(f"<h1>{_inline(line[2:])}</h1>")
            i += 1
            continue

        # Table block
        if "|" in line and i + 1 < len(lines) and re.match(r"^\|[-|:\s]+\|$", lines[i + 1].strip()):
            table_lines = []
            while i < len(lines) and "|" in lines[i]:
                table_lines.append(lines[i])
                i += 1
            body_parts.append(_parse_table(table_lines))
            continue

        # Ordered list item — collect all consecutive numbered items
        # (items may be separated by blank lines in markdown)
        m = re.match(r"^(\d+)\.\s+(.+)$", line)
        if m:
            items: list[str] = []
            while i < len(lines):
                m2 = re.match(r"^(\d+)\.\s+(.+)$", lines[i])
                if m2:
                    items.append(m2.group(2))
                    i += 1
                elif lines[i].strip() == "":
                    # blank line — peek ahead for another numbered item
                    j = i + 1
                    while j < len(lines) and lines[j].strip() == "":
                        j += 1
                    if j < len(lines) and re.match(r"^(\d+)\.\s+", lines[j]):
                        i = j  # skip blanks, continue list
                    else:
                        i += 1
                        break
                else:
                    break
            html = "<ol>\n"
            for item in items:
                content = _inline(item)
                if "⚠️" in content or "ALERTE" in content:
                    content = f'<div class="alert">{content}</div>'
                html += f"<li>{content}</li>\n"
            html += "</ol>\n"
            body_parts.append(html)
            continue

        # Unordered list item
        if line.startswith("- "):
            items = []
            while i < len(lines) and lines[i].startswith("- "):
                items.append(lines[i][2:])
                i += 1
            html = "<ul>\n"
            for item in items:
                html += f"<li>{_inline(item)}</li>\n"
            html += "</ul>\n"
            body_parts.append(html)
            continue

        # Footer lines (italic at end)
        if line.startswith("*") and line.endswith("*") and not line.startswith("**"):
            footer_lines.append(line.strip("*").strip())
            i += 1
            continue

        # Empty line
        if line.strip() == "":
            i += 1
            continue

        # Paragraph — collect consecutive non-empty lines
        para = []
        while i < len(lines) and lines[i].strip() and not lines[i].startswith("#") and not lines[i].startswith("---") and "|" not in lines[i] and not re.match(r"^\d+\.\s", lines[i]) and not lines[i].startswith("- "):
            para.append(lines[i])
            i += 1
        text = _inline(" ".join(para))
        if "⚠️" in text:
            body_parts.append(f'<div class="warning"><p>{text}</p></div>')
        else:
            body_parts.append(f"<p>{text}</p>")

    # Build footer
    if footer_lines:
        body_parts.append('<div class="footer">')
        for fl in footer_lines:
            body_parts.append(f"<p>{_inline(fl)}</p>")
        body_parts.append("</div>")

    body_html = "\n".join(body_parts)

    return f"""<!DOCTYPE html>
<html lang="fr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>{CSS}</style>
</head>
<body>
{body_html}
</body>
</html>"""


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python report_to_html.py <fichier.md>")
        sys.exit(1)

    md_path = Path(sys.argv[1])
    md_text = md_path.read_text(encoding="utf-8")
    html = md_to_html(md_text)

    html_path = md_path.with_suffix(".html")
    html_path.write_text(html, encoding="utf-8")
    print(f"HTML généré : {html_path}")

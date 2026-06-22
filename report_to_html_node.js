const fs = require('fs');
const path = require('path');

const CSS = `
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
`;

function escapeHtml(text) {
    return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

function inline(text) {
    // Bold
    text = text.replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>");
    // Italic
    text = text.replace(/\*(.+?)\*/g, "<em>$1</em>");
    // Colorize positive/negative percentages and amounts
    text = text.replace(/(<strong>)(\+[\d',.\s]+(?:CHF|USD|%))(<\/strong>)/g, '<strong class="positive">$2</strong>');
    text = text.replace(/(<strong>)(-[\d',.\s]+(?:CHF|USD|%)(?:\s*\([^)]*\))?)(<\/strong>)/g, '<strong class="negative">$2</strong>');
    return text;
}

function parseTable(lines) {
    if (lines.length < 2) return "";
    
    const headers = lines[0].trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map(c => c.trim());
    const rows = [];
    for (let i = 2; i < lines.length; i++) {
        const cells = lines[i].trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map(c => c.trim());
        rows.push(cells);
    }

    let html = "<table>\n";
    if (headers.some(h => h)) {
        html += "<thead><tr>";
        for (const h of headers) {
            html += `<th>${inline(h)}</th>`;
        }
        html += "</tr></thead>\n";
    }
    html += "<tbody>\n";
    for (const row of rows) {
        html += "<tr>";
        for (const cell of row) {
            html += `<td>${inline(cell)}</td>`;
        }
        html += "</tr>\n";
    }
    html += "</tbody></table>\n";
    return html;
}

function mdToHtml(md) {
    const lines = md.split("\n");
    const bodyParts = [];
    let i = 0;
    const footerLines = [];

    while (i < lines.length) {
        const line = lines[i];

        // Horizontal rule
        if (/^---+\s*$/.test(line)) {
            bodyParts.push("<hr>");
            i++;
            continue;
        }

        // Headers
        if (line.startsWith("### ")) {
            bodyParts.push(`<h3>${inline(line.substring(4))}</h3>`);
            i++;
            continue;
        }
        if (line.startsWith("## ")) {
            bodyParts.push(`<h2>${inline(line.substring(3))}</h2>`);
            i++;
            continue;
        }
        if (line.startsWith("# ")) {
            bodyParts.push(`<h1>${inline(line.substring(2))}</h1>`);
            i++;
            continue;
        }

        // Table block
        if (line.includes("|") && i + 1 < lines.length && /^\|[-|:\s]+\|$/.test(lines[i + 1].trim())) {
            const tableLines = [];
            while (i < lines.length && lines[i].includes("|")) {
                tableLines.push(lines[i]);
                i++;
            }
            bodyParts.push(parseTable(tableLines));
            continue;
        }

        // Ordered list item
        const olMatch = line.match(/^(\d+)\.\s+(.+)$/);
        if (olMatch) {
            const items = [];
            while (i < lines.length) {
                const m2 = lines[i].match(/^(\d+)\.\s+(.+)$/);
                if (m2) {
                    items.push(m2[2]);
                    i++;
                } else if (lines[i].trim() === "") {
                    let j = i + 1;
                    while (j < lines.length && lines[j].trim() === "") {
                        j++;
                    }
                    if (j < lines.length && /^(\d+)\.\s+/.test(lines[j])) {
                        i = j;
                    } else {
                        i++;
                        break;
                    }
                } else {
                    break;
                }
            }
            let html = "<ol>\n";
            for (const item of items) {
                let content = inline(item);
                if (content.includes("⚠️") || content.includes("ALERTE")) {
                    content = `<div class="alert">${content}</div>`;
                }
                html += `<li>${content}</li>\n`;
            }
            html += "</ol>\n";
            bodyParts.push(html);
            continue;
        }

        // Unordered list item
        if (line.startsWith("- ")) {
            const items = [];
            while (i < lines.length && lines[i].startsWith("- ")) {
                items.push(lines[i].substring(2));
                i++;
            }
            let html = "<ul>\n";
            for (const item of items) {
                html += `<li>${inline(item)}</li>\n`;
            }
            html += "</ul>\n";
            bodyParts.push(html);
            continue;
        }

        // Footer lines
        if (line.startsWith("*") && line.endsWith("*") && !line.startsWith("**")) {
            footerLines.push(line.slice(1, -1).trim());
            i++;
            continue;
        }

        // Empty line
        if (line.trim() === "") {
            i++;
            continue;
        }

        // Paragraph
        const para = [];
        while (i < lines.length && lines[i].trim() && !lines[i].startsWith("#") && !lines[i].startsWith("---") && !lines[i].includes("|") && !/^(\d+)\.\s+/.test(lines[i]) && !lines[i].startsWith("- ")) {
            para.push(lines[i]);
            i++;
        }
        const text = inline(para.join(" "));
        if (text.includes("⚠️")) {
            bodyParts.push(`<div class="warning"><p>${text}</p></div>`);
        } else {
            bodyParts.push(`<p>${text}</p>`);
        }
    }

    if (footerLines.length > 0) {
        bodyParts.push('<div class="footer">');
        for (const fl of footerLines) {
            bodyParts.push(`<p>${inline(fl)}</p>`);
        }
        bodyParts.push("</div>");
    }

    const bodyHtml = bodyParts.join("\n");

    return `<!DOCTYPE html>
<html lang="fr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>${CSS}</style>
</head>
<body>
${bodyHtml}
</body>
</html>`;
}

const inputPath = process.argv[2];
if (!inputPath) {
    console.error("Usage: node report_to_html.js <file.md>");
    process.exit(1);
}

const mdText = fs.readFileSync(inputPath, 'utf-8');
const html = mdToHtml(mdText);
const outputPath = inputPath.replace(/\\([^\\]*)$/, '\\' + '$1'.replace(/\.md$/, '.html'));
// The above regex is a bit risky. Let's use path.parse
const parsed = path.parse(inputPath);
const finalOutputPath = path.join(parsed.dir, parsed.name + '.html');

fs.writeFileSync(finalOutputPath, html, 'utf-8');
console.log(`HTML généré : ${finalOutputPath}`);

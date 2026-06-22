const nodemailer = require('nodemailer');
const fs = require('fs');
const path = require('path');

async function sendEmail(recipient, htmlFilePath) {
    const user = process.env.GMAIL_ADDRESS;
    const pass = process.env.GMAIL_APP_PASSWORD;

    if (!user || !pass) {
        console.error("Error: Missing GMAIL_ADDRESS or GMAIL_APP_PASSWORD environment variables.");
        process.exit(1);
    }

    const transporter = nodemailer.createTransport({
        service: 'gmail',
        auth: {
            user: user,
            pass: pass
        }
    });

    try {
        const htmlContent = fs.readFileSync(htmlFilePath, 'utf8');
        const mailOptions = {
            from: `Trading Assistant <${user}>`,
            to: recipient,
            subject: `Daily Trading Report - ${new Date().toISOString().split('T')[0]}`,
            html: htmlContent
        };

        const info = await transporter.sendMail(mailOptions);
        console.log('Email sent successfully: ' + info.response);
    } catch (error) {
        console.error('Error sending email: ', error);
        process.exit(1);
    }
}

const [,, recipient, filePath] = process.argv;

if (!recipient || !filePath) {
    console.error("Usage: node send_email_node.js <recipient> <path_to_html_file>");
    process.exit(1);
}

sendEmail(recipient, filePath);

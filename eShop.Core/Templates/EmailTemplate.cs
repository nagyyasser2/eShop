namespace eShop.Core.Templates
{
    public static class EmailTemplate
    {
        public static string GetEmailConfirmationTemplate(string firstName, string confirmationLink)
        {
            return $@"
                    <!DOCTYPE html>
                    <html lang=""en"">
                    <head>
                        <meta charset=""UTF-8"">
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                        <title>Confirm Your Email</title>
                        <style>
                            body {{
                                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                                margin: 0;
                                padding: 0;
                                background-color: #f8f9fa;
                                color: #333333;
                            }}
                            .container {{
                                max-width: 600px;
                                margin: 0 auto;
                                background-color: #ffffff;
                                border-radius: 8px;
                                overflow: hidden;
                                box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
                            }}
                            .header {{
                                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                                color: #ffffff;
                                padding: 40px 30px;
                                text-align: center;
                            }}
                            .header h1 {{
                                margin: 0;
                                font-size: 28px;
                                font-weight: 300;
                            }}
                            .content {{
                                padding: 40px 30px;
                            }}
                            .greeting {{
                                font-size: 18px;
                                margin-bottom: 20px;
                                color: #333333;
                            }}
                            .message {{
                                line-height: 1.6;
                                margin-bottom: 30px;
                                color: #666666;
                            }}
                            .button-container {{
                                text-align: center;
                                margin: 40px 0;
                            }}
                            .confirm-button {{
                                display: inline-block;
                                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                                color: #ffffff;
                                text-decoration: none;
                                padding: 15px 30px;
                                border-radius: 25px;
                                font-weight: 500;
                                font-size: 16px;
                                transition: transform 0.2s ease;
                            }}
                            .confirm-button:hover {{
                                transform: translateY(-2px);
                                box-shadow: 0 4px 15px rgba(102, 126, 234, 0.3);
                            }}
                            .footer {{
                                background-color: #f8f9fa;
                                padding: 20px 30px;
                                text-align: center;
                                font-size: 14px;
                                color: #888888;
                                border-top: 1px solid #eeeeee;
                            }}
                            .footer p {{
                                margin: 5px 0;
                            }}
                            .link-alternative {{
                                background-color: #f8f9fa;
                                padding: 15px;
                                border-radius: 5px;
                                margin-top: 20px;
                                font-size: 12px;
                                color: #666666;
                                word-break: break-all;
                            }}
                            .security-notice {{
                                background-color: #fff3cd;
                                border: 1px solid #ffeaa7;
                                border-radius: 5px;
                                padding: 15px;
                                margin-top: 20px;
                                font-size: 14px;
                                color: #856404;
                            }}
                            .icon {{
                                font-size: 48px;
                                margin-bottom: 10px;
                            }}
        
                            @media only screen and (max-width: 600px) {{
                                .container {{
                                    margin: 10px;
                                    border-radius: 0;
                                }}
                                .header {{
                                    padding: 30px 20px;
                                }}
                                .content {{
                                    padding: 30px 20px;
                                }}
                                .footer {{
                                    padding: 20px;
                                }}
                                .header h1 {{
                                    font-size: 24px;
                                }}
                            }}
                        </style>
                    </head>
                    <body>
                        <div class=""container"">
                            <div class=""header"">
                                <div class=""icon"">✉️</div>
                                <h1>eShop</h1>
                            </div>
        
                            <div class=""content"">
                                <div class=""greeting"">
                                    Hello {firstName},
                                </div>
            
                                <div class=""message"">
                                    <p>Welcome to eShop! We're excited to have you join our community.</p>
                                    <p>To complete your registration and start shopping, please confirm your email address by clicking the button below:</p>
                                </div>
            
                                <div class=""button-container"">
                                    <a href=""{confirmationLink}"" class=""confirm-button"">Confirm Email Address</a>
                                </div>
            
                                <div class=""security-notice"">
                                    <strong>Security Notice:</strong> This confirmation link will expire in 24 hours for your security. If you didn't create an account with eShop, please ignore this email.
                                </div>
            
                                <div class=""link-alternative"">
                                    <p><strong>Having trouble with the button?</strong> Copy and paste this link into your browser:</p>
                                    <p>{confirmationLink}</p>
                                </div>
                            </div>
        
                            <div class=""footer"">
                                <p>© 2024 eShop. All rights reserved.</p>
                                <p>This is an automated email, please do not reply to this message.</p>
                                <p>If you have any questions, contact our support team.</p>
                            </div>
                        </div>
                    </body>
                    </html>";
        }

        public static string GetPasswordResetTemplate(string firstName, string resetLink)
        {
            return $@"
    <!DOCTYPE html>
    <html>
    <head>
        <style>
            body {{
                font-family: Arial, sans-serif;
                line-height: 1.6;
                color: #333;
            }}
            .container {{
                max-width: 600px;
                margin: 0 auto;
                padding: 20px;
            }}
            .header {{
                background-color: #4CAF50;
                color: white;
                padding: 20px;
                text-align: center;
            }}
            .content {{
                background-color: #f9f9f9;
                padding: 30px;
                border: 1px solid #ddd;
            }}
            .button {{
                display: inline-block;
                padding: 12px 30px;
                background-color: #4CAF50;
                color: white;
                text-decoration: none;
                border-radius: 5px;
                margin: 20px 0;
            }}
            .footer {{
                text-align: center;
                padding: 20px;
                color: #777;
                font-size: 12px;
            }}
            .warning {{
                background-color: #fff3cd;
                border: 1px solid #ffc107;
                padding: 15px;
                border-radius: 5px;
                margin: 20px 0;
            }}
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='header'>
                <h1>Password Reset Request</h1>
            </div>
            <div class='content'>
                <h2>Hello {firstName},</h2>
                <p>We received a request to reset your password for your eShop account.</p>
                <p>Click the button below to reset your password:</p>
                <center>
                    <a href='{resetLink}' class='button'>Reset Password</a>
                </center>
                <div class='warning'>
                    <strong>⚠️ Important:</strong>
                    <ul>
                        <li>This link will expire in 24 hours</li>
                        <li>If you didn't request this password reset, please ignore this email</li>
                        <li>Your password will remain unchanged until you create a new one</li>
                    </ul>
                </div>
                <p>If the button doesn't work, copy and paste this link into your browser:</p>
                <p style='word-break: break-all; color: #0066cc;'>{resetLink}</p>
            </div>
            <div class='footer'>
                <p>© 2024 eShop. All rights reserved.</p>
                <p>This is an automated email, please do not reply.</p>
            </div>
        </div>
    </body>
    </html>";
        }
    }
}

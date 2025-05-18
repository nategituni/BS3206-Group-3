## 🛠️ Setup Instructions

### 1. Create a SendGrid Account

If you don’t already have one, go to:  
👉 [https://sendgrid.com/]
Create a free account and verify your email.

---

### 2. Generate an API Key

- Navigate to **Settings → API Keys**
- Click **Create API Key**
- Choose a name (e.g., "MAUI MFA App")
- Give it “Full Access” or at least “Mail Send” permissions
- Copy the key somewhere safe

---

### 3. Verify a Sender Email

Before SendGrid lets you send emails, you must verify a sender identity:

- Go to **Settings → Sender Authentication**
- Under **Single Sender Verification**, add your email
- Click the verification link you receive

You must use this email when sending messages from the app.

---

### 4. Update `EmailService.cs`

Open the file `EmailService.cs` and find the following lines:

```csharp
private const string SendGridApiKey = "Please add your SendGrid API key here";
var from = new EmailAddress("Please add your SendGrid Verified email", "Group 3");

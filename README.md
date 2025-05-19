To run this project you have **two options**:

- ✅ **Option A:** Use a pre-configured test account  
- ✅ **Option B:** Set up your own SendGrid credentials (recommended for full functionality)

---

## ✅ Option A: Use the Provided Test Account

You can use a built-in test account for immediate access without configuring SendGrid:

- **Email:** `test@user.com`  
- **Password:** `Testpassword1!`

> ⚠️ MFA email delivery will not function when using this account unless SendGrid credentials are also configured (see Option B). This is provided primarily for UI access and testing workflows.

---

## 🔧 Option B: Use Your Own SendGrid API Key (for MFA to Work)

To enable full MFA functionality, including receiving a verification code via email, you must use your own **SendGrid API key** and **verified sender email**.

---

### 🛠️ Setup Instructions

#### 1. Create a SendGrid Account

- Sign up at: [https://sendgrid.com/]

---

#### 2. Generate an API Key

- Go to **Settings → API Keys**
- Click **Create API Key**
- Choose a name and select **Full Access** or "Mail Send" permissions
- Save the key safely

---

#### 3. Verify a Sender Email

- Go to **Settings → Sender Authentication**
- Add your email under **Single Sender Verification**
- Verify the email via the confirmation link sent by SendGrid

---

#### 4. Update `EmailService.cs`

Open the `EmailService.cs` file in the `Services` folder and locate the following lines:


private const string SendGridApiKey = "Please add your SendGrid API key here";
var from = new EmailAddress("Please add your SendGrid Verified email", "Group 3");

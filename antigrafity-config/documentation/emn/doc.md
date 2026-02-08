#### **Overview**

The `emn.vn` authentication flow is a **Two-Step SSR (Server-Side Rendering) Login**. It requires a GET request to harvest security tokens before a POST request can be made to authenticate.

#### **Implementation Script**

TypeScript

```
import axios from 'axios';
import { wrapper } from 'axios-cookiejar-support';
import { CookieJar } from 'tough-cookie';
import * as cheerio from 'cheerio';

// 1. Setup a persistent cookie jar
const jar = new CookieJar();
const client = wrapper(axios.create({
    jar,
    withCredentials: true,
    headers: {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36'
    }
}));

async function loginToEmn() {
    const LOGIN_URL = 'https://emn.vn/login';

    try {
        // --- STEP 1: GET the Login Page ---
        // This step populates the CookieJar with the .AspNetCore.Antiforgery cookie
        const getPage = await client.get(LOGIN_URL);

        // --- STEP 2: Extract the __RequestVerificationToken ---
        const $ = cheerio.load(getPage.data);
        const token = $('input[name="__RequestVerificationToken"]').val() as string;

        if (!token) {
            throw new Error("Could not find Antiforgery Token in HTML.");
        }

        // --- STEP 3: POST the Credentials ---
        // We use URLSearchParams to match 'application/x-www-form-urlencoded'
        const params = new URLSearchParams();
        params.append('Username', 'VGPBS80409');
        params.append('Password', 'YOUR_PASSWORD');
        params.append('__RequestVerificationToken', token);

        const response = await client.post(LOGIN_URL, params, {
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'Referer': LOGIN_URL,
                'Origin': 'https://emn.vn'
            },
            maxRedirects: 5 // Allow axios to follow the 302 redirect
        });

        console.log("Status Code:", response.status);
        console.log("Final URL after redirect:", response.config.url);

        return true;
    } catch (error) {
        console.error("Login failed:", error);
        return false;
    }
}
```

---

### 3. Key Integration Details

| **Component**                    | **Purpose**                    | **Requirement**                                                                     |
| -------------------------------- | ------------------------------ | ----------------------------------------------------------------------------------- |
| **`.AspNetCore.Antiforgery`**    | Server-side validation cookie. | Must be captured from the `GET` response and sent back in the `POST`.               |
| **`__RequestVerificationToken`** | Client-side validation token.  | Must be parsed from the hidden input field in the `GET` HTML body.                  |
| **`CookieJar`**                  | Session management.            | Critical because standard `axios` is stateless and will drop cookies between calls. |
| **`URLSearchParams`**            | Data Formatting.               | ASP.NET Core expects `x-www-form-urlencoded`, not `application/json`.               |

---

### 4. How to use the "Value" in your project

Once `loginToEmn()` succeeds, the `jar` (CookieJar) object contains your authenticated session. Any subsequent calls made with the `client` instance will automatically include:

1. The `.AspNetCore.Antiforgery` cookie.
2. The **`.AspNetCore.Identity.Application`** (or similar) session cookie that allows you to access private API data.

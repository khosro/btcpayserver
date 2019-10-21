
NOTICE : The mapped port in firewall must be the same as port in local server.
For example in firewall when we must mapped as following   77.77.77.77:8080 -> 192.168.1.2:8080
Not following  77.77.77.77:8081 -> 192.168.1.2:8080(For exmaple something like we do with RDP connection, that we changed port to connect from outside)

---

1.All the return type for all api has the following format :
```
    {
    errorMessage: []
    hasError: false
    message: []
    model: "" //or model:{}
    serverErrorMessage: []
    }
```
2.All Api url has the following prefix ``` https://{baseurl}/api/v1 ```

---

### /authenticate/register

Send Data(Send as Json -  application/json; charset=utf-8) : 
```
{email = email, password = password}
```
Returns : 
```
{
    errorMessage: []
    hasError: false
    message: null
    model: "username"
    serverErrorMessage:[]
}
```

If error occured :
```
{
    "model":null,
    "message":null,
    "hasError":true,
    "errorMessage":["You can not sign in"],
    "serverErrorMessage":[]
}

```
---
### /authenticate/connect/token

Send Data (FormUrlEncodedContent - application/x-www-form-urlencoded )
```
["grant_type"] = "password",
["username"] = email,
["password"] = password,
["client_id"] = clientId,
["client_secret"] = client_secret,
["scope"] = "openid offline_access",
````

If success : 
```
{
    errorMessage: []
    hasError: false
    message: null
    model:
    access_token: ""
    error: null
    error_description: null
    expires_in: 60
    id_token: ""
    refresh_token: ""
    token_type: "Bearer"
    serverErrorMessage: []
}
```

If error occured :
```
{
    "model":"11",
    "message":null,
    "hasError":true,
    "errorMessage":["Passwords must be at least 6 characters."],
    "serverErrorMessage":[]
}
```

---
### /authenticate/connect/token

Send Data (FormUrlEncodedContent - application/x-www-form-urlencoded) :   
````
["grant_type"] = "refresh_token",
["client_id"] = "clientId",
["client_secret"] = "client_secret",
["refresh_token"] = "refresh_token",
["redirect_uri"] = ""
````
If success : 
```
{
  "model": {
    "error": null,
    "error_description": null,
    "token_type": "Bearer",
    "access_token": "",
    "expires_in": 5,
    "refresh_token": "",
    "id_token": ""
  },
  "message": null,
  "hasError": false,
  "errorMessage": [],
  "serverErrorMessage": []
}

```

If error occured :
```
{
  "model": {
    "error": "invalid_grant",
    "error_description": "The specified refresh token is invalid.",
    "token_type": null,
    "access_token": null,
    "expires_in": null,
    "refresh_token": null,
    "id_token": null
  },
  "message": null,
  "hasError": true,
  "errorMessage": ["The specified refresh token is invalid."],
  "serverErrorMessage": []
}
```
---




***This is for deployer person who deploys API in web server and for who config firewall(This instruction must be test).
NOTICE : The mapped port in firewall must be the same as port in local server.
For example in firewall when we must mapped as following   77.77.77.77:8080 -> 192.168.1.2:8080
Not following  77.77.77.77:8081 -> 192.168.1.2:8080(For exmaple something like we do with RDP connection, that we changed port to connect from outside)***

---

### A. All the return type for all api has the following format :
```
    {
    errorMessages: []
    hasError: false
    messages: []
    model: "" //or model:{} or model: null or model:[]
    serverErrorMessages: []
    }
```
***NOTICE : In the following sections we omit default values when we show API returned values***

###### A.1. If request is OK, status code ```200``` returned
```errorMessages``` and ```serverErrorMessages``` has empty array and ```hasError = false```

###### A.2. If request data has some validation error, status code ```412``` returned
 ```errorMessages``` is filled with array of error and ```hasError = true```

###### A.3. If the there is unhandled exception occured, status code ```500``` returned
 ```serverErrorMessages``` is filled with array of server error and ```hasError = true```

###### A.4. If the api call is authorized protected and caller does not pass the correct token it get ```401```
It is important that the response return type does not obey the default return type such as section A

### B. All Api url has the following prefix ``` https://{baseurl}/api/v1 ```

---

### 1.Register user ```/authenticate/register```

Send Data(Send as Json -  application/json; charset=utf-8) : 
```
{email = email, password = password}
```
Returns : 
```
{
    model: "username",
    ...
}
```

If error occured :
```
{
    "errorMessages":["You can not sign in"],
    ...
}

```
---
### 2.Get the token ```/authenticate/connect/token```

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
    model:{
	    "error": null,
	    "error_description": null,
	    "token_type": "Bearer",
	    "access_token": "value",
	    "expires_in": 60,
	    "refresh_token": "value",
	    "id_token": ""
    },
    ...
}
```

If error occured :
```
{
    "errorMessages":["Passwords must be at least 6 characters."],
    ...
}
```

---
### 3.Get refresh token ```/authenticate/connect/token```

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
    "access_token": "value",
    "expires_in": 5,
    "refresh_token": "value",
    "id_token": ""
  },
  ...
}

```

If error occured :
```
{
  "errorMessages": ["The specified refresh token is invalid."],
  ...
}
```
---



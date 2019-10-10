{baseurl}/api/authenticate/register

Send Data(Send as Json) : 
{email = email, password = password}

Returns : 
{ Status = false, Error = "Error"}

If error occured "Error" has content, otherwise empty

--------------------

#https://{baseurl}/connect/token  #Disabled, but maybe enable again
https://{baseurl}/api/authenticate/connect/token

#Send Data (FormUrlEncodedContent) :  #Disabled, but maybe enable again
Send Data (Send as Json) : 
["grant_type"] = "password",
["username"] = email,
["password"] = password,
["client_id"] = clientId,
["client_secret"] = client_secret,
["scope"] = "openid offline_access",

If success : 
{
  "token_type": "Bearer",
  "access_token": "",
  "expires_in": 60,
  "refresh_token": "",
  "id_token": ""
}

If error occured :
{
  "error": "invalid_grant",
  "error_description": "The specified credentials are invalid."
}

--------------------

#{baseurl}/connect/token   #Disabled, but maybe enable again
{baseurl}/api/authenticate/connect/token

#Send Data (FormUrlEncodedContent) :  #Disabled, but maybe enable again
Send Data (Send as Json) : 
["grant_type"] = "refresh_token",
["client_id"] = "clientId",
["client_secret"] = "client_secret",
["refresh_token"] = "refresh_token",
["redirect_uri"] = ""

If success : 
 {
  "scope": "openid offline_access",
  "token_type": "Bearer",
  "access_token": "",
  "expires_in": 60,
  "refresh_token": "",
  "id_token": ""
}

If error occured :
{
  "error": "",
  "error_description": ""
}





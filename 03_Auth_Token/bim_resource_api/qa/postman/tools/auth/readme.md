# Cognito Authenticator Tool

A command line tool, that returns access_token, id_token or refresh_token from Cognito.

## Required paramters 

- region : AWS Region where the Cognito UserPool is located 

- userPoolId : Cognito User Pool Id 

- clientId : Cognito Application Client Id 

- tokenToReturn : access_token, id_token or refresh_token 

- username: Username exists in User Pool

- password : User password 


## Sample Usage 

```shell
dotnet cognitoauthenticator.dll --region us-east-2 --userPoolId 432234 --clientId 423423 --tokenToReturn id_token --username abc --password 123456

```



 param (
    [Parameter(Mandatory=$true)]
    [string]$environment = "dev",
    [string]$region,
    [string]$userPoolId,
    [string]$clientId,
    [string]$username,
    [string]$password,
    [string]$test 
 )

$cognitoAuthCommand = 'dotnet tools/auth/cognitoauthenticator.dll --region $region --userPoolId $userPoolId --clientId $clientId --tokenToReturn id_token --username $username --password $password'

$id_token = Invoke-Expression $cognitoAuthCommand
#Write-Output $id_token 

$testCommand = 'npm run $test -- --global-var env=$environment --global-var id_token=$id_token'

Invoke-Expression $testCommand




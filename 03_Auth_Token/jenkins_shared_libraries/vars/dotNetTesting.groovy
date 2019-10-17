def call(String directory, String mode='sanity', String creds='cognitoDevCreds') {
    mode = mode.toLowerCase()
    assert mode in ['sanity', 'regression'], 'Invalid selection!  Please select from sanity or regression'

    cognitoDetails = [:]
    cognitoDetails['cognitoDevCreds']   = 'congnitoDEV'
    cognitoDetails['cognitoQaCreds']    = 'congnitoQA'

    String template = '''
mkdir -p results
npm install
npm run test:%s -- --global-var env=$Environment --global-var id_token=%s || true
    '''

    withCredentials([file(credentialsId: cognitoDetails[creds], variable: 'FILE')]) {
        dir (directory) {            
            details = readYaml (file: FILE)

            withCredentials([usernamePassword(credentialsId: creds, usernameVariable: 'cognitoUser', passwordVariable: 'cognitoPass')]) {
                tokensStr = sh(returnStdout: true, script: "aws cognito-idp initiate-auth --client-id ${details.clientid} --auth-flow ${details.authflow} --auth-parameters USERNAME=${cognitoUser},PASSWORD=${cognitoPass} --region ${details.region}")
                tokens =  readJSON text: tokensStr
                //echo tokens.AuthenticationResult.IdToken
                sh String.format(template, mode, tokens.AuthenticationResult.IdToken)
                junit String.format('**/results/%s_*.xml', mode)
            }            
        }        
    }
}
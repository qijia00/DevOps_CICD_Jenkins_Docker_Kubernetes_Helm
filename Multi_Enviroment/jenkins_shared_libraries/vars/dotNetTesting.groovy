def call(String directory, String mode='sanity', String creds='DevAPIUsernamePassword') {
    mode = mode.toLowerCase()
    assert mode in ['sanity', 'regression'], 'Invalid selection!  Please select from sanity or regression'

    String template = '''
mkdir -p results
npm install
npm run test:%s -- --global-var env=$Environment --global-var username=$API_USERNAME --global-var password=$API_PASSWORD || true
    '''

    withCredentials([usernamePassword(credentialsId: creds, usernameVariable: 'API_USERNAME', passwordVariable: "API_PASSWORD")]) {
        dir (directory) {
            sh String.format(template, mode)
        }
        junit String.format('**/%s/results/%s_*.xml', directory, mode)
    }


}
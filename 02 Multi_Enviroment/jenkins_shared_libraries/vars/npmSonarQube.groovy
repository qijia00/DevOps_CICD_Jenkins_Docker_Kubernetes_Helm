def call(Map args) {
    args.directory = args.directory ?: '.'
    args.sonarqubeServer = args.sonarqubeServer ?: 'SonarQubeServer'
    args.sonarqubeTool = args.sonarqubeTool ?: 'Sonar-Scanner'
    
    script {
        scannerHome = tool args.sonarqubeTool
    }

    withSonarQubeEnv(args.sonarqubeServer) {
        dir(args.directory) {
            sh String.format('%s/bin/sonar-scanner', scannerHome)
        }
    }
}
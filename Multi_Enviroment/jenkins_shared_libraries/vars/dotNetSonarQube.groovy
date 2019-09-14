def call(Map args) {
    assert args
    assert args.directory
    assert args.appName
    assert args.appVersion

    args.sonarqubeServer = args.sonarqubeServer ?: 'SonarQubeServer'
    args.sonarqubeTool = args.sonarqubeTool ?: 'Sonar-Dotnet2'
    
    script {
        scannerHome = tool args.sonarqubeTool
    }
    withSonarQubeEnv(args.sonarqubeServer) {
        dir(args.directory) {
            String template = '''
dotnet %s/SonarScanner.MSBuild.dll begin /k:%s
dotnet build /p:version=%s -c Release
dotnet %s/SonarScanner.MSBuild.dll end
            '''

            sh String.format(template,
                             scannerHome,
                             args.appName,
                             args.appVersion,
                             scannerHome)
        }
    }
}
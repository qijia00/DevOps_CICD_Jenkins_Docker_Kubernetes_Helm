def call(Map args) {
    assert args
    assert args.manifest
    assert args.escaped_branch

    withCredentials([string(credentialsId: 'encelium-aws-nuget-api-key', variable: 'API_KEY')]) {
        dir(args.manifest.app.project_folder) {
            //clean the packages folder
            sh 'if [ -d "packages" ]; then rm -Rf packages; fi'
            //create a package 
            script {
                if(env.GIT_BRANCH != 'dev'){  
                    args.nugetVersion = String.format('0.0.0-%s-%s', args.escaped_branch, env.BUILD_NUMBER.padLeft(6, '0'))
                }
                sh String.format('dotnet pack -c Release -o packages --no-build -p:PackageVersion="%s"', args.nugetVersion)
            }
        } 
            

        dir(args.manifest.app.project_folder + '/packages') { 
            echo "${env.AWS_NUGET_URL}"
            sh String.format('dotnet nuget push *.nupkg -k %s -s %s', API_KEY, env.AWS_NUGET_URL)
        }

    }
}
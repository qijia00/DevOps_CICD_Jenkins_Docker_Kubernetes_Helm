def call(String job, String branch='dev') {
    script {
        if(env.GIT_BRANCH == branch){
            build job: job, wait:false, propagate:false, parameters: [[$class: 'StringParameterValue', name: 'Input_Build_Number', value: env.BUILD_NUMBER]]
        }
    }
}
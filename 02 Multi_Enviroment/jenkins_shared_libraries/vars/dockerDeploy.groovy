import com.encelium.jenkins.Docker

def call(Map args) {
    assert args
    assert args.manifest
    assert args.escaped_branch
    assert args.appVersion

    args.build = args.build ?: 'Release'
    args.branch = args.branch ?: 'dev'
    args.target = args.target ?: 'config'
    args.directory = args.directory ?: args.manifest.app.project_folder

    docker = new Docker(this)

    withCredentials([[$class: 'AmazonWebServicesCredentialsBinding', credentialsId: 'Jenkins']]) {
        sh '\$(aws ecr get-login --no-include-email --region us-east-2)'
        dir(args.directory) {
            sh String.format('aws ecr create-repository --region us-east-2 --repository-name %s || true',
                             args.manifest[args.target].dockerimage.split('/').drop(1).join('/'))
            
            script {
                if(env.GIT_BRANCH == args.branch){
                    docker.publish(args.manifest[args.target].dockerimage, String.format('%s.%s', args.escaped_branch, args.appVersion))
                } else {
                    docker.publish(args.manifest[args.target].dockerimage, args.escaped_branch)
                }
            }
        }
    }
}
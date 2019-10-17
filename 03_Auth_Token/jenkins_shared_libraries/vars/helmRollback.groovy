def call(String name) {
    withCredentials([[$class: 'AmazonWebServicesCredentialsBinding', credentialsId: 'Jenkins']]) {
        sh String.format('aws eks update-kubeconfig --name %s --region %s',
                         'encelium-dev-apps',
                         'us-east-2')
        sh String.format('helm rollback %s 0 --recreate-pods --wait', name)
    }
}
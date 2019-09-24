import com.encelium.jenkins.Helm

def call(Map args) {
    assert args
    assert args.manifest
        
    args.target = args.target ?: 'config'
    args.chart_dir = args.chart_dir ?: args.manifest[args.target].helm.folder
    args.release_name = args.release_name ?: String.format('%s-%s',
                                                           args['set.environment'],
                                                           args.manifest[args.target].helm.name)
    args.dry_run = args.dry_run ?: false
    args['set.appName'] = args['set.appName'] ?: args.manifest[args.target].helm.name
    args['set.image.repository'] = args['set.image.repository'] ?: args.manifest[args.target].dockerimage

    helm = new Helm(this)

    withCredentials([[$class: 'AmazonWebServicesCredentialsBinding', credentialsId: 'Jenkins']]) {
        sh String.format('aws eks update-kubeconfig --name %s --region %s',
                         args.manifest.aws.eks.cluster_name,
                         args.manifest.aws.region)
        
        dir (args.manifest[args.target].root_folder) {
            helm.deploy(args)
        }
    }
}
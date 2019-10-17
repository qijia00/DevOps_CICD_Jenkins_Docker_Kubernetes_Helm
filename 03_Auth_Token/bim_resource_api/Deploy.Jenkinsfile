@Library('jenkins-shared-libraries@dev') _

// Required environment variables: Environment, Namespace
pipeline {
    agent any
    stages {
        stage ('Import YAML manifest') {
            steps {
                script {
                    manifest = readYaml (file: 'build.yaml')                    

                    // Initialize script variables
                    Namespace = "${ENVIRONMENT}-${NAMESPACE}"
					appVersion = manifest.app.version + '-' + "${env.Input_Build_Number}".padLeft(6,"0")
                    dockerLabel = escapedBranch()
                    image_tag = getDockerLabel(dockerLabel, appVersion)
					app_name = manifest.app.name
                    ReleaseName = "${ENVIRONMENT}-${manifest.config.helm.name}"
                    notificationLabel = "${Namespace} ${manifest.app.name}-${appVersion}"
                }
            }
        }

        stage ('Test App Deployment') {
            steps {
                echo "-------------------------------------------------------------------------------------"
                helmDeploy(
                    manifest                : manifest,
                    dry_run                 : true,
                    release_name            : ReleaseName,
                    namespace               : Namespace,
                    'set.appName'           : app_name,
                    'set.environment'       : "${ENVIRONMENT}",
                    'set.namespace'         : Namespace,
                    'set.imagePullSecrets'  : manifest.aws.ecr.secret,
                    'set.image.tag'         : image_tag,
                    'set.image.repository'  : manifest.config.dockerimage
                )           
            }
        }

        stage ('Deploy App') {
            steps {
                echo "-------------------------------------------------------------------------------------"
                helmDeploy(
                    manifest                : manifest,
                    release_name            : ReleaseName,
                    namespace               : Namespace,
                    'set.appName'           : app_name,
                    'set.environment'       : "${ENVIRONMENT}",
                    'set.namespace'         : Namespace,
                    'set.imagePullSecrets'  : manifest.aws.ecr.secret,
                    'set.image.tag'         : image_tag,
                    'set.image.repository'  : manifest.config.dockerimage
                )                
            }
        }

        stage ('Sanity Test') {
            steps {
                script {
                    if (params.Environment == 'dev') {
                        echo "--------------------- debug line in Jenkins Sanity dev ---------------------"
                        dotNetTesting(manifest.tests.integration, 'sanity', 'cognitoDevCreds')
                    } else if (params.Environment == 'qa') {
                        echo "--------------------- debug line in Jenkins Sanity qa ---------------------"
                        dotNetTesting(manifest.tests.integration, 'sanity', 'cognitoQaCreds')
                    }
                }
            }                      
        }

        stage ('Regression Test' ) {
            when {
                expression { params.Environment == 'qa' }
            }
            steps {
                echo "--------------------- debug line in Jenkins Regression qa ---------------------"
                dotNetTesting(manifest.tests.integration, 'regression', 'cognitoQaCreds')
            }            
        }
    }

    post { 
        success {
            notify('success', notificationLabel)
        }
        unstable {
			helmRollback(ReleaseName)
			notify('unstable', notificationLabel)
        }
        failure {
            helmRollback(ReleaseName)
            notify('fail', notificationLabel)
        }
    }
}

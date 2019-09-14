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
                    if (params.Environment == 'dev'} {
                        echo "----------------------------------------------------------------------------"
                        dotNetTesting(manifest.tests.integration, 'sanity')
                    } else if (params.Environment == 'qa') {
                        echo "----------------------------------------------------------------------------"
                        dotNetTesting(manifest.tests.integration, 'sanity', 'QAAPIUsernamePassword')
                    }
                }
            }                      
        }

        stage ('Regression Test - NOT IMPLEMENTED') {
            when {
                expression { params.Environment == 'qa' }
            }
            steps {
                echo "----------------------------------------------------------------------------"
                // dotNetTesting(manifest.tests.integration, 'regression', 'QAAPIUsernamePassword')
            }            
        }
    }

    post { 
        success {
            notify('success', notificationLabel)
        }
        unstable {
			notify('unstable', notificationLabel)
        }
        failure {
            helmRollback(ReleaseName)
            notify('fail', notificationLabel)
        }
    }
}

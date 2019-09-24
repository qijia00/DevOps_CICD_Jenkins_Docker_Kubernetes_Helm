// Required environment variables: Environment, Namespace

pipeline {
    agent any
    stages {
        stage ('Import YAML manifest') {
            steps {
                script {
                    manifest = readYaml (file: 'build.yaml')
					_namespace = "${ENVIRONMENT}-${NAMESPACE}"
					appVersion = manifest.app.version + '-' + "${env.Input_Build_Number}".padLeft(6,"0")				
                }
            }
        }

        stage ('Prepare environment') {
            steps {
                echo "-------------------------------------------------------------------------------------"
                withCredentials([azureServicePrincipal(manifest.azure.credentials.main.toString())]) {
                    sh "set +x"
                    sh "az login --service-principal -u ${AZURE_CLIENT_ID} -p ${AZURE_CLIENT_SECRET} -t ${AZURE_TENANT_ID}"
                    sh "az account set -s '${manifest.azure.subscription.toString()}'"
                    sh "set -x"
                    sh "az aks get-credentials --overwrite-existing --resource-group ${manifest.azure.aks.resource_group.toString()} --name ${manifest.azure.aks.resource_namespace.toString()}"
                }
            }
        }

        /* stage ('Terraform Infrastructure') {
            environment {
                AZURE_ACCESS_KEY = credentials('AzureStorageDevops')
                AZURE_PKEY = credentials('AzureTFStoragePkey')
            }

            steps {
                echo "-------------------------------------------------------------------------------------"
                dir('deployment/infrastructure') {
                    withCredentials([azureServicePrincipal('AzureTFStorage')]) {
                        sh "terraform init -backend-config='access_key=${AZURE_ACCESS_KEY}'"
                        sh "terraform plan -var 'client_id=${AZURE_CLIENT_ID}' -var 'client_secret=${AZURE_CLIENT_SECRET}' -var 'ssh_public_key=${AZURE_PKEY}' -out=plan"
                        sh "terraform apply -auto-approve plan"
                    }
                }
            }
        } */

        stage ('Test App Deployment') {
            environment {
                dockerLabel = escapedBranch("${env.GIT_BRANCH}")
				app_version = "${appVersion}"
				image_tag = "${env.dockerLabel}" + '.' + "${env.app_version}"
                // HostUrl = "${ENVIRONMENT}-${manifest.app.name.toString()}.${manifest.azure.aks.dns_host_zone.toString()}"
                Namespace = "${_namespace}"
                ReleaseName = "${ENVIRONMENT}-${manifest.config.helm.name.toString()}"
            }
            steps {
                echo "-------------------------------------------------------------------------------------"

                dir (manifest.config.root_folder.toString()) {
                    helmLint(manifest.config.helm.folder.toString())

                    // run dry-run helm chart installation
                    helmDeploy(
                        dry_run       : true,
                        release_name  : "${env.ReleaseName}",
                        chart_dir     : manifest.config.helm.folder.toString(), 
                        app_name      : manifest.app.name.toString(),
                        env           : "${ENVIRONMENT}",
                        imageTag      : "${image_tag}",
                        namespace     : "${env.Namespace}",
                        // hostUrl       : "${env.HostUrl}",
                        dbConnection  : manifest.service.db.toString()
                    )
                }
            }
        }

        stage ('Deploy App') {
            environment {
                dockerLabel = escapedBranch("${env.GIT_BRANCH}")
				app_version = "${appVersion}"
				image_tag = "${env.dockerLabel}" + '.' + "${env.app_version}"
                // HostUrl = "${ENVIRONMENT}-${manifest.app.name.toString()}.${manifest.azure.aks.dns_host_zone.toString()}"
                Namespace = "${_namespace}"
                ReleaseName = "${ENVIRONMENT}-${manifest.config.helm.name.toString()}"
            }
            steps {
                echo "-------------------------------------------------------------------------------------"

                dir ("terraform_app") {
                    helmDeploy(
                    dry_run       : false,
                    release_name  : "${env.ReleaseName}",
                    chart_dir     : manifest.config.helm.folder.toString(),
                    app_name      : manifest.app.name.toString(),
                    env           : "${ENVIRONMENT}",
                    imageTag      : "${image_tag}",
                    namespace     : "${env.Namespace}",
                    // hostUrl       : "${env.HostUrl}",
                    dbConnection  : manifest.service.db.toString()
                    )
                }
            }
        }

        stage ('Sanity Test') {
            when {
                expression { params.Environment == 'dev' || params.Environment == 'qa'}
            }
            steps {
                echo "----------------------------------------------------------------------------"

                dir (manifest.tests.integration.toString()) {
                    sh '''
                        mkdir -p results
                        npm install
                        npm run test:sanity || true
                    '''
                }
               
                junit "**/${manifest.tests.integration.toString()}/results/sanity_*.xml"
            }            
        }
        stage ('Regression Test') {
            when {
                expression { params.Environment == 'qa' }
            }
            steps {
                echo "----------------------------------------------------------------------------"

                dir (manifest.tests.integration.toString()) {
                    sh '''
                        mkdir -p results
                        npm install
                        npm run test:regression || true
                    '''
                }
               
                junit "**/${manifest.tests.integration.toString()}/results/regression_*.xml"
            }            
        }
    }

    post { 
        success {
            notifySuccessful()
        }
        unstable {
            // sh "helm rollback ${ENVIRONMENT}-${manifest.config.helm.name.toString()} 0 --recreate-pods --wait"
            notifyUnstable()
        }
        failure {
			// ReleaseName = "${ENVIRONMENT}-${manifest.config.helm.name.toString()}".
			// ReleaseName can not passed in directly, so we are passing in its value.
            sh "helm rollback ${ENVIRONMENT}-${manifest.config.helm.name.toString()} 0 --recreate-pods --wait"
            notifyFailed()
        }
    }
}

String escapedBranch(String branch) {
    return branch.replace("/", "_").replace("origin_", "")
}

void helmLint(String chart_dir) {
    // lint helm chart
    sh "helm lint '${chart_dir}'"
}

void helmDeploy(Map args) {
    //configure helm client and confirm tiller process is installed

    if (args.dry_run) {
        println "Running dry-run deployment"

        sh "helm upgrade --wait --recreate-pods --dry-run --debug --install ${args.release_name} '${args.chart_dir}' --set environment=${args.env},image.tag=${args.imageTag},appconfig.dbConnection='${args.dbConnection}',namespace=${args.namespace},appName=${args.app_name} --namespace=${args.namespace}"
    } else {
        println "Running deployment"
        sh "helm upgrade --wait --recreate-pods --install ${args.release_name} '${args.chart_dir}' --set environment=${args.env},image.tag=${args.imageTag},appconfig.dbConnection='${args.dbConnection}',namespace=${args.namespace},appName=${args.app_name} --namespace=${args.namespace}"

        echo "Application ${args.release_name} successfully deployed."
    }
}

void notifyStarted() {
    slackSend (color: '#FFFF00', message: "DEPLOYMENT STARTED: ${ENVIRONMENT}-${NAMESPACE} ${manifest.app.name.toString()}-${appVersion} | git commit: ${env.GIT_COMMIT.take(7)}\nBuild Link: <${env.BUILD_URL}|jenkins [${env.BUILD_NUMBER}]>\nAPI Docs: <http://138.91.117.215:5000/swagger/index.html|Swagger>")
}

void notifySuccessful() {
    slackSend (color: '#00FF00', message: "DEPLOYED: ${ENVIRONMENT}-${NAMESPACE} ${manifest.app.name.toString()}-${appVersion} | git commit: ${env.GIT_COMMIT.take(7)}\nBuild Link: <${env.BUILD_URL}|jenkins [${env.BUILD_NUMBER}]>\nAPI Docs: <http://138.91.117.215:5000/swagger/index.html|Swagger>")
}

void notifyUnstable() {
    slackSend (color: '#FFA500', message: "DEPLOYMENT UNSTABLE: ${ENVIRONMENT}-${NAMESPACE} ${manifest.app.name.toString()}-${appVersion} | git commit: ${env.GIT_COMMIT.take(7)}\nBuild Link: <${env.BUILD_URL}|jenkins [${env.BUILD_NUMBER}]>\nAPI Docs: <http://138.91.117.215:5000/swagger/index.html|Swagger>")
}

void notifyFailed() {
    slackSend (color: '#FF0000', message: "DEPLOYMENT FAILED: ROLLBACK ${ENVIRONMENT}-${NAMESPACE} ${manifest.app.name.toString()}-${appVersion} | git commit: ${env.GIT_COMMIT.take(7)}\nBuild Link: <${env.BUILD_URL}|jenkins [${env.BUILD_NUMBER}]>\nAPI Docs: <http://138.91.117.215:5000/swagger/index.html|Swagger>")
}

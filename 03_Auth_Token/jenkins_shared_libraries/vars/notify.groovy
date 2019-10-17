import com.encelium.jenkins.Notifications

def call(String status, String environment, String notes='') {    
    notification = new Notifications(this)

    gitChangelist = sh(returnStdout: true, script: "git shortlog ${env.GIT_PREVIOUS_COMMIT}..${env.GIT_COMMIT}  --no-merges")
    
    payload = notes ?: gitChangelist
    notification.deployStatus(status, environment, payload)
}
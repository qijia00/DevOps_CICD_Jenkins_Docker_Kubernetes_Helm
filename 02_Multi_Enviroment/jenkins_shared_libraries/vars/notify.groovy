import com.encelium.jenkins.Notifications

def call(String status, String environment, String notes='') {    
    notification = new Notifications(this)
    notification.deployStatus(status, environment, notes)
}
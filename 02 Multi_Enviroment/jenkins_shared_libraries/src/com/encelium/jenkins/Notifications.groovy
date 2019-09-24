package com.encelium.jenkins

class Notifications implements Serializable {

    private final def script
    private def status

    Notifications(def script) {
        this.script = script
        
        this.status = [:]
        this.status['start']        = ['color':'#FFFF00', 'message':'DEPLOYMENT STARTED: ']
        this.status['success']      = ['color':'#00FF00', 'message':'DEPLOYED: ']
        this.status['unstable']     = ['color':'#FFA500', 'message':'DEPLOYMENT UNSTABLE: ']
        this.status['fail']         = ['color':'#FF0000', 'message':'DEPLOYMENT FAILED: ROLLBACK ']
        this.status['nuget deploy'] = ['color':'#00FF00', 'message':'NUGET DEPLOYED: ']        
        this.status['nuget fail']   = ['color':'#FF0000', 'message':'NUGET FAILED: ']
    }

    public void deployStatus(String status, String environment, String notes='') {
        assert this.status.containsKey(status.toLowerCase()) : String.format('Please select from the following statuses %s', this.status.keySet())

        def template = '''
%s %s
git commit: %s
Build Link: <%s|jenkins [%s]>
Notes: %s \n
        '''

        def payload = String.format(template,
                                    this.status[status].message,
                                    environment,
                                    this.script.env.GIT_COMMIT.take(7),
                                    this.script.env.BUILD_URL,
                                    this.script.env.BUILD_NUMBER,
                                    notes)

        this.script.slackSend(color: this.status[status].color,
                              message: payload)
    }
}

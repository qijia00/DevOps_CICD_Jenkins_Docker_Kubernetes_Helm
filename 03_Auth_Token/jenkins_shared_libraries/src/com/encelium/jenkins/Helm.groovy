package com.encelium.jenkins

class Helm implements Serializable {

    private final def script

    Helm(def script) {
        this.script = script
    }

    public void lint(String chart_dir) {
        // lint helm chart
        this.script.sh String.format('helm lint %s', chart_dir)
    }

    public void deploy(Map args) {
        //configure helm client and confirm tiller process is installed
        
        def template = 'helm upgrade --wait --recreate-pods --debug --install %s %s'
        def payload = String.format(template, args.release_name, args.chart_dir)

        if (args.file) {
            payload += String.format(' -f %s', args.file)
        }

        if (args.dry_run) {
            this.lint(args.chart_dir)

            payload += ' --dry-run'
            this.script.echo 'Running dry-run deployment'
        }
        else {
            this.script.echo 'Running deployment'
        }

        args.remove('manifest')        
        args.remove('release_name')
        args.remove('chart_dir')
        args.remove('dry_run')
        args.remove('target')
        args.remove('file')

        for (item in args) {
            if (item.key.contains('set.')) {
                payload += String.format(' --set %s=%s', item.key - 'set.', item.value) 
            }
            else {
                payload += String.format(' --%s=%s', item.key, item.value)
            }
        }

        this.script.sh payload
        this.script.echo 'Application successfully deployed'
    }
}

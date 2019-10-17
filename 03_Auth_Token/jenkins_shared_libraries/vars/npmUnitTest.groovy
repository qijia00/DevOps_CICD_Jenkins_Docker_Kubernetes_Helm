def call(Map args) {
    assert args
    assert args.directory

    args.build = args.build ?: 'Release'

    dir(args.directory) {
        sh 'npm test'

        if (fileExists('test-coverage/cobertura-coverage.xml')) {
            step([$class: 'CoberturaPublisher', coberturaReportFile: 'test-coverage/cobertura-coverage.xml']) 
        }
    }
}
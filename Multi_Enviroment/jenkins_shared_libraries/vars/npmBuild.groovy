def call(Map args) {
    assert args
    assert args.directory

    args.build = args.build ?: 'Release'

    dir(args.directory) {
        sh 'npm install'
        sh 'npm run-script build'
    }
}
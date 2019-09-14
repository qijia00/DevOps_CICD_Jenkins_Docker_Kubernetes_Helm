def call(Map args) { 
    assert args
    assert args.directory

    args.build = args.build ?: 'Release'

    dir(args.directory) {
        sh String.format('dotnet test -c %s --no-build', args.build)
    }
}
def call(Map args) {
    assert args
    assert args.manifest
    assert args.version

    args.build = args.build ?: 'Release'

    dir(args.manifest.app.solution_folder) {
        sh 'dotnet restore'
        sh 'dotnet clean'
        sh String.format('dotnet build -m /p:version=%s -c %s', args.version, args.build)
    }
    dir(args.manifest.app.project_folder) {
        sh String.format('dotnet publish -c %s -o publish --no-build', args.build)
    }

    if(args.aux_project) {
        dir(args.manifest[args.aux_project].project_folder) {
            sh String.format('dotnet publish -c %s -o publish --no-build', args.build)
        }
    }
}
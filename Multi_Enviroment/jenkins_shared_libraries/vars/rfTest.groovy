def call(Map args) {
    assert args
    assert args.manifest
    assert args.url
    
    Map mode = ['sanity'        : 'sanity_xunit.xml',
                'regression'    : 'regression_xunit.xml']

    args.mode = args.mode.toLowerCase() ?: 'sanity'
    args.browser = args.browser ?: 'chrome'

    assert args.mode in ['sanity', 'regression'], 'Invalid selection!  Please select from sanity or regression'

    dir(args.manifest.tests.integration) {
        String template = 'robot --xunit %s --include %s --variable SERVER:%s --variable BROWSER:%s *.robot || true'

        sh String.format(template,
                         mode[args.mode],
                         args.mode,
                         args.url,
                         args.browser)

        junit mode[args.mode]

        step([$class: 'RobotPublisher',
              outputPath: '',
              outputFileName: 'output.xml',
              reportFileName: 'report.html',
              logFileName: 'log.html',
              otherFiles: '',
              disableArchiveOutput: false,
              enableCache: true
            ])
    }
}
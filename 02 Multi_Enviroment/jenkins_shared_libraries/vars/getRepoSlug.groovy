def call() {
    return String.format('%s', env.GIT_URL).split('/')[1].replace('.git','')
}
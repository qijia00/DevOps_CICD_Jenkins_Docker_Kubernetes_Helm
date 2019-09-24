def call(String escape="_") {
    String branch = env.BRANCH_NAME ?: String.format('%s', env.GIT_BRANCH).replace('origin/', '')
    return branch.replaceAll("[/_]", escape) 
}
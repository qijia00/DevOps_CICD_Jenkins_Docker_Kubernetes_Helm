package com.encelium.jenkins

class Git implements Serializable {

    private final def script

    Git(def script) {
        this.script = script
    }

    String escapedBranch(String escape="_") {
        String branch = script.env.BRANCH_NAME
        return branch.replaceAll("[/_]", escape) 
    }
}

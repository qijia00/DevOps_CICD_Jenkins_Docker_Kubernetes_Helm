package com.encelium.jenkins

class Docker implements Serializable {

    private final def script

    Docker(def script) {
        this.script = script
    }

    public void publish(String image, String tag, String directory='.') {
        this.script.sh String.format('docker build -t %s:%s %s', image, tag, directory)
        this.script.sh String.format('docker push %s:%s', image, tag)
    }
}
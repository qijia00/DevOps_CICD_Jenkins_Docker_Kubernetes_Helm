def call(String job) {
    tempValue = job.replace("/", "-")
    //get the first _
    index = tempValue.indexOf("-")
    String newName = tempValue.substring(0,index)+'/'+tempValue.substring(index+1)
    return newName + "-deploy"
}

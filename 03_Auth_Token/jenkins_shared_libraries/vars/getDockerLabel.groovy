def call(String label, String version='') {
    String retval = label == 'dev' ? String.format('%s.%s', label, version) : label
    return retval
}

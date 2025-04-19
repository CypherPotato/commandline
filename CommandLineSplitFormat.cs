namespace CommandLine {

    /// <summary>
    /// Specifies the format to use when splitting command lines.
    /// </summary>
    public enum CommandLineSplitFormat {

        /// <summary>
        /// Automatically detects the command line format based on the input.
        /// </summary>
        AutoDetect,

        /// <summary>
        /// Bash command line format, typically used on Unix-like systems.
        /// </summary>
        Bash,

        /// <summary>
        /// Windows command line format, typically used on Windows systems.
        /// </summary>
        Windows
    }
}

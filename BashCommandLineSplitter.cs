using System;
using System.Collections.Generic;
using System.Text;

static class BashCommandLineParser {

    public static string [] Split ( string commandLine ) {
        if (string.IsNullOrWhiteSpace ( commandLine )) {
            return Array.Empty<string> ();
        }

        var args = new List<string> ();
        var currentArg = new StringBuilder ();
        bool inDoubleQuotes = false;
        bool inSingleQuotes = false;
        bool argumentStarted = false;

        commandLine = commandLine.Replace ( "\r\n", "\n" );

        for (int i = 0; i < commandLine.Length; i++) {
            char c = commandLine [ i ];

            if (inSingleQuotes) {
                if (c == '\'') {
                    inSingleQuotes = false;
                }
                else {
                    currentArg.Append ( c );
                    argumentStarted = true;
                }
            }
            else if (inDoubleQuotes) {
                if (c == '"') {
                    inDoubleQuotes = false;
                }
                else if (c == '\\') {
                    i++;
                    if (i < commandLine.Length) {
                        char nextChar = commandLine [ i ];
                        if (nextChar == '$' || nextChar == '`' || nextChar == '"' || nextChar == '\\') {
                            currentArg.Append ( nextChar );
                            argumentStarted = true;
                        }
                        else if (nextChar == '\n') {
                            argumentStarted = true;
                        }
                        else {
                            currentArg.Append ( '\\' );
                            currentArg.Append ( nextChar );
                            argumentStarted = true;
                        }
                    }
                    else {
                        currentArg.Append ( '\\' );
                        argumentStarted = true;
                    }
                }
                else {
                    currentArg.Append ( c );
                    argumentStarted = true;
                }
            }
            else {
                if (c == '\\') {
                    i++;
                    if (i < commandLine.Length) {
                        char nextChar = commandLine [ i ];
                        if (nextChar == '\n') {
                            if (currentArg.Length > 0)
                                argumentStarted = true;
                        }
                        else {
                            currentArg.Append ( nextChar );
                            argumentStarted = true;
                        }
                    }
                    else {
                        currentArg.Append ( '\\' );
                        argumentStarted = true;
                    }
                }
                else if (c == '\'') {
                    inSingleQuotes = true;
                    argumentStarted = true;
                }
                else if (c == '"') {
                    inDoubleQuotes = true;
                    argumentStarted = true;
                }
                else if (char.IsWhiteSpace ( c )) {
                    if (argumentStarted) {
                        args.Add ( currentArg.ToString () );
                        currentArg.Clear ();
                        argumentStarted = false;
                    }
                }
                else {
                    currentArg.Append ( c );
                    argumentStarted = true;
                }
            }
        }

        if (argumentStarted) {
            args.Add ( currentArg.ToString () );
        }

        if (inSingleQuotes || inDoubleQuotes) {
            Console.Error.WriteLine ( "Aviso: Aspas não fechadas na linha de comando." );
        }


        return args.ToArray ();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

static class WindowsCommandLineParser {

    public static string [] Split ( string commandLine ) {
        if (string.IsNullOrEmpty ( commandLine )) {
            return Array.Empty<string> ();
        }

        var args = new List<string> ();
        var currentArg = new StringBuilder ();
        bool inQuotes = false;
        bool argumentStarted = false;

        for (int i = 0; i < commandLine.Length; i++) {
            char c = commandLine [ i ];

            if (c == '\\') {
                int numBackslashes = 0;
                int j = i;
                while (j < commandLine.Length && commandLine [ j ] == '\\') {
                    numBackslashes++;
                    j++;
                }

                if (j < commandLine.Length && commandLine [ j ] == '"') {
                    currentArg.Append ( '\\', numBackslashes / 2 );
                    argumentStarted = true;
                    if (numBackslashes % 2 == 1) {
                        currentArg.Append ( '"' );
                        i = j;
                    }
                    else {
                        i = j - 1;
                    }
                }
                else {
                    currentArg.Append ( '\\', numBackslashes );
                    argumentStarted = true;
                    i = j - 1;
                }
            }
            else if (c == '"') {
                inQuotes = !inQuotes;
                argumentStarted = true;
            }
            else if (char.IsWhiteSpace ( c ) && !inQuotes) {
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

        if (argumentStarted) {
            args.Add ( currentArg.ToString () );
        }

        if (inQuotes) {
            Console.Error.WriteLine ( "Aviso: Aspas não fechadas na linha de comando (estilo Windows)." );
        }

        return args.ToArray ();
    }
}

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace CommandLine {
    static class AutoDetectCommandLineParser {
        public static string [] Split ( string commandLine ) {
            // --- Heurísticas de Detecção ---

            bool hasSingleQuotes = commandLine.Contains ( '\'' );
            bool hasBashStyleEscape = Regex.IsMatch ( commandLine, @"\\[^""\\\n]" );
            // Verifica continuação de linha do Bash
            bool hasBashLineContinuation = commandLine.Contains ( "\\\n" );
            // Verifica variável estilo Bash ($VAR ou ${VAR})
            bool hasBashVariable = Regex.IsMatch ( commandLine, @"\$(?:[a-zA-Z_][a-zA-Z0-9_]*|\{[a-zA-Z_][a-zA-Z0-9_]*\})" );

            bool looksLikeBash = hasSingleQuotes || hasBashStyleEscape || hasBashLineContinuation || hasBashVariable;

            // Verifica variável estilo Windows (%VAR%)
            bool hasWindowsVariable = Regex.IsMatch ( commandLine, @"%[^%]+%" );
            // Verifica letra de unidade (C:\) - mais robusto para evitar falsos positivos
            bool hasDriveLetter = Regex.IsMatch ( commandLine, @"^[a-zA-Z]:\\| [a-zA-Z]:\\" );
            // Verifica caminho UNC (\\server) - início da string ou após espaço
            bool hasUncPath = Regex.IsMatch ( commandLine, @"^(?:\\\\)|(?: \\\\)" );
            // Verifica barra invertida usada possivelmente como separador de caminho
            // (não seguida por ", \, n, espaço ou fim da string) - heurística mais fraca
            bool hasProbablePathSeparator = Regex.IsMatch ( commandLine, @"\\[^""\\\n\s]" );

            bool looksLikeWindows = hasWindowsVariable || hasDriveLetter || hasUncPath || hasProbablePathSeparator;

            // --- Decisão ---

            if (looksLikeBash && !looksLikeWindows) {
                // Console.WriteLine($"// AutoDetect: Bash ({ (hasSingleQuotes?"' ": "") + (hasBashStyleEscape?"\\e ": "") + (hasBashLineContinuation?"\\n ": "") + (hasBashVariable?"$V ": "")})");
                return BashCommandLineParser.Split ( commandLine );
            }
            else if (looksLikeWindows && !looksLikeBash) {
                // Console.WriteLine($"// AutoDetect: Windows ({ (hasWindowsVariable?"%V% ": "") + (hasDriveLetter?"C:\\ ": "") + (hasUncPath?"\\\\S ": "") + (hasProbablePathSeparator?"\\p ": "")})");
                return WindowsCommandLineParser.Split ( commandLine );
            }
            else if (looksLikeBash && looksLikeWindows) {
                // AMBÍGUO: Contém características de ambos.
                // O que fazer?
                // Opção 1: Priorizar um (Bash tem características mais distintas?)
                // Opção 2: Usar o OS atual como desempate.
                // Opção 3: Lançar erro.

                bool isWindowsOS = RuntimeInformation.IsOSPlatform ( OSPlatform.Windows );
                return isWindowsOS ? WindowsCommandLineParser.Split ( commandLine ) : BashCommandLineParser.Split ( commandLine );
            }
            else {
                // NENHUMA CARACTERÍSTICA CLARA: Comando simples como "programa arg1 arg2"
                // Opção mais segura: usar o padrão do sistema operacional onde o código está rodando.
                // Console.WriteLine("// AutoDetect: Nenhuma característica clara. Usando OS Padrão.");

                bool isWindowsOS = RuntimeInformation.IsOSPlatform ( OSPlatform.Windows );
                return isWindowsOS ? WindowsCommandLineParser.Split ( commandLine ) : BashCommandLineParser.Split ( commandLine );
            }
        }
    }
}

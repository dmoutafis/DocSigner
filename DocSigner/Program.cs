using System;

namespace DocSigner
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            var logFile = "DocSigner.log";
            var log = new Logger(logFile);

            // Log the start of the program
            log.ToFile("Program started.");

            // Start the signing process
            var signingProcess = new ProcessCore();
            signingProcess.Execute();

            // Log the end of the program
            log.ToFile("Program ended.");
        }
    }
}
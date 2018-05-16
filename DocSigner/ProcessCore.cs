using System;

namespace DocSigner
{
    class ProcessCore
    {
        public void Execute()
        {
            Console.Clear();
            Console.Title = "pdf document signing.";
            Console.WriteLine("Selecting  file...");

            // Perform signing of the file
            var logFile = "DocSigner.log";
            var file = new FileSelector();
            var pdf = new PdfManipulator();

            pdf.PerformSign(file.Select(), logFile); // Use null instead for '1234' for not password-protect the pdf
        }
    }
}
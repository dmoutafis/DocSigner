using System;
using System.Windows.Forms;

namespace DocSigner
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            Console.WriteLine("Selecting  file...");

            // Perform signing of the file
            var file = new FileSelector();
            var pdf = new PdfManipulator();

            pdf.PerformSign(file.Select());
            
            Application.Exit();
        }
    }
}
namespace DocSigner
{
    using System;
    using System.IO;

    public class Logger
    {
        private string _logfile;

        public Logger(string logfile)
        {
            _logfile = logfile;
        }

        public void ToFile(string message)
        {
            // if the certificate's friendly name contains "'s", remove this
            if (message.Contains("'s"))
            {
                message = message.Replace("'s", "");
            }

            using (StreamWriter lf = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + _logfile, true))
            {
                if (message.Contains("ended"))
                    lf.Write("{0}: {1}", DateTime.Now, message);
                else
                    lf.WriteLine("{0}: {1}", DateTime.Now, message);
            }
        }
    }
}
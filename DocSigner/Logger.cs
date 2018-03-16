namespace DocSigner
{
    using System;
    using System.IO;

    public class Logger
    {
        public void ToFile(string message, bool enableLog)
        {
            if (enableLog == false) return;

            // if the certificate's friendly name contains "'s", we remove this
            if (message.Contains("'s"))
            {
                message = message.Replace("'s", "");
            }

            using (StreamWriter lf = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\logger.log", true))
            {
                lf.WriteLine("{0} ==> {1}", DateTime.Now, message);
            }
        }
    }
}
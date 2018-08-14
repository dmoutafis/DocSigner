using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;

using Org.BouncyCastle.Security;

using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace DocSigner
{
    public class PdfManipulator
    {
        private string _signedFile { get; set; }
        private IList<ICrlClient> _crlList { get; set; }
        private ITSAClient _tsaClient { get; set; }
        private IOcspClient _ocspClient { get; set; }
        private IList<X509Certificate> _chain { get; set; }
        private X509Certificate2 _pk { get; set; }
        private X509Certificate2Collection _collection { get; set; }
        private X509Certificate _cert { get; set; }
        private string _thumbprint { get; set; }
        private string _destPath { get; set; }
        private string _logfile { get; set; }
        private string _myReason { get; set; }
        private string _password { get; set; }

        public PdfManipulator()
        {
            _pk = null;
            _collection = null;
            _ocspClient = null;
            _signedFile = string.Empty;

            _thumbprint = Folders.ToraDirectCertificateThumbprint;
            //_thumbprint = Folders.ToraWalletCertificateThumbprint;

            _chain = new List<X509Certificate>();
        }

        public void PerformSign(string fileToBeSigned, string logfile)
        {
            _logfile = logfile;
            _myReason = "";
            _password = "";

            var log = new Logger(_logfile);
            log.ToFile("Signing process started.");

            if (string.IsNullOrEmpty(fileToBeSigned))
            {
                log.ToFile("No file selected!");
                Console.WriteLine("No file selected!");
                return;
            }

            _destPath = Path.GetDirectoryName(fileToBeSigned) + "\\";

            try
            {
                // Pick a signing certificate from specific thumbprint in 'my store'
                GetCertificate(_thumbprint);

                // Getting the timestamp from ERMIS.
                if (_tsaClient == null) GetTimestamp(_chain);

                // Perform the signing if file exists && there is a certificate
                ProcessSigning(fileToBeSigned,_destPath,_pk,_chain,_collection,_crlList,_ocspClient,_tsaClient);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception:" + Environment.NewLine + e.Message);
                log.ToFile($"Exception:{e.Message}");
            }
        }

        private void GetCertificate(string _thumbprint)
        {
            X509Store x509Store = new X509Store(StoreName.My,StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.ReadOnly | OpenFlags.IncludeArchived);

            _collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint,_thumbprint,false);

            // If there is at least one certificate
            if (_collection.Count > 0)
            {
                X509Certificate2Enumerator certificatesEn = _collection.GetEnumerator();
                certificatesEn.MoveNext();
                _pk = certificatesEn.Current;
                X509Chain x509chain = new X509Chain();
                x509chain.Build(_pk);

                foreach (X509ChainElement x509ChainElement in x509chain.ChainElements)
                {
                    _chain.Add(DotNetUtilities.FromX509Certificate(x509ChainElement.Certificate));
                }
            }
            else
            {
                var log = new Logger(_logfile);
                log.ToFile("No certificate found for signing!");
                Console.WriteLine("Please check log file!");
            }
           
            x509Store.Close();
        }

        private void GetTimestamp(IList<X509Certificate> _chain)
        {
            for (int i = 0; i < _chain.Count; i++)
            {
                _cert = _chain[i];
                string tsaUrl = Folders.ermisLink;
                if (tsaUrl != null) _tsaClient = new TSAClientBouncyCastle(tsaUrl);
            }

            _crlList = new List<ICrlClient>
                {
                new CrlClientOnline(_chain)
                };
        }

        private void ProcessSigning(string fileToBeSigned,
                                    string _destPath,
                                    X509Certificate2 _pk,
                                    IList<X509Certificate> _chain,
                                    X509Certificate2Collection _collection,
                                    IList<ICrlClient> _crlList,
                                    IOcspClient _ocspClient,
                                    ITSAClient _tsaClient)
        {
            if (OkToSign(fileToBeSigned))
            {
                // Set the signed file's full path+filename
                _signedFile = (_destPath + Path.GetFileNameWithoutExtension(fileToBeSigned) + "_signed.pdf");
#if DEBUG
                _password = null;
                Sign(fileToBeSigned,_signedFile,_chain,_pk,DigestAlgorithms.SHA1,CryptoStandard.CMS,
                                        "development",null,_crlList,_ocspClient,_tsaClient,0, _password);
#else
                // Get signing reason and password as input from the user
                GetDetails();

                Sign(fileToBeSigned,_signedFile,_chain,_pk,DigestAlgorithms.SHA1,CryptoStandard.CMS,
                                        _myReason,null,_crlList,_ocspClient,_tsaClient,0, _password);
#endif
                // Logging the operation to txt file
                var log = new Logger(_logfile);
                log.ToFile("Signed " + "'" + Path.GetFileNameWithoutExtension(fileToBeSigned) + "'" + " using " + 
                    ((_pk.FriendlyName == "") ? _pk.Subject : "'" + _pk.FriendlyName + "'"));

                Console.WriteLine("Signed file created!");
            }
            else
            {
                _signedFile = string.Empty;
            }
        }

        private void Sign(string src,string dest,
                         ICollection<X509Certificate> _chain,X509Certificate2 _pk,
                         string digestAlgorithm,CryptoStandard subfilter,
                         string reason,string location,
                         ICollection<ICrlClient> _crlList,
                         IOcspClient _ocspClient,
                         ITSAClient _tsaClient,
                         int estimatedSize,
                         string _password)
        {
            // Creating the reader and the stamper
            PdfReader reader = null;
            PdfStamper stamper = null;
            FileStream fs = null;
            try
            {
                reader = new PdfReader(src);
                fs = new FileStream(dest,FileMode.Create);
                stamper = PdfStamper.CreateSignature(reader,fs,'\0');

                // Creating the appearance
                PdfSignatureAppearance appearance = stamper.SignatureAppearance;
                appearance.Reason = reason;
                appearance.SetVisibleSignature(new Rectangle(400,40,480,70),1,"signature");

                // If password is not null, then set encryption
                if (!string.IsNullOrEmpty(_password))
                {
                    byte[] USER = Encoding.ASCII.GetBytes(_password);
                    byte[] OWNER = Encoding.ASCII.GetBytes(_password);

                    stamper.SetEncryption(USER, OWNER, PdfWriter.AllowPrinting, PdfWriter.ENCRYPTION_AES_128);
                }

                // Creating the signature
                IExternalSignature pks = new X509Certificate2Signature(_pk,digestAlgorithm);
                MakeSignature.SignDetached(appearance,pks,_chain,_crlList,_ocspClient,_tsaClient,estimatedSize,subfilter);
            }
            finally
            {
                if (reader != null) reader.Close();
                if (stamper != null) stamper.Close();
                if (fs != null) fs.Close();
            }
        }

        public void ProtectWithPassword(string file, string password)
        {
            var source = file;
            var dest = Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + "_pwd.pdf";

            using (Stream input = new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read))
            using (Stream output = new FileStream(dest,FileMode.Create,FileAccess.Write,FileShare.None))
            {
                PdfReader reader = new PdfReader(input);
                PdfEncryptor.Encrypt(reader,output,true,password,password,PdfWriter.AllowCopy);
            }
        }

        private bool OkToSign(string fileToBeSigned)
        {
            return (!string.IsNullOrWhiteSpace(fileToBeSigned) && 
                    File.Exists(fileToBeSigned) && _collection.Count > 0);
        }

        private void GetDetails()
        {
            Console.Write("Enter sign reason:");
            _myReason = Console.ReadLine();

            Console.Write("Enter password for encryption:");
            _password = Console.ReadLine();
        }
    }
}
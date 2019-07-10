using System;

namespace LetsEncrypt.Client
{
    public class Configuration
    {
        public Boolean IsStaging { get; set; }
        public String AccountEmail { get; set; }
        public String AccountPem { get; set; }
        public String DomainName { get; set; }
        public String Country { get; set; }
        public String State { get; set; }
        public String Locality { get; set; }
        public String Organization { get; set; }
        public String Unit { get; set; }
        public String CertificatePassword { get; set; }
        public Int32 Delay { get; set; }
        public String ChallengePath { get; set; }
        public String CertificatePath { get; set; }
    }
}
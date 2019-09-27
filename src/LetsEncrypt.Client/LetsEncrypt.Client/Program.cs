using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using static System.Console;

namespace LetsEncrypt.Client
{
    class Program
    {
        private static Configuration _configuration = new Configuration();

        static async Task Main(string[] args)
        {
            ShowTitle();
            LoadConfiguration();

            while (true)
            {
                try
                {
                    (bool isAboutToExpire, string notAfter) = CertificateIsAboutToExpire();

                    if (isAboutToExpire)
                    {
                        var acme = await LoadAccount();
                        var order = await CreateOrder(acme);
                        await ValidateOrder(order);
                        await GenerateOrder(order);
                    }
                    else
                    {
                        Log($" V. Current certificate is still valid :) [{notAfter}]");
                    }
                }
                catch (Exception ex)
                {
                    Log($" X. ERROR {ex}");
                }

                await Task.Delay(_configuration.Delay);
            }
        }

        private static (bool, string) CertificateIsAboutToExpire()
        {
            var certPath = Path.Combine(_configuration.CertificatePath, $"{_configuration.DomainName}.pfx");
            var certPass = _configuration.CertificatePassword;

            if (!File.Exists(certPath))
            {
                return (true, string.Empty);
            }

            bool isAboutToExpire = false;
            string notAfter = string.Empty;

            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(certPath, certPass, X509KeyStorageFlags.PersistKeySet);
            foreach (var cert in collection)
            {
                isAboutToExpire = isAboutToExpire || cert.NotAfter < DateTime.Today.AddDays(7);
                notAfter = $"{cert.NotAfter:dd-MM-yyyy}";
            }

            return (isAboutToExpire, notAfter);
        }

        private static async Task GenerateOrder(IOrderContext order)
        {
            Log(" 7. Generating Certificate...");
            var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);

            var cert = await order.Generate(new CsrInfo
            {
                CountryName = _configuration.Country,
                State = _configuration.State,
                Locality = _configuration.Locality,
                Organization = _configuration.Organization,
                OrganizationUnit = _configuration.Unit,
                CommonName = _configuration.DomainName,
            }, privateKey);

            Log(" 8. Building PFX...");
            var pfxBuilder = cert.ToPfx(privateKey);
            var pfx = pfxBuilder.Build(_configuration.DomainName, _configuration.CertificatePassword);
            File.WriteAllBytes(Path.Combine(_configuration.CertificatePath, $"{_configuration.DomainName}.pfx"), pfx);
        }

        private static async Task ValidateOrder(IOrderContext order)
        {
            Log($" 4. Validating domain {_configuration.DomainName}...");
            var authz = (await order.Authorizations()).First();
            var httpChallenge = await authz.Http();
            var keyAuthz = httpChallenge.KeyAuthz;

            Log(" 5. Writing challenge file");
            var tokens = keyAuthz.Split('.');
            await File.WriteAllTextAsync(Path.Combine(_configuration.ChallengePath, tokens[0]), keyAuthz);

            var chall = await httpChallenge.Validate();

            while (chall.Status == ChallengeStatus.Pending)
            {
                await Task.Delay(10000);
                chall = await httpChallenge.Validate();
            }

            if (chall.Status == ChallengeStatus.Valid)
            {
                Log($" 6. Domain {_configuration.DomainName} is valid!");
            }

            if (chall.Status == ChallengeStatus.Invalid)
            {
                Log($" 6. Domain {_configuration.DomainName} is NOT valid! {chall.Error.Detail}");
            }
        }

        private static async Task<IOrderContext> CreateOrder(AcmeContext acme)
        {
            Log($" 3. Creating order { _configuration.DomainName}...");
            return await acme.NewOrder(new[] { _configuration.DomainName });
        }

        private static async Task<AcmeContext> LoadAccount()
        {
            AcmeContext acme;

            var server = _configuration.IsStaging ? WellKnownServers.LetsEncryptStagingV2 : WellKnownServers.LetsEncryptV2;
            Log($" 1. Setting Environment {server}...");

            if (String.IsNullOrEmpty(_configuration.AccountPem))
            {
                Log(" 2. Creating account...");
                acme = new AcmeContext(server);
                var account = await acme.NewAccount(_configuration.AccountEmail, true);
                _configuration.AccountPem = acme.AccountKey.ToPem();
            }
            else
            {
                Log(" 2. Using existing account...");
                var accountKey = KeyFactory.FromPem(_configuration.AccountPem);
                acme = new AcmeContext(server, accountKey);
            }

            return acme;
        }

        private static void ShowTitle()
        {
            Log("Let's Encrypt Client is starting...");
            WriteLine();
        }

        private static void LoadConfiguration()
        {
            Log(" 0. Loading environment variables...");

            var staging = Environment.GetEnvironmentVariable("STAGING");
            var accountName = Environment.GetEnvironmentVariable("ACCOUNT_EMAIL");
            var accountKey = Environment.GetEnvironmentVariable("ACCOUNT_KEY");
            var domain = Environment.GetEnvironmentVariable("DOMAIN");
            var country = Environment.GetEnvironmentVariable("CERT_COUNTRY");
            var state = Environment.GetEnvironmentVariable("CERT_STATE");
            var locality = Environment.GetEnvironmentVariable("CERT_LOCALITY");
            var organization = Environment.GetEnvironmentVariable("CERT_ORGANISATION");
            var organizationUnit = Environment.GetEnvironmentVariable("CERT_ORGANISATION_UNIT");
            var certificatePassword = Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD");
            var certificatePath = Environment.GetEnvironmentVariable("CERTIFICATE_PATH");
            var challengePath = Environment.GetEnvironmentVariable("CHALLENGE_PATH");
            var delay = Environment.GetEnvironmentVariable("DELAY");

            _configuration.IsStaging = staging == "YES";
            _configuration.AccountEmail = accountName;
            _configuration.AccountPem = accountKey;
            _configuration.DomainName = domain;
            _configuration.Country = country;
            _configuration.State = state;
            _configuration.Locality = locality;
            _configuration.Organization = organization;
            _configuration.Unit = organizationUnit;
            _configuration.Delay = Convert.ToInt32(delay);
            _configuration.CertificatePassword = certificatePassword;
            _configuration.CertificatePath = certificatePath;
            _configuration.ChallengePath = challengePath;
        }

        private static void Log(String message)
        {
            WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm}] {message}");
        }
    }
}
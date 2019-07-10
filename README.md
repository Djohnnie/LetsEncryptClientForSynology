# LetsEncryptClientForSynology
A custom client for Let's Encrypt, running on my Synology NAS


So - I know this is an old post, but I have figured out how to fix the problem.

Synology put the alias into the .conf files of the various web server types to point .well-known to /var/lib/letsencrypt/.well-known

This is to enable the gui download of letsencrypt certificates to work.

Simple fix (not sure if will survive an upgrade)

You will need to ssh into the nas.,,,,

Create .well-known in the web root (/volume1/web)
mkdir /volume1/web/.well-known
Create acme-challenge under .well-known
mkdir /volume1/web/.well-known/acme-challenge

Remove the file under letencrypt
sudo rm /var/lib/letsencrypt/.well-known

create a link

sudo ln -s /volume1/web/.well-known /var/lib/letsencrypt/.well-known

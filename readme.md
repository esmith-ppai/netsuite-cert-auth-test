# Integrating C# .NET Applications with NetSuite using OAuth 2.0 (RSA-PSS)

Support for RSA PKCSv1.5 is no longer supported in NetSuite as of March 1st, 2025.

https://docs.extendtech.net/general/netsuite-end-of-rsa-pkcsv1-5-scheme-support

This example project demonstrates how a private key and certificate using the RSA-PSS scheme can be used to authenticate with NetSuite.

1. Copy the `.env.example` file to `.env`

2. Generate a new certificate and private key pair. On Windows, you can use Git Bash, WSL, Cygwin, etc. to run and generate the following openssl command. 

```bash
openssl req -nodes -newkey rsa:3072 -keyout private-key.ppai.netsuite-20250123.pem -out certificate.ppai.netsuite-20250123.pem -days 365 -x509 -sigopt rsa_padding_mode:pss -subj "/C=US/ST=Texas/L=Irving/O=PPAI/OU=IT Department/CN=org-ppai-netsuite"
```

This will produce two files. A certificate (certificate.ppai.netsuite-20250123.pem) and a private key (private-key.ppai.netsuite-20250123.pem).

Copy the contents of the private key to this project's NETSUITE_PRIVATE_KEY_PEM environment variable in your newly created .env.

3.  In NetSuite, create a new Integration (Setup &raquo; Integration &raquo; New) Be sure to **uncheck/disable** TBA: Authorization flow and Token-based Authentication. Enable "Client credentials (machine to machine) Grant". You may also want to enable Restlets and Rest Web Services. This is well demonstrated in [this video](https://www.youtube.com/watch?v=Ug2ZtI8wCDg).  

    
Once you are issued a client id and secret, copy those values as `NETSUITE_API_CONSUMER_KEY` and `NETSUITE_API_CONSUMER_SECRET` to your environment (.env) file. These values will only be displayed once in NetSuite, so be sure to copy them to a secure location.

4. Create a new OAuth 2.0 Client Credentials Setup, uploading the **certificate** to your OAuth 2.0 Client Credentials Mapping record in NetSuite. This is also demonstrated in the linked video above.

Once the certificate is created, and you have a "Certificate Id" for this integration, copy this value to your `NETSUITE_CLIENT_CREDENTIALS_CERTIFICATE_ID` environment (.env) variable.

5. Now that you have NetSuite set up with an Integration that is connected to an OAuth 2.0 Credential and all of the values in your .env filled in, you should be able to run this Console Application and see an access token returned from NetSuite. This token can then be used on subsequent requests. Keep in mind that this token expires and needs to be renewed.

Example output:

```powershell
PS C:\Users\Evan\RiderProjects\NetSuiteCertAuthTest> dotnet run
Account ID: 1234567-sb1
JWT Token: egJhbGciOiJQUzI1NiIsImtpZCI6ImJNNTQ5aXMxN1dOTE1uemhjdDBhd05MZXFsSzZqWlpjVzBmbEZubkdvbkUiLCJ0eXAiOiJKV1QifQ.eyJzY29wZSI6WyJyZXN0X3dlIcNlcnZpY2VzIl0sIm5iZiI6MTczNzc1OTI2MiwiZXhwIjoxNzM3NzU5NTYyLCJpYXQiOjE3Mzc3NTkyNjIsImlzcyI6ImU0YzJjNGNmYmU5NjZkOTBmZWQzZDAwNTA4ZmQ4MmJiNDA1OTQ1YmEyM2NhYWZhZWQ1ZTI4N2UwMDBhMmE4MGYiLCJhdWQiOiJodHRwczovLzc1NTc5MDctc2IxLnN1aXRldGFsay5hcGkubmV0c3VpdGUuY29tL3NlcnZpY2K9L3Jlc3QvYXV0aC9vYXV0aDIvdjEvdG9rZW4ifQ.nE3k3Kz0Q51lGkm3CWnZ3t6zG5LHXVFf4bJipgzvB0TFrwuAkegS92vedwWAxE4-bYqGzLMWh7fdCW9LEv-1FcAr6N9B1cDem04uDpBfnis6PSbVe1m-x811mZQm-nX-kmGzSlFtIKf3wRgiUKryKhyIZDXQUTg-__8jLWRT5iDbWIvE8LMHuGIQNz70jIGpMOr3q9zCAYtsjdozFs6jvRNUfvsbsX0-RsQTVJn6yF5P3mzjLINasVPg8-PimvbigVOGhpy7y1iKuzeeHsMAsOztdAffaEy13YKTO-nlMoim1uDpC8bXdzUbDJko7N5g-wUq761Q3zk4PALUHKsPoYGHQnFiLxiKYY0si-sbQrvuhZ6VVhrbtCepDiyRbYt1BosoFOYjG8-UEaW1LeylwPxnds9lt--YeIL4WqSA3cyJNVnPslYRPfAL6Xy4v0j34ZgqGl_iildsNC5hTqbZKM4-XGYrNnRxzRYneFs3sjt78sutJLOX77cpDodLdJyH
Found 3 out of 3 customers
```

IMPORTANT REMINDERS: 
 - You created a certificate that expires, and you will need to renew it before its expiration to prevent your integration connection from failing.
 - The token you created has an expiration. Ensure your code handles a missing or expired token. 
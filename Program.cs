using System.Text.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DotNetEnv;
using Microsoft.IdentityModel.Tokens;

namespace NetSuiteCertAuthTest
{
    public class Program
    {
        private static string AccountId;
        private static string ClientCredentialsCertificateId;
        private static string ApiConsumerKey;

        /*
         * Use RSA with PSS (PKCS v1.5 deprecated by NetSuite on 2025/03/01). Check Readme
         */
        private static string PrivateKeyPem;
        
        // NetSuite Api Endpoints
        private static string RestApiRoot => $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest";
        private static string Oauth2ApiRoot => $"{RestApiRoot}/auth/oauth2/v1";
        private static string RecordApiRoot => $"{RestApiRoot}/record/v1";
        private static string TokenEndPointUrl => $"{Oauth2ApiRoot}/token";
        
        // Http Client
        private static readonly HttpClient _httpClient = new HttpClient();
        
        // NetSuite short-lived access token
        private static string? _accessToken; // Store access token provided by NetSuite after auth (time limit: 5min)

        public static async Task Main(string[] args)
        {
            // Load .env variables 
            Env.NoClobber().Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
            AccountId = Env.GetString("NETSUITE_ACCOUNT_ID");
            ClientCredentialsCertificateId = Env.GetString("NETSUITE_CLIENT_CREDENTIALS_CERTIFICATE_ID");
            ApiConsumerKey = Env.GetString("NETSUITE_API_CONSUMER_KEY");
            PrivateKeyPem = Env.GetString("NETSUITE_PRIVATE_KEY_PEM");
            
            // Display Account Id
            Console.WriteLine($"Account ID: {AccountId}");
            
            // Display Access Token returned from NetSuite
            string token = await GetAccessToken();
            Console.WriteLine($"Initialize Application with Access Token: {token}");

            // Example API call. List customers found in NetSuite (limit of 3)
            List<string> customerIds = await FindCustomerIds(3);
            if (customerIds.Any())
            {
                Console.WriteLine($"Found {customerIds.Count} out of 3 customers");
            }
            else
            {
                Console.WriteLine("No customers found");
            }

        }

        private static async Task<string> GetAccessToken()
        {
            var url = Oauth2ApiRoot + "/token/";

            string assertion = GetJwtToken();

            var requestParams = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials"),
                new("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                new("client_assertion", assertion)
            };
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = new FormUrlEncodedContent(requestParams);

            var httpResponse = await _httpClient.SendAsync(httpRequest);
            var responseJson = await httpResponse.Content.ReadAsStringAsync();

            var response = JsonSerializer.Deserialize<NsToken>(responseJson);
            var responseCode = (int)httpResponse.StatusCode;
            // Console.WriteLine($"{responseCode} - {responseJson}");
            if (response?.access_token == null)
            {
                throw new InvalidOperationException("Failed to retrieve access token.");
            }
            _accessToken = response.access_token;
            return _accessToken;

        }

        private static string GetJwtToken()
        {
            string privateKeyPem = RemoveCommentedLines(PrivateKeyPem);

            // Create the RSA key
            using var rsa = RSA.Create();
            byte[] privateKeyRaw = Convert.FromBase64String(privateKeyPem);
            rsa.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(privateKeyRaw), out _);
            RsaSecurityKey rsaSecurityKey = new RsaSecurityKey(rsa);
            
            // Sign with RSA PS256 algorithm
            var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSsaPssSha256)
            {
                Key = {KeyId = ClientCredentialsCertificateId}
            };

            // Get issuing timestamp.
            var now = DateTime.UtcNow;

            // Create token.
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = ApiConsumerKey,     // Note: this it not the secret, it is the key/id.
                Audience = TokenEndPointUrl,
                Expires = now.AddMinutes(5),
                IssuedAt = now,
                Claims = new Dictionary<string, object>
                {
                    { "scope", new[] { "rest_webservices" } }
                },
                SigningCredentials = signingCredentials
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenText = tokenHandler.WriteToken(token);
            Console.WriteLine($"JWT Token: {tokenText}");

            return tokenText;
        }

        // Test call to NetSuite after auth
        public static async Task<List<string>> FindCustomerIds(int limit)
        {
            var url = RecordApiRoot + "/customer?limit=" + limit;
            
            _accessToken ??= await GetAccessToken();

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var httpResponse = await _httpClient.SendAsync(httpRequest);
            var responseJson = await httpResponse.Content.ReadAsStringAsync();

            var response = JsonSerializer.Deserialize<NsFindIdsResponse>(responseJson);

            return response.items.Select(i => i.id).ToList();
        }

        // Strip private key (.pem) file of commented lines beginning with "--"
        private static string RemoveCommentedLines(string keyfile)
        {
            StringBuilder result = new StringBuilder();
            using (StringReader reader = new StringReader(keyfile))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.TrimStart().StartsWith("--"))
                    {
                        result.AppendLine(line);
                    }
                }
            }

            return result.ToString();
        }


    }
}
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace iProov.APIClient
{
    public enum ClaimType
    {
        Verify,
        Enrol
    }

    public enum PhotoSource
    {
        EID,
        OID,
        Selfie
    }

    public enum AssuranceType
    {
        GenuinePresence,
        Liveness
    }

    public enum ErrorType
    {
        InvalidImage,
        NoToken,
        InvalidJSON,
        ServerError
    }

    public class Error: Exception
    {
        public ErrorType Type { get; }
        public string? ServerErrorMessage { get; }

        public Error(ErrorType type, string? serverErrorMessage = null)
        {
            Type = type;
            ServerErrorMessage = serverErrorMessage;
        }

        public override string Message
        {
            get
            {
                switch (Type)
                {
                    case ErrorType.InvalidImage:
                        return "Invalid image";
                    case ErrorType.NoToken:
                        return "No token";
                    case ErrorType.InvalidJSON:
                        return "Invalid JSON";
                    case ErrorType.ServerError:
                        return ServerErrorMessage ?? "unknown";
                    default:
                        return base.Message;
                }
            }
        }
    }

    static class EnumExtensions
    {

        public static string toInternalString(this ClaimType claimType)
        {
            switch (claimType)
            {
                case ClaimType.Verify:
                    return "verify";

                case ClaimType.Enrol:
                    return "enrol";

                default:
                    throw new NotImplementedException();
            }
        }

        public static string toInternalString(this PhotoSource photoSource)
        {
            switch (photoSource)
            {
                case PhotoSource.EID:
                    return "eid";

                case PhotoSource.OID:
                    return "oid";

                case PhotoSource.Selfie:
                    return "selfie";

                default:
                    throw new NotImplementedException();
            }
        }


        public static string toInternalString(this AssuranceType assuranceType)
        {
            switch (assuranceType)
            {
                case AssuranceType.GenuinePresence:
                    return "genuine_presence";

                case AssuranceType.Liveness:
                    return "liveness";

                default:
                    throw new NotImplementedException();
            }
        }
    }

    public class ApiClient
    {
        private readonly string baseURL;
        private readonly string apiKey;
        private readonly string secret;
        private readonly string appID;

        private readonly HttpClient httpClient = new HttpClient();

        public ApiClient(string baseURL, string apiKey, string secret, string appID)
        {
            this.baseURL = baseURL;
            this.apiKey = apiKey;
            this.secret = secret;
            this.appID = appID;

            // User-Agent must always be sent, and Xamarin.Android doesn't send a
            // user agent for some reason
            httpClient.DefaultRequestHeaders.Add("User-Agent", ".NET Standard Library");
        }

        public async Task<string> GetToken(AssuranceType assuranceType, ClaimType type, string userID)
        {

            var request = new Dictionary<string, string>
            {
                { "assurance_type", assuranceType.toInternalString() },
                { "api_key", apiKey },
                { "secret", secret },
                { "resource", appID },
                { "client", "dotnet" },
                { "user_id", userID }
            };

            var json = JsonConvert.SerializeObject(request);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{baseURL}/claim/{type.toInternalString()}/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

            if (responseDict == null)
            {
                throw new Error(type: ErrorType.InvalidJSON);
            }

            if (!response.IsSuccessStatusCode)
            {
                if (responseDict.TryGetValue("error_description", out var serverErrorMessageObj))
                {
                    string serverErrorMessage = (string)serverErrorMessageObj;
                    throw new Error(type: ErrorType.ServerError, serverErrorMessage: serverErrorMessage);
                }

                int statusCodeAsInt = (int)response.StatusCode;
                throw new Error(ErrorType.ServerError, serverErrorMessage: $"Unexpected status code: {statusCodeAsInt}");
            }

            if (responseDict.TryGetValue("token", out var tokenObj))
            {
                string token = (string)tokenObj;
                return token;
            }

            throw new Error(type: ErrorType.NoToken);
        }

        public async Task<bool> EnrolPhoto(string token, byte[] jpegImage, PhotoSource source)
        {
            ByteArrayContent? fileContent = new ByteArrayContent(jpegImage);
            if (fileContent == null)
            {
                throw new Error(type: ErrorType.InvalidImage);
            }

            var multipartFormData = new MultipartFormDataContent();
            multipartFormData.Add(new StringContent(apiKey), "api_key");
            multipartFormData.Add(new StringContent(secret), "secret");
            multipartFormData.Add(new StringContent("0"), "rotation");
            multipartFormData.Add(new StringContent(token), "token");
            multipartFormData.Add(fileContent, "image", "image.jpg");
            multipartFormData.Add(new StringContent(source.ToString()), "source");

            var response = await httpClient.PostAsync($"{baseURL}/claim/enrol/image", multipartFormData);
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);

            if (responseDict == null)
            {
                throw new Error(type: ErrorType.InvalidJSON);
            }

            return responseDict.ContainsKey("success") && bool.Parse(responseDict["success"]);
        }

        // TODO: Turn into a proper ValidationResult
        public async Task<Dictionary<string, object>> Validate(string token, string userID)
        {
            var request = new Dictionary<string, string>
            {
                { "api_key", apiKey },
                { "secret", secret },
                { "user_id", userID },
                { "token", token },
                { "ip", "127.0.0.1" },
                { "client", appID }
            };

            var json = JsonConvert.SerializeObject(request);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{baseURL}/claim/verify/validate", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

            if (responseDict == null)
            {
                throw new Error(type: ErrorType.InvalidJSON);
            }

            if (!response.IsSuccessStatusCode)
            {
                if (responseDict.TryGetValue("error_description", out var serverErrorMessageObj))
                {
                    string serverErrorMessage = (string)serverErrorMessageObj;
                    throw new Error(type: ErrorType.ServerError, serverErrorMessage: serverErrorMessage);
                }

                int statusCodeAsInt = (int)response.StatusCode;
                throw new Error(ErrorType.ServerError, serverErrorMessage: $"Unexpected status code: {statusCodeAsInt}");
            }

            return responseDict;
        }

        public async Task<string> EnrolPhotoAndGetVerifyToken(string userID, byte[] jpegImage, PhotoSource source)
        {
            var enrolToken = await GetToken(AssuranceType.GenuinePresence, ClaimType.Enrol, userID);
            await EnrolPhoto(enrolToken, jpegImage, source);
            return await GetToken(AssuranceType.GenuinePresence, ClaimType.Verify, userID);
        }

        public async Task<string> GetOAuthToken(string username, string password)
        {
            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ':' + password));
            var authType = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "scope", "user-write" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseURL}/{apiKey}/access_token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            request.Content = new FormUrlEncodedContent(authType);

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);

            if (responseDict == null)
            {
                throw new Error(type: ErrorType.InvalidJSON);
            }

            return responseDict["access_token"];
        }
    }
}

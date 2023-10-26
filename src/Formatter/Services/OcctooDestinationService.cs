using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using outfit_international.Dtos.Queries;
using outfit_international.Exceptions;
using outfit_international.Responses;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace outfit_international.Services
{
    public class OcctooDestinationService
    {
        #region Variables and constants
        private readonly string _occtooUrlBase = "";
        private readonly string _occtooUrlTokenBase = "";
        private readonly string _occtooClientId = "";
        private readonly string _occtooClientSecret = "";

        private string _occtooAuthorizationToken = "";
        private DateTime _expiresOn = DateTime.Now;

        protected static readonly int MAX_TRYOUTS = 3;

        private static readonly object mSyncLock = new();
        #endregion

        #region Constructor
        protected OcctooDestinationService(string occtooUrlBase, string occtooUrlTokenBase,
            string occtooClientId, string occtooClientSecret)
        {
            _occtooUrlBase = occtooUrlBase;
            _occtooUrlTokenBase = occtooUrlTokenBase;
            _occtooClientId = occtooClientId;
            _occtooClientSecret = occtooClientSecret;
        }
        #endregion

        #region GetObjectsAsync, PostObjectsAsync
        protected async Task<IEnumerable<T>> GetObjectsAsync<T>(string endpoint, string queryPart, string resultPropertyName = "results")
        {
            var tryOut = 1;
            var postObjects = await GetObjectsInternalAsync<T>(endpoint, queryPart, resultPropertyName);
            while (postObjects == null)
            {
                postObjects = await GetObjectsInternalAsync<T>(endpoint, queryPart, resultPropertyName);
                tryOut++;
                if (postObjects == null && tryOut >= MAX_TRYOUTS)
                    break;
            }
            return postObjects ?? throw new OcctooServerError();
        }

        private async Task<IEnumerable<T>> GetObjectsInternalAsync<T>(string endpoint, string queryPart, string resultPropertyName = "results")
        {
            try
            {
                if (!GetOcctooAuthorizationToken())
                    return null;

                var hierarchy = new List<T>();

                var cancellationTokenSource = new CancellationTokenSource(new TimeSpan(1, 0, 0));

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_occtooAuthorizationToken}");
                var response = await httpClient.GetAsync($"{_occtooUrlBase}/{endpoint}?{queryPart}", cancellationTokenSource.Token);
                var jsonStr = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(jsonStr);
                var array = (JArray)result.Property(resultPropertyName)?.Value;
                if (array != null)
                    hierarchy = JsonConvert.DeserializeObject<List<T>>(array.ToString());

                return hierarchy;
            }
            catch
            {
                return null;
            }
        }

        protected async Task<IEnumerable<T>> PostObjectsAsync<T>(string endpoint, OcctooDestinationRequestDto filter, string resultPropertyName = "results")
        {
            var tryOut = 1;
            var postObjects = await PostObjectsInternalAsync<T>(endpoint, filter, resultPropertyName);
            while (postObjects == null)
            {
                postObjects = await PostObjectsInternalAsync<T>(endpoint, filter, resultPropertyName);
                tryOut++;
                if (postObjects == null && tryOut >= MAX_TRYOUTS)
                    break;
            }
            return postObjects ?? throw new OcctooServerError();
        }

        private async Task<IEnumerable<T>> PostObjectsInternalAsync<T>(string endpoint, OcctooDestinationRequestDto filter, string resultPropertyName = "results")
        {
            try
            {
                if (!GetOcctooAuthorizationToken())
                    return null;

                var hierarchy = new List<T>();

                var cancellationTokenSource = new CancellationTokenSource(new TimeSpan(1, 0, 0));

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_occtooAuthorizationToken}");
                var content = new StringContent(JsonConvert.SerializeObject(filter), System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{_occtooUrlBase}/{endpoint}", content, cancellationTokenSource.Token);

                var jsonStr = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(jsonStr);
                var array = (JArray)result.Property(resultPropertyName)?.Value;

                if (array != null)
                    hierarchy = JsonConvert.DeserializeObject<List<T>>((array.ToString()));

                return hierarchy;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region Authorization
        protected bool GetOcctooAuthorizationToken()
        {
            var tryOut = 1;
            var authorized = GetOcctooAuthorizationTokenInternal();
            while (!authorized)
            {
                authorized = GetOcctooAuthorizationTokenInternal(true);
                tryOut++;
                if (!authorized && tryOut >= MAX_TRYOUTS)
                {
                    authorized = false;
                    break;
                }
            }

            return authorized ? authorized : throw new OcctooServerError();
        }

        private bool GetOcctooAuthorizationTokenInternal(bool forceAuthorization = false)
        {
            try
            {
                if (!forceAuthorization && !string.IsNullOrEmpty(_occtooAuthorizationToken) && _expiresOn > DateTime.Now)
                    return true;

                lock (mSyncLock)
                {
                    var cancellationTokenSource = new CancellationTokenSource(new TimeSpan(1, 0, 0));

                    var httpClient = new HttpClient();
                    var request = new OcctooQueryAuthorizationToken() { clientId = _occtooClientId, clientSecret = _occtooClientSecret };
                    var body = JsonConvert.SerializeObject(request);
                    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    var response = httpClient.PostAsync($"{_occtooUrlTokenBase}", content, cancellationTokenSource.Token).Result;
                    var jsonStr = response.Content.ReadAsStringAsync().Result;
                    var tokenResponse = JsonConvert.DeserializeObject<OcctooResponseAuthorizationToken>(jsonStr);

                    _occtooAuthorizationToken = tokenResponse.accessToken;
                    _expiresOn = DateTime.Now.AddSeconds(int.Parse(tokenResponse.expiresIn) - 60);

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace outfit_international.Helpers
{
    /// <summary>  
    /// Azure variable provides Azure threads synchronization and object sharing between threads and applications through Azure server.
    /// </summary>  
    public class AzureVariable<T>
    {
        #region Variables
        private static BlobContainerClient _blobNamespace = null;

        private BlobClient _blobVariableFile;
        private string _leaseId;

        private readonly string _resName;
        private readonly string _blobName;
        private readonly string _connection;
        #endregion

        #region Constructors
        /// <summary>
        /// Create instance of <typeparamref name="AzureVariable"/>.
        /// </summary>
        /// <param name="connection">Connection string (AzureWebJobsStorage) that contains Azure storage account credentials.</param>
        /// <param name="appName">Name space (container) used as a place holder for all Azure variables used in this application.</param>
        /// <param name="variableName">Variable name used as a part of file name on Azure server.</param>
        /// <exception cref="ArgumentNullException">Exception is thrown when <cref name="connection"/> or <cref name="appName"/> are null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Exception is thrown when <cref name="variableName"/> contains invalid file name char.</exception>
        public AzureVariable(string connection, string appName, string variableName = "")
        {
            if (string.IsNullOrEmpty(connection))
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(appName))
                throw new ArgumentNullException(nameof(appName));

            _blobVariableFile = null;
            _leaseId = string.Empty;
            _resName = $"{appName.ToLower()}-variables";
            _connection = connection;
            variableName = variableName.Trim();
            if (variableName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentOutOfRangeException(nameof(variableName));
            if (string.IsNullOrEmpty(variableName))
                variableName = "default";
            _blobName = $"{variableName}-({typeof(T).Name}).json";
        }

        /// <summary>
        /// Create instance of <typeparamref name="AzureVariable"/>. Use it only if you have set AzureWebJobsStorage and ApplicationName within <c>local.settings.json</c> and <c>Configuration</c> on Azure Function App server Settings.
        /// </summary>
        /// <param name="variableName">Variable name used as a part of file name on Azure server.</param>
        /// <exception cref="ArgumentNullException">Exception is thrown when <cref name="connection"/> or <cref name="appName"/> are null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Exception is thrown when <cref name="variableName"/> contains invalid file name char.</exception>
        public AzureVariable(string variableName = "") : this(Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                Environment.GetEnvironmentVariable("ApplicationName"),
                variableName)
        {
        }

        ~AzureVariable() => Unlock();
        #endregion

        #region PrepareNamespace
        /// <summary>
        /// Prepare azure variable name space.
        /// </summary>
        /// <param name="connection">Connection string (AzureWebJobsStorage) that contains Azure storage account credentials.</param>
        /// <param name="appName">Name space (container) used as a place holder for Azure variable json file.</param>
        public static async Task PrepareNamespaceAsync(string connection, string appName)
        {
            if (string.IsNullOrEmpty(connection))
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(appName))
                throw new ArgumentNullException(nameof(appName));

            var resName = $"{appName}-variables";
            var blobServiceClient = new BlobServiceClient(connection);
            var blobContainer = blobServiceClient.GetBlobContainerClient(resName);
            if (await blobContainer.ExistsAsync())
            {
                await blobContainer.DeleteAsync();
                while (await blobContainer.ExistsAsync())
                    await Task.Delay(25);
            }
        }

        /// <summary>
        /// Prepare azure variable name space. Use it only if you have set AzureWebJobsStorage and ApplicationName within <c>local.settings.json</c> and <c>Configuration</c> on Azure Function App server Settings.
        /// </summary>
        public static async Task PrepareNamespaceAsync() =>
            await PrepareNamespaceAsync(Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                Environment.GetEnvironmentVariable("ApplicationName"));
        #endregion

        #region Helpers
        private async Task<bool> AssertBlobNamespaceAndVariableFile()
        {
            try
            {
                if (_blobNamespace == null)
                {
                    var blobServiceClient = new BlobServiceClient(_connection);
                    _blobNamespace = blobServiceClient.GetBlobContainerClient(_resName);
                    if (!await _blobNamespace.ExistsAsync())
                    {
                        _blobNamespace = await blobServiceClient.CreateBlobContainerAsync(_resName);
                        while (!await _blobNamespace.ExistsAsync())
                            await Task.Delay(25);
                    }
                }
                if (_blobVariableFile == null)
                {
                    _blobVariableFile = _blobNamespace.GetBlobClient(_blobName);
                    if (!await _blobVariableFile.ExistsAsync())
                    {
                        await _blobVariableFile.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(string.Empty)));
                        while (!await _blobVariableFile.ExistsAsync())
                            await Task.Delay(25);
                    }
                }
                return true;
            }
            catch
            {
                _blobNamespace = null;
                _blobVariableFile = null;
                return false;
            }
        }

        private async Task<string> TryToLockAsync()
        {
            if (!await AssertBlobNamespaceAndVariableFile())
                return string.Empty;

            _leaseId = string.Empty;
            try
            {
                var leaseClient = _blobVariableFile.GetBlobLeaseClient();
                var lease = await leaseClient.AcquireAsync(BlobLeaseClient.InfiniteLeaseDuration);
                _leaseId = lease.Value.LeaseId;

                return _leaseId;
            }
            catch
            {
                Unlock();
                return _leaseId;
            }
        }

        private async Task<T> WaitOnLockInternalAsync(CancellationToken? cancelationToken = null, TimeSpan? timeout = null)
        {
            if (!string.IsNullOrEmpty(_leaseId))
                return await LoadAsync();

            var breakTime = timeout != null ? ((TimeSpan)timeout).TotalMilliseconds : 0;
            while (true)
            {
                if (cancelationToken != null && ((CancellationToken)cancelationToken).IsCancellationRequested)
                    throw new OperationCanceledException();
                try
                {
                    var lockId = await TryToLockAsync();
                    if (string.IsNullOrEmpty(lockId))
                    {
                        var nextMSecInterval = new Random(Guid.NewGuid().GetHashCode()).Next(250, 1000);
                        if (timeout != null)
                        {
                            breakTime -= nextMSecInterval;
                            if (breakTime <= 0)
                                throw new TimeoutException();
                        }
                        await Task.Delay(TimeSpan.FromMilliseconds(nextMSecInterval));
                    }
                    else
                    {
                        var objInstance = await LoadAsync();
                        if (objInstance != null)
                            return objInstance;
                    }
                }
                catch (TimeoutException)
                {
                    throw;
                }
                catch { }
            }
        }

        private async Task<bool> SaveToJsonAsync(T value)
        {
            if (!await AssertBlobNamespaceAndVariableFile())
                return false;

            try
            {
                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                var jsonStr = JsonConvert.SerializeObject(value);
                writer.WriteLine(jsonStr);
                writer.Flush();
                stream.Position = 0;
                await _blobVariableFile.UploadAsync(stream, new BlobUploadOptions() { Conditions = new BlobRequestConditions() { LeaseId = _leaseId } });
                writer.Close();
                stream.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<T> LoadFromJsonAsync()
        {
            if (!await AssertBlobNamespaceAndVariableFile())
                return default;

            try
            {
                using var stream = new MemoryStream();
                await _blobVariableFile.DownloadToAsync(stream);
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                var jsonStr = reader.ReadLine();
                reader.Close();
                stream.Close();
                if (jsonStr == null)
                    return (T)Activator.CreateInstance(typeof(T), Array.Empty<object>());
                return JsonConvert.DeserializeObject<T>(jsonStr);
            }
            catch
            {
                return default;
            }
        }
        #endregion

        #region Load, Save and SaveAndUnlock
        /// <summary>
        /// Load properties for <typeparamref name="T"/> object from Azure server.
        /// </summary>
        /// <returns>
        /// Default instance of type <typeparamref name="T"/> or instance filled with properties already saved on Azure server
        /// </returns>
        public async Task<T> LoadAsync() => await LoadFromJsonAsync();

        /// <summary>
        /// Save properties of <typeparamref name="T"/> object to Azure server.
        /// </summary>
        /// <param name="value"><typeparamref name="T"/> object whose properties should be saved on Azure server.</param>
        /// <returns>
        /// <c>true</c> if the <typeparamref name="T"/> object can be saved; otherwise, <c>false</c>.
        /// </returns>        
        public async Task<bool> SaveAsync(T value) => await SaveToJsonAsync(value);

        /// <summary>
        /// Save properties of <typeparamref name="T"/> object to Azure server and unlock/release variable.
        /// </summary>
        /// <param name="value"><typeparamref name="T"/> object whose properties should be saved on Azure server.</param>
        /// <returns>
        /// <c>true</c> if the <typeparamref name="T"/> object can be saved; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> SaveAndUnlockAsync(T value)
        {
            try
            {
                return await SaveToJsonAsync(value);
            }
            finally
            {
                Unlock();
            }
        }
        #endregion

        #region TryToLockAndLoad, WaitOnLockAsync, IsLocked and Unlock
        /// <summary>
        /// Try to establish exclusive access on object <typeparamref name="T"/> and load it's properties. 
        /// </summary>
        /// <returns>
        /// If exclusive access can be set default instance of type <typeparamref name="T"/> or instance filled with properties already saved on Azure server, or null otherwise.
        /// </returns>
        public async Task<T> TryToLockAndLoadAsync()
        {
            if (string.IsNullOrEmpty(_leaseId))
            {
                var lockId = await TryToLockAsync();
                if (string.IsNullOrEmpty(lockId))
                    return default;
            }

            return await LoadAsync();
        }

        /// <summary>
        /// Indefinite wait for object to be free so exclusive access can be established.
        /// </summary>
        /// <returns>
        /// Default instance of type <typeparamref name="T"/> or instance filled with properties already saved on Azure server.
        /// </returns>
        public Task<T> WaitOnLockAsync() =>
            WaitOnLockInternalAsync();

        /// <summary>
        /// Indefinite wait for object to be free so exclusive access can be established. Cancelation token can be used to cancel wait operation.
        /// </summary>
        /// <param name="cancelationToken">Cancelation token that can be used to cancel the wait operation.</param>
        /// <returns>
        /// Default instance of type <typeparamref name="T"/> or instance filled with properties already saved on Azure server.
        /// </returns>
        /// <exception cref="OperationCanceledException">Exception is thrown when token cancelation is requested.</exception>
        public Task<T> WaitOnLockAsync(CancellationToken cancelationToken) =>
            //WaitOnLockInternalAsync((cancelationToken != CancellationToken.None) ? cancelationToken : null);
            WaitOnLockInternalAsync(cancelationToken);

        /// <summary>
        /// Indefinite wait for object to be free so exclusive access can be established. 
        /// Upon timeout expiration wait operation will be canceled and exception <typeparamref name="TimeoutException"/> will be thrown.
        /// </summary>
        /// <returns>
        /// Default instance of type <typeparamref name="T"/> or instance filled with properties already saved on Azure server.
        /// </returns>
        /// <param name="timeoutSec">Timeout in seconds after .</param>
        /// <exception cref="TimeoutException">Exception is thrown when timeout exceeded.</exception>
        public Task<T> WaitOnLockAsync(TimeSpan timeout) =>
            WaitOnLockInternalAsync(null, timeout);

        /// <summary>
        /// Indefinite wait for object to be free so exclusive access can be established. <br/> Cancelation token can be used to cancel wait operation. 
        /// Upon timeout expiration wait operation will be canceled and exception <typeparamref name="TimeoutException"/> will be thrown.
        /// </summary>
        /// <returns>
        /// Default instance of type <typeparamref name="T"/> or instance filled with properties already saved on Azure server.
        /// </returns>
        /// <param name="cancelationToken">Cancelation token that can be used to cancel the wait operation.</param>
        /// <param name="timeoutSec">Timeout in seconds after .</param>
        /// <exception cref="TimeoutException">Exception is thrown when timeout exceeded.</exception> 
        /// <exception cref="OperationCanceledException">Exception is thrown when token cancelation is requested.</exception>
        public Task<T> WaitOnLockAsync(CancellationToken cancelationToken, TimeSpan timeout) =>
            WaitOnLockInternalAsync(cancelationToken, timeout);

        /// <summary>
        /// Check if lock is established on Azure variable.
        /// </summary>
        public bool IsLocked => !string.IsNullOrEmpty(_leaseId);

        /// <summary>
        /// Unlock/realase <typeparamref name="T"/> object.
        /// </summary>
        public void Unlock()
        {
            try
            {
                if (!string.IsNullOrEmpty(_leaseId))
                {
                    var blobLease = _blobVariableFile.GetBlobLeaseClient(_leaseId);
                    blobLease.ReleaseAsync().Wait();
                }
            }
            catch { }
            finally
            {
                _leaseId = string.Empty;
            }
        }

        /// <summary>
        /// Unlock/realase <typeparamref name="T"/> object.
        /// </summary>
        /// <param name="removeOnUnlock">If true azure blob will be removed after unlocking.</param>
        /// <returns>
        public async Task UnlockAsync(bool removeOnUnlock = false)
        {
            try
            {
                if (!string.IsNullOrEmpty(_leaseId))
                {
                    var blobLease = _blobVariableFile.GetBlobLeaseClient(_leaseId);
                    await blobLease.ReleaseAsync();

                    if (removeOnUnlock && await _blobVariableFile.ExistsAsync())
                    {
                        await _blobVariableFile.DeleteAsync();
                        while (await _blobVariableFile.ExistsAsync())
                            await Task.Delay(25);
                    }
                }
            }
            catch { }
            finally
            {
                _leaseId = string.Empty;
            }
        }
        #endregion
    }
}
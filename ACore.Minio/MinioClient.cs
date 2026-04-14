using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Storage.Files;
using AUtils.IoC;
using Minio;

namespace ACore.Minio;

[Log(Category = "Minio")]
internal class MinioClient : IInitializable, IDisposable, IStorageFiles
{
    private readonly IContainer mContainer;
    private global::Minio.MinioClient mClient;
    private readonly ILogger<MinioClient> mLogger;

    public MinioClient(ILogger<MinioClient> logger, IContainer container)
    {
        mLogger = logger;
        mContainer = container;
    }
    
    public void Initialize()
    {
       var configuration = mContainer.Resolve<IConfiguration>()
           .Get(() => MinioConfiguration.Default);
       var env = mContainer.Resolve<ICellEnvironment>();
        mLogger.Debug($"Connecting to MinIO: {configuration.Endpoint}");
        try
        {
            mClient = new global::Minio.MinioClient()
                .WithEndpoint(configuration.Endpoint)
                .WithTimeout(configuration.Timeout)
                .WithCredentials(configuration.AccessKey, configuration.SecretKey)
                .WithSSL()
                .Build();
            
            mClient.SetAppInfo(env.Role, env.Build);
            mLogger.Success("Connection succeed");
        }
        catch (Exception e)
        {
            mLogger.Error($"Fail to connect to MinIO", e);
        }
    }

    public async Task Upload(string path, string fileName, Stream content)
    {
        if (path == null) 
            throw new ArgumentNullException(nameof(path));
        
        if (fileName == null) 
            throw new ArgumentNullException(nameof(fileName));
        
        if (content == null) 
            throw new ArgumentNullException(nameof(content));
        
        if(mClient == null)
        {
            mLogger.Debug($"[Disabled] Uploading file {path}.{fileName}");
            return;
        }

        try
        {
            await CreateBucketIfNotExists(path);

            var args = new PutObjectArgs()
                .WithStreamData(content)
                .WithFileName(fileName)
                .WithBucket(path);

            await mClient.PutObjectAsync(args).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<bool> Exists(string path)
    {
        if (path == null) 
            throw new ArgumentNullException(nameof(path));
        
        if (mClient == null)
            return false;

        return await mClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(path));
    }

    public async Task<bool> Exists(string path, string fileName)
    {
        if (path == null) 
            throw new ArgumentNullException(nameof(path));
        
        if (fileName == null) 
            throw new ArgumentNullException(nameof(fileName));

        if (mClient == null)
            return false;

        var objects = mClient.ListObjectsAsync(new ListObjectsArgs().WithBucket(path));
        return await objects.Any(x => x.Key == fileName).ToTask();
    }

    public async Task Delete(string path)
    {
        if (path == null) 
            throw new ArgumentNullException(nameof(path));
        
        if(mClient == null)
        {
            mLogger.Debug($"[Disabled] Deleting file {path}");
            return;
        }
        
        await mClient.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(path));
    }

    public async Task Delete(string path, string fileName)
    {
        if (path == null) 
            throw new ArgumentNullException(nameof(path));
        
        if (fileName == null) 
            throw new ArgumentNullException(nameof(fileName));
        
        if(mClient == null)
        {
            mLogger.Debug($"[Disabled] Deleting file {path}.{fileName}");
            return;
        }
        
        await mClient.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(path).WithObject(fileName));
    }

    private async Task CreateBucketIfNotExists(string bucketName)
    {
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        if (await mClient.BucketExistsAsync(beArgs).ConfigureAwait(false))
            return;

        var mbArgs = new MakeBucketArgs()
            .WithBucket(bucketName);
        await mClient.MakeBucketAsync(mbArgs).ConfigureAwait(false);
    }
    
    public void Dispose()
    {
        mClient?.Dispose();
    }
    
    #region Utils

    [Configuration("minio")]
    private class MinioConfiguration
    {
        public string Endpoint { get; set; }
        
        public string AccessKey { get; set; }
        
        public string SecretKey { get; set; }
        
        public int Timeout { get; set; }

        public static MinioConfiguration Default => new()
        {
            Endpoint = "",
            AccessKey = "",
            SecretKey = "",
            Timeout = 2000
        };
    }

    #endregion
}
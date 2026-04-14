using ACore.Abstractions;
using ACore.Abstractions.Worker;
using ACore.Patching;

namespace Fb.Seed;

[Worker("seeder")]
public class SeedWorker : IRunnable
{
    private readonly IPatchService mPatchService;

    public SeedWorker(IPatchService patchService)
    {
        mPatchService = patchService;
    }

    public Task Run(CancellationToken token = default)
    {
        return mPatchService.Migrate(PatchCategories.World, token);
    }
}
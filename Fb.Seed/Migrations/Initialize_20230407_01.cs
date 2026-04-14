using System.ComponentModel;
using ACore.Patching;

namespace Fb.Seed.Migrations;

[Description("Test patch 1")]
public class Initialize_20230407_01 : Patch
{
    public override string Order => "20230407_01";
    public override string Category => PatchCategories.World;
    public override Task Up()
    {
        throw new NotImplementedException();
    }

    public override Task Down()
    {
        throw new NotImplementedException();
    }
}
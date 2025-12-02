using UnityEditor;

public static class BuildServer
{
    public static void Build()
    {
        string path = "/home/akshay/venv/wingman-game/build/WingmanServer.x86_64";

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/Sample.unity"
            },
            locationPathName = path,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.EnableHeadlessMode // Dedicate server mode
        };

        BuildPipeline.BuildPlayer(opts);
    }
}

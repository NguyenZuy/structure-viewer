namespace StructureViewer.Editor.Setup
{
    // Where the setup tool writes generated assets; tests point it at a temporary folder.
    public sealed class SetupPaths
    {
        public const string WoodColor = "Assets/_Project/Art/Textures/Wood096_Color.jpg";
        public const string WoodNormal = "Assets/_Project/Art/Textures/Wood096_Normal.jpg";
        public const string ConcreteColor = "Assets/_Project/Art/Textures/Concrete034_Color.jpg";
        public const string ConcreteNormal = "Assets/_Project/Art/Textures/Concrete034_Normal.jpg";
        public const string Theme = "Assets/_Project/UI/RuntimeTheme.tss";
        public const string MainScene = "Assets/_Project/Scenes/Main.unity";

        public static readonly SetupPaths Project = new SetupPaths(
            "Assets/_Project/Materials",
            "Assets/_Project/Art/Textures/Grid.png",
            "Assets/_Project/Data/RenderingConfig.asset",
            "Assets/_Project/UI/PanelSettings.asset");

        public SetupPaths(string materialsFolder, string gridTexture, string renderingConfig, string panelSettings)
        {
            MaterialsFolder = materialsFolder;
            GridTexture = gridTexture;
            RenderingConfig = renderingConfig;
            PanelSettings = panelSettings;
        }

        public string MaterialsFolder { get; }
        public string GridTexture { get; }
        public string RenderingConfig { get; }
        public string PanelSettings { get; }

        public string Material(string name) => $"{MaterialsFolder}/{name}.mat";
    }
}

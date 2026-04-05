using UnityEditor.AssetImporters;

/// <summary>
/// Custom importer for .bub files, which are JSON dialogue files used by Bubble Spinner.
/// </summary>

[ScriptedImporter(1, "bub")]
public class BubFileImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var text = System.IO.File.ReadAllText(ctx.assetPath);
        var asset = new UnityEngine.TextAsset(text);
        ctx.AddObjectToAsset("text", asset);
        ctx.SetMainObject(asset);
    }
}
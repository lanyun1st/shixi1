using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SentryAssetInstaller
{
    static SentryAssetInstaller(){EditorApplication.delayCall+=Ensure;}
    public static void Ensure()
    {
        const string path="Assets/Resources/SentrySettings.asset";
        var settings=AssetDatabase.LoadAssetAtPath<SentrySettings>(path);
        if(settings==null)
        {
            Directory.CreateDirectory("Assets/Resources");settings=ScriptableObject.CreateInstance<SentrySettings>();
            AssetDatabase.CreateAsset(settings,path);AssetDatabase.SaveAssets();
        }
        settings.Validate();
    }
}

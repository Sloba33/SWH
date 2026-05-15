using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TextureOptimizer : EditorWindow
{
    private Vector2 scrollPosition;
    private bool optimizeUI = false;
    private bool optimizeCharacters = true;
    private bool optimizeEnvironment = true;
    private int targetMaxSize = 1024;
    private int targetAndroidMaxSize = 512;
    private int targetIOSMaxSize = 1024;
    
    [MenuItem("Tools/Texture Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<TextureOptimizer>("Texture Optimizer");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Texture Optimization Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Backup Section
        GUILayout.Label("1. Backup Current Settings", EditorStyles.boldLabel);
        if (GUILayout.Button("Backup All Texture Settings"))
        {
            BackupTextureSettings();
        }
        if (GUILayout.Button("Restore From Backup"))
        {
            RestoreTextureSettings();
        }
        GUILayout.Space(10);
        
        // Optimization Settings
        GUILayout.Label("2. Optimization Settings", EditorStyles.boldLabel);
        optimizeUI = EditorGUILayout.Toggle("Optimize UI Textures", optimizeUI);
        optimizeCharacters = EditorGUILayout.Toggle("Optimize Character Textures", optimizeCharacters);
        optimizeEnvironment = EditorGUILayout.Toggle("Optimize Environment Textures", optimizeEnvironment);
        
        GUILayout.Space(5);
        targetMaxSize = EditorGUILayout.IntSlider("Desktop Max Size", targetMaxSize, 256, 4096);
        targetAndroidMaxSize = EditorGUILayout.IntSlider("Android Max Size", targetAndroidMaxSize, 256, 4096);
        targetIOSMaxSize = EditorGUILayout.IntSlider("iOS Max Size", targetIOSMaxSize, 256, 4096);
        
        GUILayout.Space(10);
        
        // Optimization Section
        GUILayout.Label("3. Optimize Textures", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This will modify texture import settings. Make sure you've backed up first!", MessageType.Warning);
        
        if (GUILayout.Button("Optimize Textures (Safe Mode - Preview Only)"))
        {
            PreviewTextureOptimization();
        }
        
        if (GUILayout.Button("APPLY OPTIMIZATION", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Confirm Optimization", 
                "This will permanently change texture import settings. Have you created a backup?", 
                "Yes, Apply", "Cancel"))
            {
                OptimizeTextures();
            }
        }
        
        GUILayout.Space(10);
        
        // Info Section
        GUILayout.Label("4. Information", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        
        EditorGUILayout.HelpBox(
            "Backup File Location: Assets/TextureSettingsBackup.json\n\n" +
            "To revert: Click 'Restore From Backup' button above\n\n" +
            "Texture Guidelines:\n" +
            "- 4096: 16 MB (Too big for mobile)\n" +
            "- 2048: 8 MB (Use only for hero images)\n" +
            "- 1024: 2 MB (Good for detailed textures)\n" +
            "- 512: 0.5 MB (Good for environment)\n" +
            "- 256: 0.125 MB (Good for small props)\n\n" +
            "ASTC Compression: Best quality/size ratio for mobile\n" +
            "DXT/ETC2: Good for desktop",
            MessageType.Info);
        
        EditorGUILayout.EndScrollView();
        
        GUILayout.Space(10);
        
        // Report Section
        if (GUILayout.Button("Generate Texture Report"))
        {
            GenerateTextureReport();
        }
    }
    
    [System.Serializable]
    public class TextureSettings
    {
        public string path;
        public int maxSize;
        public int compressionQuality;
        public string textureCompression;
        public string androidFormat;
        public int androidMaxSize;
        public string iosFormat;
        public int iosMaxSize;
    }
    
    [System.Serializable]
    public class TextureSettingsBackup
    {
        public List<TextureSettings> settings;
        public string backupDate;
        public int totalTextures;
    }
    
    private void BackupTextureSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        var backup = new TextureSettingsBackup();
        backup.settings = new List<TextureSettings>();
        backup.backupDate = System.DateTime.Now.ToString();
        backup.totalTextures = guids.Length;
        
        int processed = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null)
            {
                var settings = new TextureSettings();
                settings.path = path;
                settings.maxSize = importer.maxTextureSize;
                settings.compressionQuality = importer.compressionQuality;
                settings.textureCompression = importer.textureCompression.ToString();
                
                // Get Android settings
                var androidSettings = importer.GetPlatformTextureSettings("Android");
                if (androidSettings != null && androidSettings.overridden)
                {
                    settings.androidFormat = androidSettings.format.ToString();
                    settings.androidMaxSize = androidSettings.maxTextureSize;
                }
                
                // Get iOS settings
                var iosSettings = importer.GetPlatformTextureSettings("iPhone");
                if (iosSettings != null && iosSettings.overridden)
                {
                    settings.iosFormat = iosSettings.format.ToString();
                    settings.iosMaxSize = iosSettings.maxTextureSize;
                }
                
                backup.settings.Add(settings);
                processed++;
                
                if (processed % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("Backing Up Textures", $"Processing {processed}/{guids.Length}", processed / (float)guids.Length);
                }
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        string json = JsonUtility.ToJson(backup, true);
        File.WriteAllText(Application.dataPath + "/TextureSettingsBackup.json", json);
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ Backup completed! {processed} textures saved to Assets/TextureSettingsBackup.json");
        EditorUtility.DisplayDialog("Backup Complete", $"Saved settings for {processed} textures", "OK");
    }
    
    private void RestoreTextureSettings()
    {
        string backupPath = Application.dataPath + "/TextureSettingsBackup.json";
        if (!File.Exists(backupPath))
        {
            EditorUtility.DisplayDialog("Error", "No backup file found! Please create a backup first.", "OK");
            return;
        }
        
        string json = File.ReadAllText(backupPath);
        TextureSettingsBackup backup = JsonUtility.FromJson<TextureSettingsBackup>(json);
        
        if (backup == null || backup.settings == null)
        {
            Debug.LogError("Failed to parse backup file!");
            return;
        }
        
        int restored = 0;
        foreach (var settings in backup.settings)
        {
            TextureImporter importer = AssetImporter.GetAtPath(settings.path) as TextureImporter;
            if (importer != null)
            {
                importer.maxTextureSize = settings.maxSize;
                importer.compressionQuality = settings.compressionQuality;
                importer.textureCompression = (TextureImporterCompression)System.Enum.Parse(typeof(TextureImporterCompression), settings.textureCompression);
                
                // Restore Android settings if they existed
                if (settings.androidFormat != null)
                {
                    var androidSettings = importer.GetPlatformTextureSettings("Android");
                    androidSettings.overridden = true;
                    androidSettings.format = (TextureImporterFormat)System.Enum.Parse(typeof(TextureImporterFormat), settings.androidFormat);
                    androidSettings.maxTextureSize = settings.androidMaxSize;
                    importer.SetPlatformTextureSettings(androidSettings);
                }
                
                // Restore iOS settings if they existed
                if (settings.iosFormat != null)
                {
                    var iosSettings = importer.GetPlatformTextureSettings("iPhone");
                    iosSettings.overridden = true;
                    iosSettings.format = (TextureImporterFormat)System.Enum.Parse(typeof(TextureImporterFormat), settings.iosFormat);
                    iosSettings.maxTextureSize = settings.iosMaxSize;
                    importer.SetPlatformTextureSettings(iosSettings);
                }
                
                importer.SaveAndReimport();
                restored++;
                
                if (restored % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("Restoring Textures", $"Restoring {restored}/{backup.settings.Count}", restored / (float)backup.settings.Count);
                }
            }
        }
        
        EditorUtility.ClearProgressBar();
        Debug.Log($"✅ Restored {restored} textures from backup!");
        EditorUtility.DisplayDialog("Restore Complete", $"Restored {restored} textures to original settings.", "OK");
    }
    
    private void PreviewTextureOptimization()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        var report = new Dictionary<string, int>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null)
            {
                string category = GetTextureCategory(path);
                if (!ShouldOptimize(category)) continue;
                
                int currentSize = importer.maxTextureSize;
                int newSize = GetTargetSize(importer, category);
                
                if (currentSize > newSize)
                {
                    long fileSize = new FileInfo(path).Length;
                    long savedSize = (long)(fileSize * (1 - (newSize * newSize) / (float)(currentSize * currentSize)));
                    
                    if (!report.ContainsKey(category))
                        report[category] = 0;
                    report[category] += (int)savedSize;
                }
            }
        }
        
        Debug.Log("=== TEXTURE OPTIMIZATION PREVIEW ===\n");
        long totalSavings = 0;
        foreach (var kvp in report)
        {
            Debug.Log($"{kvp.Key}: Will save ~{FormatBytes(kvp.Value)}");
            totalSavings += kvp.Value;
        }
        Debug.Log($"\nTotal estimated savings: {FormatBytes(totalSavings)}");
        Debug.Log($"\nEstimated final texture size: {FormatBytes(GetCurrentTextureTotalSize() - totalSavings)}");
    }
    
    private void OptimizeTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        int optimized = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null)
            {
                string category = GetTextureCategory(path);
                if (!ShouldOptimize(category)) continue;
                
                // Set desktop settings
                int targetSize = GetTargetSize(importer, category);
                importer.maxTextureSize = targetSize;
                importer.compressionQuality = 50;
                importer.textureCompression = TextureImporterCompression.Compressed;
                
                // Set Android settings (most important for mobile)
                TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
                androidSettings.overridden = true;
                androidSettings.maxTextureSize = targetAndroidMaxSize;
                androidSettings.format = TextureImporterFormat.ASTC_6x6;
                androidSettings.compressionQuality = 50;
                importer.SetPlatformTextureSettings(androidSettings);
                
                // Set iOS settings
                TextureImporterPlatformSettings iosSettings = importer.GetPlatformTextureSettings("iPhone");
                iosSettings.overridden = true;
                iosSettings.maxTextureSize = targetIOSMaxSize;
                iosSettings.format = TextureImporterFormat.ASTC_6x6;
                iosSettings.compressionQuality = 50;
                importer.SetPlatformTextureSettings(iosSettings);
                
                importer.SaveAndReimport();
                optimized++;
                
                if (optimized % 20 == 0)
                {
                    EditorUtility.DisplayProgressBar("Optimizing Textures", $"Optimizing {optimized}/{guids.Length}", optimized / (float)guids.Length);
                }
            }
        }
        
        EditorUtility.ClearProgressBar();
        Debug.Log($"✅ Optimized {optimized} textures!");
        EditorUtility.DisplayDialog("Optimization Complete", $"Optimized {optimized} textures.\nBackup saved, you can restore if needed.", "OK");
    }
    
    private void GenerateTextureReport()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        var report = new List<string>();
        
        long totalSize = 0;
        report.Add("=== TEXTURE SIZE REPORT ===\n");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null)
            {
                long fileSize = new FileInfo(path).Length;
                totalSize += fileSize;
                
                if (fileSize > 1024 * 1024) // > 1MB
                {
                    report.Add($"{FormatBytes(fileSize)} - {path} (Size: {importer.maxTextureSize})");
                }
            }
        }
        
        report.Add($"\nTotal Texture Size: {FormatBytes(totalSize)}");
        report.Add($"Number of Textures: {guids.Length}");
        
        string reportPath = Application.dataPath + "/TextureSizeReport.txt";
        File.WriteAllLines(reportPath, report);
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ Report saved to {reportPath}");
        EditorUtility.DisplayDialog("Report Generated", $"Texture report saved to:\n{reportPath}", "OK");
    }
    
    private string GetTextureCategory(string path)
    {
        if (path.Contains("/UI/") || path.Contains("_UI/")) return "UI";
        if (path.Contains("/Characters/") || path.Contains("_Player/")) return "Characters";
        if (path.Contains("/Environment/") || path.Contains("_FBX/")) return "Environment";
        return "Other";
    }
    
    private bool ShouldOptimize(string category)
    {
        switch (category)
        {
            case "UI": return optimizeUI;
            case "Characters": return optimizeCharacters;
            case "Environment": return optimizeEnvironment;
            default: return true;
        }
    }
    
    private int GetTargetSize(TextureImporter importer, string category)
    {
        switch (category)
        {
            case "UI": return 1024;
            case "Characters": return 1024;
            case "Environment": return 512;
            default: return targetMaxSize;
        }
    }
    
    private long GetCurrentTextureTotalSize()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        long total = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            total += new FileInfo(path).Length;
        }
        
        return total;
    }
    
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TextureOptimizer : EditorWindow
{
    private Vector2 scrollPosition;
    private bool optimizeUI = true;
    private bool optimizeCharacters = true;
    private bool optimizeEnvironment = true;
    private int targetMaxSize = 512;
    private int targetAndroidMaxSize = 512;
    private int targetIOSMaxSize = 512;
    
    // Additional optimization options
    private bool useASTCCompression = true;
    private bool enableMipMaps = false;
    private int compressionQuality = 50;

    [MenuItem("Tools/Texture Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<TextureOptimizer>("Texture Optimizer");
    }
    
    [MenuItem("Tools/Find Largest Textures")]
    public static void FindLargestTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        var textures = new List<(string path, long size, TextureImporter importer)>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                long size = new FileInfo(path).Length;
                textures.Add((path, size, importer));
            }
        }
        
        // Sort by size descending
        textures.Sort((a, b) => b.size.CompareTo(a.size));
        
        Debug.Log("=== LARGEST TEXTURES ===\n");
        for (int i = 0; i < Mathf.Min(20, textures.Count); i++)
        {
            var tex = textures[i];
            Debug.Log($"{FormatBytesStatic(tex.size)} - {tex.path} (MaxSize: {tex.importer.maxTextureSize})");
        }
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
        targetMaxSize = EditorGUILayout.IntSlider("Desktop Max Size", targetMaxSize, 256, 2048);
        targetAndroidMaxSize = EditorGUILayout.IntSlider("Android Max Size", targetAndroidMaxSize, 256, 2048);
        targetIOSMaxSize = EditorGUILayout.IntSlider("iOS Max Size", targetIOSMaxSize, 256, 2048);
        
        GUILayout.Space(5);
        useASTCCompression = EditorGUILayout.Toggle("Use ASTC Compression (Mobile)", useASTCCompression);
        enableMipMaps = EditorGUILayout.Toggle("Enable Mip Maps (3D textures)", enableMipMaps);
        compressionQuality = EditorGUILayout.IntSlider("Compression Quality", compressionQuality, 0, 100);

        GUILayout.Space(10);

        // Aggressive Optimization Section
        EditorGUILayout.HelpBox("AGGRESSIVE OPTIMIZATION: Reduces textures to 256-512px", MessageType.Warning);
        if (GUILayout.Button("AGGRESSIVE OPTIMIZATION (512px Max)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Confirm Aggressive Optimization",
                "This will set ALL textures to max 512px (256px for small UI).\nThis will significantly reduce quality but drastically reduce APK size.\nHave you created a backup?",
                "Yes, Optimize Aggressively", "Cancel"))
            {
                OptimizeTexturesAggressively();
            }
        }

        GUILayout.Space(10);

        // Standard Optimization
        EditorGUILayout.HelpBox("This will modify texture import settings. Make sure you've backed up first!", MessageType.Warning);

        if (GUILayout.Button("Preview Optimization"))
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
            "Recommended Mobile Sizes:\n" +
            "- UI Icons: 128-256px\n" +
            "- Character textures: 512px\n" +
            "- Environment: 256-512px\n" +
            "- Backgrounds: 1024px\n\n" +
            "ASTC 6x6 gives best compression for mobile",
            MessageType.Info);

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        // Report Section
        if (GUILayout.Button("Generate Detailed Texture Report"))
        {
            GenerateDetailedTextureReport();
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
                else
                {
                    settings.androidFormat = "";
                    settings.androidMaxSize = 1024;
                }

                // Get iOS settings
                var iosSettings = importer.GetPlatformTextureSettings("iPhone");
                if (iosSettings != null && iosSettings.overridden)
                {
                    settings.iosFormat = iosSettings.format.ToString();
                    settings.iosMaxSize = iosSettings.maxTextureSize;
                }
                else
                {
                    settings.iosFormat = "";
                    settings.iosMaxSize = 1024;
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
        int errors = 0;

        foreach (var settings in backup.settings)
        {
            TextureImporter importer = AssetImporter.GetAtPath(settings.path) as TextureImporter;
            if (importer != null)
            {
                try
                {
                    // Restore basic settings
                    importer.maxTextureSize = settings.maxSize;
                    importer.compressionQuality = settings.compressionQuality;

                    // Safely restore texture compression
                    if (!string.IsNullOrEmpty(settings.textureCompression))
                    {
                        try
                        {
                            importer.textureCompression = (TextureImporterCompression)System.Enum.Parse(typeof(TextureImporterCompression), settings.textureCompression);
                        }
                        catch
                        {
                            Debug.LogWarning($"Failed to restore compression for {settings.path}, using default");
                            importer.textureCompression = TextureImporterCompression.Compressed;
                        }
                    }

                    // Restore Android settings if they existed
                    if (!string.IsNullOrEmpty(settings.androidFormat))
                    {
                        try
                        {
                            var androidSettings = importer.GetPlatformTextureSettings("Android");
                            androidSettings.overridden = true;

                            if (System.Enum.TryParse<TextureImporterFormat>(settings.androidFormat, true, out TextureImporterFormat format))
                            {
                                androidSettings.format = format;
                            }
                            else
                            {
                                androidSettings.format = TextureImporterFormat.ASTC_6x6;
                                Debug.LogWarning($"Unknown format {settings.androidFormat} for {settings.path}, using ASTC_6x6");
                            }

                            androidSettings.maxTextureSize = settings.androidMaxSize > 0 ? settings.androidMaxSize : 1024;
                            importer.SetPlatformTextureSettings(androidSettings);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Failed to restore Android settings for {settings.path}: {e.Message}");
                        }
                    }

                    // Restore iOS settings if they existed
                    if (!string.IsNullOrEmpty(settings.iosFormat))
                    {
                        try
                        {
                            var iosSettings = importer.GetPlatformTextureSettings("iPhone");
                            iosSettings.overridden = true;

                            if (System.Enum.TryParse<TextureImporterFormat>(settings.iosFormat, true, out TextureImporterFormat format))
                            {
                                iosSettings.format = format;
                            }
                            else
                            {
                                iosSettings.format = TextureImporterFormat.ASTC_6x6;
                                Debug.LogWarning($"Unknown format {settings.iosFormat} for {settings.path}, using ASTC_6x6");
                            }

                            iosSettings.maxTextureSize = settings.iosMaxSize > 0 ? settings.iosMaxSize : 1024;
                            importer.SetPlatformTextureSettings(iosSettings);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Failed to restore iOS settings for {settings.path}: {e.Message}");
                        }
                    }

                    importer.SaveAndReimport();
                    restored++;
                }
                catch (System.Exception e)
                {
                    errors++;
                    Debug.LogError($"Failed to restore {settings.path}: {e.Message}");
                }

                if (restored % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("Restoring Textures", $"Restoring {restored}/{backup.settings.Count}", restored / (float)backup.settings.Count);
                }
            }
        }

        EditorUtility.ClearProgressBar();

        if (errors > 0)
        {
            Debug.LogWarning($"⚠️ Restored {restored} textures with {errors} errors. Check Console for details.");
            EditorUtility.DisplayDialog("Restore Complete with Errors", $"Restored {restored} textures.\n{errors} textures had errors (check Console).", "OK");
        }
        else
        {
            Debug.Log($"✅ Successfully restored {restored} textures!");
            EditorUtility.DisplayDialog("Restore Complete", $"Successfully restored {restored} textures to original settings.", "OK");
        }
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
                importer.compressionQuality = compressionQuality;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.mipmapEnabled = enableMipMaps;

                // Set Android settings
                TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
                androidSettings.overridden = true;
                androidSettings.maxTextureSize = targetAndroidMaxSize;
                if (useASTCCompression)
                {
                    androidSettings.format = TextureImporterFormat.ASTC_6x6;
                }
                androidSettings.compressionQuality = compressionQuality;
                importer.SetPlatformTextureSettings(androidSettings);

                // Set iOS settings
                TextureImporterPlatformSettings iosSettings = importer.GetPlatformTextureSettings("iPhone");
                iosSettings.overridden = true;
                iosSettings.maxTextureSize = targetIOSMaxSize;
                if (useASTCCompression)
                {
                    iosSettings.format = TextureImporterFormat.ASTC_6x6;
                }
                iosSettings.compressionQuality = compressionQuality;
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

    private void OptimizeTexturesAggressively()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        int optimized = 0;
        int skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                string category = GetTextureCategory(path);
                if (!ShouldOptimize(category)) 
                {
                    skipped++;
                    continue;
                }

                // Determine aggressive target size
                int targetSize = 512; // Default
                if (category == "UI")
                {
                    targetSize = 256;
                }
                else if (category == "Characters")
                {
                    targetSize = 512;
                }
                else if (category == "Environment")
                {
                    targetSize = 256;
                }

                // Set desktop settings
                importer.maxTextureSize = targetSize;
                importer.compressionQuality = 50;
                importer.textureCompression = TextureImporterCompression.Compressed;
                
                // Disable mip maps for UI (saves memory)
                if (category == "UI")
                {
                    importer.mipmapEnabled = false;
                }

                // Set Android settings
                TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
                androidSettings.overridden = true;
                androidSettings.maxTextureSize = targetSize;
                androidSettings.format = TextureImporterFormat.ASTC_8x8; // More compression than 6x6
                androidSettings.compressionQuality = 50;
                importer.SetPlatformTextureSettings(androidSettings);

                // Set iOS settings
                TextureImporterPlatformSettings iosSettings = importer.GetPlatformTextureSettings("iPhone");
                iosSettings.overridden = true;
                iosSettings.maxTextureSize = targetSize;
                iosSettings.format = TextureImporterFormat.ASTC_8x8;
                iosSettings.compressionQuality = 50;
                importer.SetPlatformTextureSettings(iosSettings);

                importer.SaveAndReimport();
                optimized++;

                if (optimized % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("Aggressive Optimization", 
                        $"Optimizing {optimized}/{guids.Length} textures", optimized / (float)guids.Length);
                }
            }
        }

        EditorUtility.ClearProgressBar();
        
        long newTotalSize = GetCurrentTextureTotalSize();
        Debug.Log($"✅ Aggressively optimized {optimized} textures! (Skipped {skipped})");
        Debug.Log($"📊 New total texture size: {FormatBytes(newTotalSize)}");
        EditorUtility.DisplayDialog("Aggressive Optimization Complete", 
            $"Optimized {optimized} textures.\nTarget sizes: UI=256, Characters=512, Environment=256\n\nRun Generate Detailed Texture Report to see results.", "OK");
    }

    private void GenerateDetailedTextureReport()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        var largeTextures = new List<string>();
        var totalByCategory = new Dictionary<string, long>();
        long totalSize = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null)
            {
                long fileSize = new FileInfo(path).Length;
                string category = GetTextureCategory(path);
                totalSize += fileSize;
                
                if (!totalByCategory.ContainsKey(category))
                    totalByCategory[category] = 0;
                totalByCategory[category] += fileSize;
                
                // Flag textures that are too large
                if (fileSize > 1024 * 1024) // > 1MB
                {
                    largeTextures.Add($"{FormatBytes(fileSize)} - {path} (Size: {importer.maxTextureSize})");
                }
            }
        }

        // Create report
        var report = new List<string>();
        report.Add("=== DETAILED TEXTURE REPORT ===\n");
        report.Add($"TOTAL TEXTURE SIZE: {FormatBytes(totalSize)}");
        report.Add($"TOTAL TEXTURES: {guids.Length}\n");
        
        report.Add("--- SIZE BY CATEGORY ---");
        foreach (var kvp in totalByCategory.OrderByDescending(x => x.Value))
        {
            float percentage = (kvp.Value / (float)totalSize) * 100f;
            report.Add($"{kvp.Key}: {FormatBytes(kvp.Value)} ({percentage:F1}%)");
        }
        
        report.Add("\n--- LARGE TEXTURES (>1MB) ---");
        report.AddRange(largeTextures);
        
        if (largeTextures.Count == 0)
            report.Add("No textures larger than 1MB found!");
        
        report.Add("\n--- OPTIMIZATION RECOMMENDATIONS ---");
        report.Add("1. Set UI textures to 256-512px max");
        report.Add("2. Use ASTC compression for all mobile textures");
        report.Add("3. Disable Read/Write on textures when possible");
        report.Add("4. Consider using Texture Atlases for UI elements");
        
        string reportPath = Application.dataPath + "/DetailedTextureReport.txt";
        File.WriteAllLines(reportPath, report);
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ Detailed report saved to {reportPath}");
        EditorUtility.DisplayDialog("Report Generated", 
            $"Total Texture Size: {FormatBytes(totalSize)}\n\nLargest category: {totalByCategory.OrderByDescending(x => x.Value).First().Key}\n\nFull report saved to:\n{reportPath}", "OK");
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
        // Now respects the targetMaxSize slider value
        switch (category)
        {
            case "UI": return targetMaxSize;
            case "Characters": return targetMaxSize;
            case "Environment": return targetMaxSize;
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

    private static string FormatBytesStatic(long bytes)
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;

namespace MyFirstProject.Extensions
{
    /// <summary>
    /// Manages configuration settings for corridor form data
    /// </summary>
    public static class CorridorFormConfigurationManager
    {
        /// <summary>
        /// Represents saved form configuration data
        /// </summary>
        public class SavedCorridorFormConfiguration
        {
            public string CorridorName { get; set; } = "";
            public string TargetAlignment1Name { get; set; } = "";
            public string TargetAlignment2Name { get; set; } = "";
            public string AssemblyName { get; set; } = "";
            public int AlignmentCount { get; set; } = 2;
            public DateTime SavedDate { get; set; } = DateTime.Now;
            public string ConfigurationName { get; set; } = "";
        }

        private static SavedCorridorFormConfiguration? _lastConfiguration;
        private static readonly string ConfigFileName = "CorridorFormConfig.json";

        /// <summary>
        /// Gets the configuration file path in the user's temp directory
        /// </summary>
        private static string GetConfigFilePath()
        {
            string tempPath = Path.GetTempPath();
            return Path.Combine(tempPath, "MyFirstProject", ConfigFileName);
        }

        /// <summary>
        /// Saves the current form configuration to file
        /// </summary>
        public static void SaveConfiguration(string corridorName, string target1Name, string target2Name, 
            string assemblyName, int alignmentCount, string configName = "Default")
        {
            try
            {
                var config = new SavedCorridorFormConfiguration
                {
                    CorridorName = corridorName ?? "",
                    TargetAlignment1Name = target1Name ?? "",
                    TargetAlignment2Name = target2Name ?? "",
                    AssemblyName = assemblyName ?? "",
                    AlignmentCount = alignmentCount,
                    ConfigurationName = configName,
                    SavedDate = DateTime.Now
                };

                // Ensure directory exists
                string filePath = GetConfigFilePath();
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Save to JSON file
                string jsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(filePath, jsonString);

                // Cache in memory
                _lastConfiguration = config;

                A.Ed.WriteMessage($"\n✓ Đã lưu cấu hình form: {configName}");
            }
            catch (Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi lưu cấu hình form: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the last saved form configuration from file
        /// </summary>
        public static SavedCorridorFormConfiguration? LoadConfiguration()
        {
            try
            {
                // Return cached configuration if available
                if (_lastConfiguration != null)
                {
                    return _lastConfiguration;
                }

                string filePath = GetConfigFilePath();
                if (!File.Exists(filePath))
                {
                    A.Ed.WriteMessage("\nKhông tìm thấy cấu hình form đã lưu.");
                    return null;
                }

                string jsonString = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<SavedCorridorFormConfiguration>(jsonString);
                
                // Cache for future use
                _lastConfiguration = config;
                
                if (config != null)
                {
                    A.Ed.WriteMessage($"\n✓ Đã tải cấu hình form: {config.ConfigurationName} (Lưu lúc: {config.SavedDate:dd/MM/yyyy HH:mm})");
                }

                return config;
            }
            catch (Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi tải cấu hình form: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if a saved configuration exists
        /// </summary>
        public static bool HasSavedConfiguration()
        {
            try
            {
                // Check memory cache first
                if (_lastConfiguration != null)
                {
                    return true;
                }

                // Check file system
                string filePath = GetConfigFilePath();
                return File.Exists(filePath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Clears the cached configuration (forces reload from file)
        /// </summary>
        public static void ClearCache()
        {
            _lastConfiguration = null;
        }

        /// <summary>
        /// Deletes the saved configuration file
        /// </summary>
        public static void DeleteSavedConfiguration()
        {
            try
            {
                string filePath = GetConfigFilePath();
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _lastConfiguration = null;
                    A.Ed.WriteMessage("\n✓ Đã xóa cấu hình form đã lưu.");
                }
                else
                {
                    A.Ed.WriteMessage("\nKhông có cấu hình form nào để xóa.");
                }
            }
            catch (Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi xóa cấu hình form: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds an object by name in the current drawing
        /// </summary>
        public static ObjectId FindObjectByName<T>(string objectName) where T : Autodesk.AutoCAD.DatabaseServices.DBObject
        {
            if (string.IsNullOrEmpty(objectName) || objectName == "(Chưa chọn)")
            {
                return ObjectId.Null;
            }

            try
            {
                using (Transaction tr = A.Db.TransactionManager.StartTransaction())
                {
                    // For corridors
                    if (typeof(T) == typeof(Corridor))
                    {
                        var corridors = A.Cdoc.CorridorCollection;
                        foreach (ObjectId corridorId in corridors)
                        {
                            Corridor? corridor = tr.GetObject(corridorId, OpenMode.ForRead) as Corridor;
                            if (corridor != null && corridor.Name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                            {
                                tr.Commit();
                                return corridorId;
                            }
                        }
                    }
                    // For alignments
                    else if (typeof(T) == typeof(Alignment))
                    {
                        var alignmentIds = A.Cdoc.GetAlignmentIds();
                        foreach (ObjectId alignmentId in alignmentIds)
                        {
                            Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                            if (alignment != null && alignment.Name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                            {
                                tr.Commit();
                                return alignmentId;
                            }
                        }
                    }
                    // For assemblies
                    else if (typeof(T) == typeof(Assembly))
                    {
                        var assemblies = A.Cdoc.AssemblyCollection;
                        foreach (ObjectId assemblyId in assemblies)
                        {
                            Assembly? assembly = tr.GetObject(assemblyId, OpenMode.ForRead) as Assembly;
                            if (assembly != null && assembly.Name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                            {
                                tr.Commit();
                                return assemblyId;
                            }
                        }
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi tìm kiếm đối tượng '{objectName}': {ex.Message}");
            }

            return ObjectId.Null;
        }
    }
}

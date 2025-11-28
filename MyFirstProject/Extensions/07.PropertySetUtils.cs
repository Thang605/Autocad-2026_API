using Autodesk.Aec.PropertyData.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using MyFirstProject.Extensions;

namespace MyFirstProject.Extensions
{
    public class PropertySetUtils
    {
        /// <summary>
        /// Thiết lập Property Set cho 3D Solid với các thuộc tính được tính toán
        /// </summary>
        /// <param name="tr">Transaction</param>
        /// <param name="solid">3D Solid object</param>
        public static void SetupSolidWithCalculatedProperties(Transaction tr, Solid3d solid)
        {
            if (solid == null) return;

            try
            {
                // Lấy thông tin cơ bản của solid
                string layerName = solid.Layer;
                double volume = solid.MassProperties.Volume;
                Point3d centroid = solid.MassProperties.Centroid;

                // Sử dụng tên Property Set cố định
                string propertySetName = "IFC ĐƯỜNG GIAO THÔNG2";

                // Kiểm tra và tạo Property Set Definition nếu chưa có
                ObjectId propertySetDefId = GetOrCreatePropertySetDefinition(tr, propertySetName);

                if (propertySetDefId.IsNull) return;

                // Attach Property Set vào solid
                AttachPropertySetToObject(tr, solid, propertySetDefId);

                // Set các giá trị properties
                SetPropertyValues(tr, solid, propertySetDefId, new Dictionary<string, object>
                {
                    { "Layer", layerName },
                    { "Volume", Math.Round(volume, 3) },
                    { "CentroidX", Math.Round(centroid.X, 3) },
                    { "CentroidY", Math.Round(centroid.Y, 3) },
                    { "CentroidZ", Math.Round(centroid.Z, 3) },
                    { "CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                });
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi thiết lập Property Set: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy hoặc tạo Property Set Definition
        /// </summary>
        private static ObjectId GetOrCreatePropertySetDefinition(Transaction tr, string propertySetName)
        {
            try
            {
                Database db = A.Db;
                DictionaryPropertySetDefinitions propSetDefs = new(db);

                // Kiểm tra xem Property Set Definition đã tồn tại chưa
                if (propSetDefs.Has(propertySetName, tr))
                {
                    return propSetDefs.GetAt(propertySetName);
                }

                // Tạo mới Property Set Definition
                PropertySetDefinition propSetDef = new();
                propSetDef.SubSetDatabaseDefaults(db);
                propSetDef.Description = "Property Set cho đường giao thông IFC";

                // Thêm các Property Definitions
                AddPropertyDefinitions(propSetDef);

                // Thiết lập Applies To
                StringCollection appliesToFilter = ["AcDb3dSolid"];
                propSetDef.SetAppliesToFilter(appliesToFilter, false);

                propSetDefs.AddNewRecord(propertySetName, propSetDef);
                tr.AddNewlyCreatedDBObject(propSetDef, true);

                return propSetDef.ObjectId;
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi tạo Property Set Definition: {ex.Message}");
                return ObjectId.Null;
            }
        }

        /// <summary>
        /// Thêm các Property Definitions vào Property Set Definition
        /// </summary>
        private static void AddPropertyDefinitions(PropertySetDefinition propSetDef)
        {
            // Property: Cấu kiện
            PropertyDefinition cauKienProp = new()
            {
                Name = "Cấu kiện",
                Description = "Loại cấu kiện",
                DataType = Autodesk.Aec.PropertyData.DataType.Text,
                DefaultData = ""
            };
            propSetDef.Definitions.Add(cauKienProp);

            // Property: Vật liệu
            PropertyDefinition vatLieuProp = new()
            {
                Name = "Vật liệu",
                Description = "Loại vật liệu",
                DataType = Autodesk.Aec.PropertyData.DataType.Text,
                DefaultData = ""
            };
            propSetDef.Definitions.Add(vatLieuProp);

            // Property: Thể tích (m3)
            PropertyDefinition theTichProp = new()
            {
                Name = "Thể tích (m3)",
                Description = "Thể tích của đối tượng",
                DataType = Autodesk.Aec.PropertyData.DataType.Real,
                DefaultData = 0.0
            };
            propSetDef.Definitions.Add(theTichProp);

            // Property: CentroidX
            PropertyDefinition centroidXProp = new()
            {
                Name = "CentroidX",
                Description = "Tọa độ X trọng tâm",
                DataType = Autodesk.Aec.PropertyData.DataType.Real,
                DefaultData = 0.0
            };
            propSetDef.Definitions.Add(centroidXProp);

            // Property: CentroidY
            PropertyDefinition centroidYProp = new()
            {
                Name = "CentroidY",
                Description = "Tọa độ Y trọng tâm",
                DataType = Autodesk.Aec.PropertyData.DataType.Real,
                DefaultData = 0.0
            };
            propSetDef.Definitions.Add(centroidYProp);

            // Property: CentroidZ
            PropertyDefinition centroidZProp = new()
            {
                Name = "CentroidZ",
                Description = "Tọa độ Z trọng tâm",
                DataType = Autodesk.Aec.PropertyData.DataType.Real,
                DefaultData = 0.0
            };
            propSetDef.Definitions.Add(centroidZProp);

            // Property: CreatedDate
            PropertyDefinition dateProp = new()
            {
                Name = "CreatedDate",
                Description = "Ngày tạo",
                DataType = Autodesk.Aec.PropertyData.DataType.Text,
                DefaultData = ""
            };
            propSetDef.Definitions.Add(dateProp);

            // Property: Layer
            PropertyDefinition layerProp = new()
            {
                Name = "Layer",
                Description = "Layer của đối tượng",
                DataType = Autodesk.Aec.PropertyData.DataType.Text,
                DefaultData = ""
            };
            propSetDef.Definitions.Add(layerProp);

            // Property: Volume
            PropertyDefinition volumeProp = new()
            {
                Name = "Volume",
                Description = "Thể tích (m³)",
                DataType = Autodesk.Aec.PropertyData.DataType.Real,
                DefaultData = 0.0
            };
            propSetDef.Definitions.Add(volumeProp);
        }

        /// <summary>
        /// Attach Property Set vào object
        /// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
        private static void AttachPropertySetToObject(Transaction tr, DBObject dbObject, ObjectId propertySetDefId)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            try
            {
                PropertyDataServices.AddPropertySet(dbObject, propertySetDefId);
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi attach Property Set: {ex.Message}");
            }
        }

        /// <summary>
        /// Set giá trị cho các properties
        /// </summary>
        private static void SetPropertyValues(Transaction tr, DBObject dbObject, ObjectId propertySetDefId, Dictionary<string, object> values)
        {
            try
            {
                ObjectIdCollection propertySetIds = PropertyDataServices.GetPropertySets(dbObject);
                
                foreach (ObjectId propSetId in propertySetIds)
                {
                    PropertySet? propSet = tr.GetObject(propSetId, OpenMode.ForWrite) as PropertySet;
                    if (propSet?.PropertySetDefinition == propertySetDefId)
                    {
                        PropertySetDefinition? propSetDef = tr.GetObject(propertySetDefId, OpenMode.ForRead) as PropertySetDefinition;
                        
                        foreach (var kvp in values)
                        {
                            try
                            {
                                // Tìm index của property theo tên
                                int propertyIndex = -1;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                for (int i = 0; i < propSetDef.Definitions.Count; i++)
                                {
                                    if (propSetDef.Definitions[i].Name == kvp.Key)
                                    {
                                        propertyIndex = i;
                                        break;
                                    }
                                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                                
                                if (propertyIndex >= 0)
                                {
                                    propSet.SetAt(propertyIndex, kvp.Value);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                A.Ed.WriteMessage($"\nLỗi khi set property {kvp.Key}: {ex.Message}");
                            }
                        }

                        // Set giá trị cho "Thể tích (m3)" từ Volume
                        if (values.ContainsKey("Volume"))
                        {
                            try
                            {
                                int theTichIndex = -1;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                for (int i = 0; i < propSetDef.Definitions.Count; i++)
                                {
                                    if (propSetDef.Definitions[i].Name == "Thể tích (m3)")
                                    {
                                        theTichIndex = i;
                                        break;
                                    }
                                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                                
                                if (theTichIndex >= 0)
                                {
                                    propSet.SetAt(theTichIndex, values["Volume"]);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                A.Ed.WriteMessage($"\nLỗi khi set property Thể tích (m3): {ex.Message}");
                            }
                        }
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi set property values: {ex.Message}");
            }
        }

        /// <summary>
        /// Hiển thị thông tin Property Set của 3D Solid
        /// </summary>
        public static void ShowSolidPropertySetInfo(Transaction tr, Solid3d solid)
        {
            try
            {
                A.Ed.WriteMessage($"\n=== THÔNG TIN 3D SOLID ===");
                A.Ed.WriteMessage($"\nLayer: {solid.Layer}");
                A.Ed.WriteMessage($"\nHandle: {solid.Handle}");
                
                var massProps = solid.MassProperties;
                A.Ed.WriteMessage($"\nThể tích: {massProps.Volume:F3} m³");
                A.Ed.WriteMessage($"\nTrọng tâm: X={massProps.Centroid.X:F3}, Y={massProps.Centroid.Y:F3}, Z={massProps.Centroid.Z:F3}");

                // Hiển thị Property Sets
                ObjectIdCollection propertySetIds = PropertyDataServices.GetPropertySets(solid);
                if (propertySetIds.Count > 0)
                {
                    A.Ed.WriteMessage($"\n\n=== PROPERTY SETS ({propertySetIds.Count}) ===");
                    
                    foreach (ObjectId propSetId in propertySetIds)
                    {
                        PropertySet? propSet = tr.GetObject(propSetId, OpenMode.ForRead) as PropertySet;
                        if (propSet != null)
                        {
                            PropertySetDefinition? propSetDef = tr.GetObject(propSet.PropertySetDefinition, OpenMode.ForRead) as PropertySetDefinition;
                            A.Ed.WriteMessage($"\n\nProperty Set: {propSetDef?.Name}");
                            A.Ed.WriteMessage($"Mô tả: {propSetDef?.Description}");
                            
                            if (propSetDef != null)
                            {
                                for (int i = 0; i < propSetDef.Definitions.Count; i++)
                                {
                                    PropertyDefinition propDef = propSetDef.Definitions[i];
                                    try
                                    {
                                        object value = propSet.GetAt(i);
                                        A.Ed.WriteMessage($"\n  - {propDef.Name}: {value} ({propDef.Description})");
                                    }
                                    catch
                                    {
                                        A.Ed.WriteMessage($"\n  - {propDef.Name}: <không có giá trị> ({propDef.Description})");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    A.Ed.WriteMessage($"\n\nKhông có Property Set nào được gắn vào đối tượng này.");
                }
                
                A.Ed.WriteMessage($"\n================================\n");
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\nLỗi khi hiển thị thông tin: {ex.Message}");
            }
        }
    }
}

// (C) Copyright 2015 by  
//
// AT_OffsetAlignment Command for Civil 3D
// This file contains the AT_OffsetAlignment command and its required dependencies
// 
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices.Styles;
using System;
using System.Collections.Generic;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(MyFirstProject.AT_OffsetAlignment_Civil))]

namespace MyFirstProject
{
    /// <summary>
    /// Class containing AT_OffsetAlignment command for Civil 3D
    /// </summary>
    public class AT_OffsetAlignment_Civil
    {
        // Lệnh tạo offset alignment
        [CommandMethod("AT_OffsetAlignment")]
        public static void AT_OffsetAlignment()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            using Transaction tr = db.TransactionManager.StartTransaction();
            try
            {
                var ui = new UserInputHelper();
                var alignmentService = new AlignmentServiceHelper();

                // 1. Chọn Alignment gốc trước
                ObjectId alignmentId = ui.GetAlignmentId("Chọn Alignment gốc để tạo offset: ");
                if (alignmentId == ObjectId.Null)
                {
                    ed.WriteMessage("\nKhông có Alignment nào được chọn!");
                    return;
                }
                Alignment alignment = (Alignment)tr.GetObject(alignmentId, OpenMode.ForRead);

                // 2. Nhập bề rộng offset với tùy chọn theo chuẩn AutoCAD
                double offsetWidth = 0;
                var result = ui.GetDoubleResult("Nhập bề rộng offset [Pick point] <10.0>: ", ["Pick", "P"], "Pick");
                
                if (result.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.Keyword && 
                    (result.StringResult.Equals("PICK", StringComparison.CurrentCultureIgnoreCase) || result.StringResult.Equals("P", StringComparison.CurrentCultureIgnoreCase)))
                {
                    // Chọn điểm để tính khoảng cách đến alignment
                    var pt = ui.GetPoint("Chọn điểm để đo khoảng cách đến tim đường:");
                    double station = 0, offset = 0;
                    alignment.StationOffset(pt.X, pt.Y, ref station, ref offset);
                    offsetWidth = Math.Abs(offset);
                    ed.WriteMessage($"\nKhoảng cách từ điểm đến tim đường: {offsetWidth:F3} m");
                }
                else if (result.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    offsetWidth = result.Value;
                }
                else
                {
                    // Default value
                    offsetWidth = 10.0;
                    ed.WriteMessage($"\nSử dụng giá trị mặc định: {offsetWidth} m");
                }
                
                if (offsetWidth == 0)
                {
                    ed.WriteMessage("\nBề rộng offset không hợp lệ!");
                    return;
                }

                // 3. Chọn điểm lấy lý trình đầu/cuối
                ed.WriteMessage("\nChọn điểm lấy lý trình đầu:");
                var ptStart = ui.GetPoint("Chọn điểm đầu:");
                alignmentService.GetStationAndOffsetFromPoint(ptStart, alignment, out double startStation, out double startOffset);

                ed.WriteMessage("\nChọn điểm lấy lý trình cuối:");
                var ptEnd = ui.GetPoint("Chọn điểm cuối:");
                alignmentService.GetStationAndOffsetFromPoint(ptEnd, alignment, out double endStation, out double endOffset);

                if (startStation > endStation)
                {
                    (endStation, startStation) = (startStation, endStation);
                }
                if (startStation < alignment.StartingStation || endStation > alignment.EndingStation || startStation >= endStation)
                {
                    ed.WriteMessage("\nLý trình không hợp lệ!");
                    return;
                }

                // 4. Hiển thị danh sách style và chọn style theo số thứ tự
                var styleList = alignmentService.GetAllAlignmentStyles();
                ed.WriteMessage("\nDanh sách Alignment Style:");
                for (int i = 0; i < styleList.Count; i++)
                {
                    ed.WriteMessage($"\n  {i + 1}. {styleList[i].Name}");
                }
                int styleIndex = (int)ui.GetDouble($"\nNhập số thứ tự style muốn chọn (1-{styleList.Count}, Enter=1): ");
                if (styleIndex < 1 || styleIndex > styleList.Count) styleIndex = 1;
                var styleId = styleList[styleIndex - 1].Id;

                // 5. Tạo offset alignment qua service
                string offsetName = alignment.Name + $"_Offset_{offsetWidth:F1}";
                ObjectId offsetAlignmentId = alignmentService.CreateOffsetAlignment(
                    alignment,
                    offsetName,
                    offsetWidth,
                    startStation,
                    endStation,
                    styleId
                );
                if (offsetAlignmentId != ObjectId.Null)
                {
                    ed.WriteMessage($"\nĐã tạo offset alignment: {offsetName}");
                }
                else
                {
                    ed.WriteMessage("\nKhông thể tạo offset alignment!");
                }
                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                ed.WriteMessage($"\nLỗi: {e.Message}");
            }
        }
    }

    #region Helper Classes for OffsetAlignment

    /// <summary>
    /// Helper class for user input operations
    /// </summary>
    public class UserInputHelper
    {
        private readonly Autodesk.AutoCAD.EditorInput.Editor _editor;

        public UserInputHelper()
        {
            _editor = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
        }

        public double GetDouble(string prompt)
        {
            var options = new Autodesk.AutoCAD.EditorInput.PromptDoubleOptions(prompt)
            {
                AllowNegative = true,
                AllowZero = true,
                UseDefaultValue = false
            };

            var result = _editor.GetDouble(options);
            if (result.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
            {
                return result.Value;
            }
            return 0.0;
        }

        public Autodesk.AutoCAD.EditorInput.PromptDoubleResult GetDoubleResult(string prompt, string[]? keywords = null, string? defaultKeyword = null)
        {
            var options = new Autodesk.AutoCAD.EditorInput.PromptDoubleOptions(prompt)
            {
                AllowNegative = true,
                AllowZero = true,
                UseDefaultValue = true,
                DefaultValue = 10.0
            };

            // Add keywords if provided
            if (keywords != null && keywords.Length > 0)
            {
                foreach (string keyword in keywords)
                {
                    options.Keywords.Add(keyword);
                }
                options.Keywords.Default = defaultKeyword ?? keywords[0];
            }

            return _editor.GetDouble(options);
        }

        public ObjectId GetAlignmentId(string prompt)
        {
            var options = new Autodesk.AutoCAD.EditorInput.PromptEntityOptions(prompt);
            options.SetRejectMessage("\nĐối tượng không phải là Alignment.");
            options.AddAllowedClass(typeof(Alignment), true);

            var result = _editor.GetEntity(options);
            if (result.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
            {
                return result.ObjectId;
            }
            return ObjectId.Null;
        }

        public Point3d GetPoint(string prompt)
        {
            var options = new Autodesk.AutoCAD.EditorInput.PromptPointOptions(prompt);
            var result = _editor.GetPoint(options);
            if (result.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
            {
                return result.Value;
            }
            return Point3d.Origin;
        }
    }

    /// <summary>
    /// Helper class for alignment operations
    /// </summary>
    public class AlignmentServiceHelper
    {
        private readonly Autodesk.AutoCAD.EditorInput.Editor _editor;

        public AlignmentServiceHelper()
        {
            _editor = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
        }

        /// <summary>
        /// Gets station and offset from a point relative to an alignment
        /// </summary>
        public double GetStationAndOffsetFromPoint(Point3d point, Alignment alignment, out double station, out double offset)
        {
            station = 0;
            offset = 0;

            ArgumentNullException.ThrowIfNull(alignment);

            try
            {
                alignment.StationOffset(point.X, point.Y, ref station, ref offset);
                _editor.WriteMessage($"\nStation: {station:F2} m, Offset: {offset:F2} m");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                _editor.WriteMessage($"Error getting station from point: {e.Message}");
            }
            return station;
        }

        /// <summary>
        /// Gets alignment style ID by name (if not found, returns default style)
        /// </summary>
        public ObjectId GetAlignmentStyleIdByName(string styleName)
        {
            CivilDocument civilDoc = CivilApplication.ActiveDocument;
            if (string.IsNullOrWhiteSpace(styleName))
                return civilDoc.Styles.AlignmentStyles[0]; // default

            foreach (ObjectId id in civilDoc.Styles.AlignmentStyles)
            {
                // Access style name through DBObject
                Autodesk.AutoCAD.DatabaseServices.DBObject styleObj = id.GetObject(OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.DBObject;
                if (styleObj != null)
                {
                    var nameProp = styleObj.GetType().GetProperty("Name");
                    if (nameProp != null)
                    {
                        string? name = nameProp.GetValue(styleObj) as string;
                        if (!string.IsNullOrEmpty(name) && name.Equals(styleName, StringComparison.OrdinalIgnoreCase))
                            return id;
                    }
                }
            }
            _editor.WriteMessage($"\nKhông tìm thấy style '{styleName}', dùng style mặc định.");
            return civilDoc.Styles.AlignmentStyles[0];
        }

        /// <summary>
        /// Creates offset alignment from a parent alignment, allows passing styleId
        /// </summary>
        public ObjectId CreateOffsetAlignment(Alignment parentAlignment, string offsetName, double offsetWidth, 
            double startStation, double endStation, ObjectId styleId)
        {
            ArgumentNullException.ThrowIfNull(parentAlignment);
            if (offsetWidth == 0)
                throw new ArgumentException("Offset width must not be zero", nameof(offsetWidth));
            if (startStation < parentAlignment.StartingStation || endStation > parentAlignment.EndingStation || startStation >= endStation)
                throw new ArgumentException("Invalid station range");

            Database db = parentAlignment.Database;
            ObjectId newAlignmentId = ObjectId.Null;
            
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    ObjectId offsetId = Alignment.CreateOffsetAlignment(
                        offsetName,
                        parentAlignment.ObjectId,
                        offsetWidth,
                        styleId,
                        startStation,
                        endStation
                    );
                    
                    if (offsetId != ObjectId.Null)
                    {
                        newAlignmentId = offsetId;
                        Alignment? offsetAlignment = tr.GetObject(newAlignmentId, OpenMode.ForWrite) as Alignment;
                        if (offsetAlignment != null)
                        {
                            offsetAlignment.Name = offsetName;
                        }
                    }
                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    _editor.WriteMessage($"\nLỗi tạo offset alignment: {ex.Message}");
                    tr.Abort();
                }
            }
            return newAlignmentId;
        }

        /// <summary>
        /// Returns list of all Alignment Styles (Id and Name)
        /// </summary>
        public List<(ObjectId Id, string Name)> GetAllAlignmentStyles()
        {
            var result = new List<(ObjectId, string)>();
            CivilDocument civilDoc = CivilApplication.ActiveDocument;
            
            foreach (ObjectId id in civilDoc.Styles.AlignmentStyles)
            {
                try
                {
                    using var tr = id.Database.TransactionManager.StartTransaction();
                    var styleObj = tr.GetObject(id, OpenMode.ForRead);
                    string styleName = "Unknown Style";

                    // Try different ways to get the style name
                    if (styleObj is Autodesk.Civil.DatabaseServices.Styles.AlignmentStyle alignStyle)
                    {
                        styleName = alignStyle.Name;
                    }
                    else
                    {
                        // Fallback method using reflection
                        var nameProp = styleObj.GetType().GetProperty("Name");
                        if (nameProp != null && nameProp.CanRead)
                        {
                            var name = nameProp.GetValue(styleObj) as string;
                            if (!string.IsNullOrEmpty(name))
                            {
                                styleName = name;
                            }
                        }
                    }

                    result.Add((id, styleName));
                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    _editor.WriteMessage($"\nLỗi đọc style: {ex.Message}");
                    result.Add((id, "Error Reading Style"));
                }
            }
            return result;
        }
    }

    #endregion
}

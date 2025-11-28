// (C) Copyright 2015 by  
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Civil3DCsharp.CT_Copy_NoiDung_Text_Commands))]

namespace Civil3DCsharp
{
    public class CT_Copy_NoiDung_Text_Commands
    {
        // Biến static để lưu nội dung text đã chọn cho lần sử dụng tiếp theo
        private static string savedTextContent = string.Empty;

    /// <summary>
        /// Strip formatting codes từ MText để lấy text thuần túy
        /// </summary>
        private static string StripMTextFormatting(string mTextContent)
        {
     if (string.IsNullOrEmpty(mTextContent))
       return string.Empty;

       // Loại bỏ các formatting codes phổ biến trong MText
  string cleaned = mTextContent;

// Loại bỏ color codes: {\C#;...} hoặc \C#;
       cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\\C\d+;", "");
     cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\{\\C\d+;([^}]*)\}", "$1");

     // Loại bỏ font codes: {\F...|...}
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\{\\[Ff][^;]*;([^}]*)\}", "$1");

          // Loại bỏ height codes: {\H...x;...}
    cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\{\\H[\d.]+x?;([^}]*)\}", "$1");

     // Loại bỏ width codes: {\W...;...}
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\{\\W[\d.]+;([^}]*)\}", "$1");

       // Loại bỏ underline, overline, strikethrough: {\L...}, {\O...}, {\K...}
     cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\{\\[LOKlok]([^}]*)\}", "$1");

    // Loại bỏ paragraph breaks: \P
      cleaned = cleaned.Replace("\\P", "\n");
            cleaned = cleaned.Replace("\\p", "\n");

            // Loại bỏ stacking: \S...^...;
       cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\\S([^^;]+)\^([^;]*);", "$1/$2");

          // Loại bỏ các dấu ngoặc nhọn thừa
            cleaned = cleaned.Replace("{", "").Replace("}", "");

     // Loại bỏ backslash đơn lẻ
     cleaned = cleaned.Replace("\\", "");

     return cleaned.Trim();
    }

        [CommandMethod("CT_Copy_NoiDung_Text")]
public static void CT_Copy_NoiDung_Text()
        {
         Document doc = Application.DocumentManager.MdiActiveDocument;
          Editor ed = doc.Editor;
        Database db = doc.Database;

  try
      {
            // Bước 1: Chọn text nguồn (Text, MText, hoặc DBText)
          PromptEntityOptions sourcePrompt = new PromptEntityOptions("\nChọn text nguồn để copy nội dung: ");
       sourcePrompt.SetRejectMessage("\nĐối tượng phải là Text hoặc MText!");
      sourcePrompt.AddAllowedClass(typeof(DBText), true);
sourcePrompt.AddAllowedClass(typeof(MText), true);

    PromptEntityResult sourceResult = ed.GetEntity(sourcePrompt);
 if (sourceResult.Status != PromptStatus.OK)
        {
          ed.WriteMessage("\nĐã hủy chọn text nguồn.");
        return;
  }

   // Lấy nội dung từ text nguồn
     string textContent = string.Empty;
       using (Transaction tr = db.TransactionManager.StartTransaction())
  {
           Entity sourceEntity = tr.GetObject(sourceResult.ObjectId, OpenMode.ForRead) as Entity;

               if (sourceEntity is DBText dbText)
      {
          textContent = dbText.TextString;
    }
       else if (sourceEntity is MText mText)
          {
             // Sử dụng Text property thay vì Contents để lấy text thuần túy
             // Nếu vẫn còn formatting, dùng StripMTextFormatting
  textContent = mText.Text;
 
   // Backup: nếu Text property rỗng, dùng Contents và strip formatting
     if (string.IsNullOrEmpty(textContent))
          {
              textContent = StripMTextFormatting(mText.Contents);
                  }
        }

   tr.Commit();
    }

           if (string.IsNullOrEmpty(textContent))
    {
         ed.WriteMessage("\nText nguồn không có nội dung!");
        return;
                }

         // Lưu nội dung cho lần sử dụng tiếp theo
       savedTextContent = textContent;

     ed.WriteMessage($"\nNội dung đã copy: \"{textContent}\"");
          ed.WriteMessage("\n" + new string('-', 50));

     // Bước 2: Chọn nhiều text đích để cập nhật nội dung
       PromptSelectionOptions selOptions = new PromptSelectionOptions
       {
         MessageForAdding = "\nChọn các text cần cập nhật nội dung (hoặc nhấn Enter để kết thúc): ",
     AllowDuplicates = false
       };

      // Tạo filter chỉ chọn Text và MText
          TypedValue[] filterList = new TypedValue[]
           {
 new TypedValue((int)DxfCode.Start, "TEXT,MTEXT")
        };
      SelectionFilter filter = new SelectionFilter(filterList);

  PromptSelectionResult selResult = ed.GetSelection(selOptions, filter);
      if (selResult.Status != PromptStatus.OK)
       {
           ed.WriteMessage("\nĐã hủy chọn text đích.");
    return;
     }

    SelectionSet selSet = selResult.Value;
             int successCount = 0;
          int totalCount = selSet.Count;

         // Bước 3: Cập nhật nội dung cho các text đích
        using (Transaction tr = db.TransactionManager.StartTransaction())
                {
     foreach (SelectedObject selObj in selSet)
     {
        if (selObj != null)
            {
   Entity entity = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite) as Entity;

          if (entity is DBText targetDbText)
        {
      targetDbText.TextString = textContent;
          successCount++;
           ed.WriteMessage($"\nĐã cập nhật DBText (ID: {selObj.ObjectId})");
    }
  else if (entity is MText targetMText)
                {
    // Cập nhật MText với text thuần túy (không có formatting codes)
        targetMText.Contents = textContent;
    successCount++;
         ed.WriteMessage($"\nĐã cập nhật MText (ID: {selObj.ObjectId})");
               }
   }
               }

    tr.Commit();
    }

                // Thông báo kết quả
     ed.WriteMessage("\n" + new string('=', 50));
          ed.WriteMessage($"\nHoàn thành: {successCount}/{totalCount} text đã được cập nhật nội dung.");
           ed.WriteMessage($"\nNội dung đã copy: \"{textContent}\"");
     ed.WriteMessage($"\nNội dung này đã được lưu cho lần sử dụng lệnh tiếp theo.");
          ed.WriteMessage("\n" + new string('=', 50));
            }
  catch (System.Exception ex)
          {
           ed.WriteMessage($"\nLỗi: {ex.Message}");
       }
        }
 }
}

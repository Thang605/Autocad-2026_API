using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Acad = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using ATable = Autodesk.AutoCAD.DatabaseServices.Table;
using AcadDocument = Autodesk.AutoCAD.ApplicationServices.Application;

using Civil = Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.Civil.Runtime;
using Autodesk.Civil.Settings;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Civil.ApplicationServices;
using CivSurface = Autodesk.Civil.DatabaseServices.TinSurface;
using Section = Autodesk.Civil.DatabaseServices.Section;
using Autodesk.Civil;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Autodesk.AutoCAD.Colors;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using MyFirstProject.Extensions;

namespace Civil3DCsharp
{
    public class AT_TongKichThuoc
    {
        [CommandMethod("AT_TongDoDai_Replace")]
        public static void AT_TongDoDai_Replace()
        {
            // start transantion
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();
                //start here
                ObjectIdCollection dimColl = UserInput.GSelectionSetWithType("Chọn các đối tượng (Dimension) cần tính tổng chiều dài/giá trị đo: \n", "DIMENSION");
                List<String> listMeasurement = [];

                //tạo field data
                string str1 = "%<\\AcObjProp Object(%<\\_ObjId ";
                string str2 = ">%).Measurement \\f \"%lu6\">%";
                string format = "";
                foreach (ObjectId dimId in dimColl)
                {
                    string strId = dimId.ToString().Replace("(", "").Replace(")", "");
                    format = str1 + strId + str2;
                    listMeasurement.Add(format);
                }

                //tính tổng data file
                string formatTong = "0";
                string str3 = "%<\\AcExpr (";
                string str4 = ") \\f \" %lu6\">%";
                foreach (String item in listMeasurement)
                {
                    formatTong = formatTong + "+" + item;
                }
                formatTong = str3 + formatTong + str4;
                // vẽ text
                ObjectId textId = UserInput.GTextOrMText("Chọn text/mtext cần nhập nội dung: \n");
                var textObj = tr.GetObject(textId, OpenMode.ForWrite);
                if (textObj is DBText dbText)
                    dbText.TextString = formatTong;
                else if (textObj is MText mText)
                    mText.Contents = formatTong;
                A.Ed.Command("_UPDATEFIELD", textId);

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }
    }
}

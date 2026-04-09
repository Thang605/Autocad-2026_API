---
name: Civil 3D Command Patterns
description: Các pattern và template code chuẩn để phát triển lệnh AutoCAD Civil 3D bằng C#
---

# Civil 3D Command Patterns

Skill này cung cấp các pattern và code template chuẩn để phát triển lệnh Civil 3D cho dự án này.

## 📁 Cấu trúc thư mục

```
MyFirstProject/
├── Acad Tool/          # Các lệnh AutoCAD thuần (sử dụng prefix AT_)
├── Civil Tool/         # Các lệnh Civil 3D chính (sử dụng prefix CT_)
├── Civil Tool 2/       # Các lệnh Civil 3D mở rộng
├── Extensions/         # Các extension methods
├── Help System/        # Hệ thống trợ giúp
└── Menu form/          # Các form menu
```

## 🔤 Quy tắc đặt tên lệnh

| Prefix | Ý nghĩa | Ví dụ |
|--------|---------|-------|
| `AT_` | AutoCAD Tool | `AT_PolylineFromSection` |
| `CT_` | Civil Tool (chung) | `CT_ThongTinDoiTuong` |
| `CTS_` | Civil Tool - Sample Line | `CTS_DoiTenCoc` |
| `CTA_` | Civil Tool - Alignment | `CTA_TaoDuong_ConnectedAlignment_NutGiao` |
| `CTC_` | Civil Tool - Corridor | `CTC_TaoCorridor_ChoTuyenDuong` |
| `CTP_` | Civil Tool - Profile | `CTP_ThayDoi_profile_Band` |
| `CTPV_` | Civil Tool - Profile View | `CTPV_VeTracDoc` |
| `CTSV_` | Civil Tool - Section View | `CTSV_VeTracNgangThietKe` |
| `CTSU_` | Civil Tool - Surface | `CTSU_CaoDoMatPhang_TaiCogopoint` |
| `CTPO_` | Civil Tool - Cogo Point | `CTPO_DoiTen_Cogopoint` |
| `CTPI_` | Civil Tool - Pipe | `CTPI_ThayDoi_CaoDo_DayCong` |

## 🧩 Template Command cơ bản

### Template 1: Command đơn giản với Transaction

```csharp
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.DatabaseServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(Civil3DCsharp.TenClass))]

namespace Civil3DCsharp
{
    public class TenClass
    {
        [CommandMethod("TEN_LENH")]
        public static void TenMethod()
        {
            using Transaction tr = A.Db.TransactionManager.StartTransaction();
            try
            {
                // 1. Khởi tạo utilities
                UserInput UI = new();
                UtilitiesCAD CAD = new();
                UtilitiesC3D C3D = new();

                // 2. Lấy input từ user
                ObjectId alignmentId = UserInput.GAlignmentId("\\n Chọn tim đường:");
                Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;

                // 3. Xử lý logic
                // ... code logic ở đây

                // 4. Commit transaction
                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }
    }
}
```

### Template 2: Command với Form (Dialog)

```csharp
[CommandMethod("TEN_LENH_FORM")]
public static void TenMethodForm()
{
    // 1. Hiển thị form trước
    var form = new MyFirstProject.Civil_Tool.TenForm();
    var result = Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(form);

    if (result != System.Windows.Forms.DialogResult.OK || !form.FormAccepted)
    {
        A.Ed.WriteMessage("\\n Đã hủy lệnh.");
        return;
    }

    // 2. Lấy giá trị từ form
    string value1 = form.Value1;
    int value2 = form.Value2;

    // 3. Bắt đầu transaction
    using Transaction tr = A.Db.TransactionManager.StartTransaction();
    try
    {
        // ... xử lý logic với giá trị từ form

        tr.Commit();
    }
    catch (Autodesk.AutoCAD.Runtime.Exception e)
    {
        A.Ed.WriteMessage(e.Message);
    }
}
```

### Template 3: Command với vòng lặp (nhiều lần chọn)

```csharp
[CommandMethod("TEN_LENH_LOOP")]
public static void TenMethodLoop()
{
    ObjectId profileViewId = UserInput.GProfileViewId("\\n Chọn trắc dọc:");
    string answer = "y";

    while (answer == "y")
    {
        using (Transaction tr = A.Db.TransactionManager.StartTransaction())
        {
            try
            {
                // ... xử lý logic trong mỗi vòng lặp

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                A.Ed.WriteMessage(e.Message);
            }
        }

        // Hỏi tiếp tục
        answer = UserInput.GString("Tiếp tục? (y/n)");
    }
}
```

## 🔧 Các Helper Class quan trọng

### Class A (Application shortcut)
```csharp
// Truy cập nhanh các đối tượng thường dùng
A.Doc    // Document hiện tại
A.Db     // Database hiện tại
A.Ed     // Editor hiện tại

// Ví dụ sử dụng
A.Ed.WriteMessage("\\n Thông báo: ...");
using Transaction tr = A.Db.TransactionManager.StartTransaction();
```

### Class UserInput
```csharp
// Các method chọn đối tượng
UserInput.GAlignmentId("Prompt:")           // Chọn Alignment
UserInput.GSampleLineId("Prompt:")          // Chọn SampleLine  
UserInput.GProfileViewId("Prompt:")         // Chọn ProfileView
UserInput.GSectionView("Prompt:")           // Chọn SectionView
UserInput.GCogoPointId("Prompt:")           // Chọn CogoPoint
UserInput.GObjId("Prompt:")                 // Chọn Object bất kỳ
UserInput.GTable("Prompt:")                 // Chọn Table

// Input khác
UserInput.GPoint("Prompt:")                 // Chọn điểm
UserInput.GString("Prompt:")                // Nhập chuỗi
UserInput.GInt("Prompt:")                   // Nhập số nguyên
UserInput.GSelectionSet("Prompt:")          // Chọn nhiều đối tượng
UserInput.GStopWithESC()                    // Dừng với phím ESC
```

### Class UtilitiesC3D
```csharp
// Các utility Civil 3D
UtilitiesC3D.CreateSampleline(name, groupId, alignment, station)
UtilitiesC3D.CreateCogoPointFromPoint3D(point, description)
UtilitiesC3D.SetDefaultPointSetting(styleName, labelStyleName)
```

### Class UtilitiesCAD
```csharp
// Các utility AutoCAD
UtilitiesCAD.CreateTableCoordinate(...)
UtilitiesCAD.CreateOpenPolyline(...)
```

## ⚠️ QUAN TRỌNG: Luôn dùng OpenMode.ForWrite

> [!CAUTION]
> **LUÔN sử dụng `OpenMode.ForWrite`** thay vì `OpenMode.ForRead` khi mở đối tượng trong transaction.
> 
> Việc dùng `ForRead` có thể gây **crash AutoCAD** trong một số trường hợp, đặc biệt khi:
> - Đối tượng được tham chiếu bởi đối tượng khác
> - Transaction lồng nhau
> - Đối tượng Civil 3D phức tạp (Corridor, Surface, v.v.)

```csharp
// ❌ TRÁNH - Có thể gây crash
Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;

// ✅ ĐÚNG - An toàn hơn
Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;
```

## ⚠️ Xử lý xung đột Namespace

Các alias thường dùng để tránh xung đột:

```csharp
// AutoCAD vs Windows Forms
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using DrawingFont = System.Drawing.Font;

// AutoCAD vs Civil 3D
using ATable = Autodesk.AutoCAD.DatabaseServices.Table;
using Section = Autodesk.Civil.DatabaseServices.Section;
using CivSurface = Autodesk.Civil.DatabaseServices.TinSurface;

// Namespace aliases
using Acad = Autodesk.AutoCAD.ApplicationServices;
using Civil = Autodesk.Civil.ApplicationServices;
```

## 📋 Danh sách using chuẩn cho file lệnh

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

// AutoCAD
using Autodesk.AutoCAD.Runtime;
using Acad = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

// Civil 3D
using Civil = Autodesk.Civil.ApplicationServices;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.Civil.Runtime;
using Autodesk.Civil.Settings;

// Aliases để tránh xung đột
using ATable = Autodesk.AutoCAD.DatabaseServices.Table;
using CivSurface = Autodesk.Civil.DatabaseServices.TinSurface;
using Section = Autodesk.Civil.DatabaseServices.Section;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

// Extensions của project
using MyFirstProject.Extensions;
```

## 🎨 Làm việc với các đối tượng Civil 3D

### Alignment (Tim tuyến)
```csharp
ObjectId alignmentId = UserInput.GAlignmentId("Chọn tim đường:");
Alignment? alignment = tr.GetObject(alignmentId, OpenMode.ForWrite) as Alignment;

// Các thuộc tính
alignment.Name              // Tên
alignment.Length            // Chiều dài
alignment.StartingStation   // Lý trình đầu
alignment.EndingStation     // Lý trình cuối

// Lấy tọa độ tại station
double x = 0, y = 0;
alignment.PointLocation(station, offset, ref x, ref y);

// Lấy các điểm hình học
Station[] stations = alignment.GetStationSet(StationTypes.GeometryPoint);
```

### SampleLine Group & SampleLine (Hệ cọc)
```csharp
// Lấy sample line group đầu tiên của alignment
ObjectId sampleLineGroupId = alignment.GetSampleLineGroupIds()[0];
SampleLineGroup? group = tr.GetObject(sampleLineGroupId, OpenMode.ForWrite) as SampleLineGroup;

// Tạo mới sample line group
ObjectId newGroupId = SampleLineGroup.Create(groupName, alignmentId);

// Duyệt qua các sample line
foreach (ObjectId slId in group.GetSampleLineIds())
{
    SampleLine? sl = tr.GetObject(slId, OpenMode.ForWrite) as SampleLine;
    double station = sl.Station;
    string name = sl.Name;
    int number = sl.Number;
}
```

### ProfileView (Trắc dọc)
```csharp
ObjectId pvId = UserInput.GProfileViewId("Chọn trắc dọc:");
ProfileView? pv = tr.GetObject(pvId, OpenMode.ForWrite) as ProfileView;

// Tìm station & elevation tại điểm click
double station = 0, elevation = 0;
pv.FindStationAndElevationAtXY(point.X, point.Y, ref station, ref elevation);

// Lấy alignment của profile view
ObjectId alignmentId = pv.AlignmentId;
```

### SectionView (Trắc ngang)
```csharp
ObjectId svId = UserInput.GSectionView("Chọn trắc ngang:");
SectionView? sv = tr.GetObject(svId, OpenMode.ForWrite) as SectionView;

// Tìm offset & elevation
double offset = 0, elevation = 0;
sv.FindOffsetAndElevationAtXY(point.X, point.Y, ref offset, ref elevation);

// Lấy sample line
ObjectId sampleLineId = sv.SampleLineId;
```

### Surface (Mặt phẳng địa hình)
```csharp
ObjectId surfaceId = UserInput.GObjId("Chọn mặt phẳng:");
CivSurface? surface = tr.GetObject(surfaceId, OpenMode.ForWrite) as CivSurface;

// Tìm cao độ tại tọa độ
double elevation = surface.FindElevationAtXY(x, y);
```

### CogoPoint (Điểm khảo sát)
```csharp
ObjectId pointId = UserInput.GCogoPointId("Chọn điểm:");
CogoPoint? point = tr.GetObject(pointId, OpenMode.ForWrite) as CogoPoint;

// Thuộc tính
string name = point.PointName;
double x = point.Easting;
double y = point.Northing;
double z = point.Elevation;
string description = point.RawDescription;
```

## ✅ Checklist khi tạo lệnh mới

1. [ ] Tạo file với prefix và đặt tên phù hợp (VD: `35.CTS_TenLenh.cs`)
2. [ ] Thêm `[assembly: CommandClass(...)]` ở đầu file
3. [ ] Sử dụng namespace `Civil3DCsharp`
4. [ ] Thêm các using cần thiết và alias tránh xung đột
5. [ ] Đặt tên method phù hợp với tên lệnh (VD: `CTSTenMethod`)
6. [ ] Bọc code trong `using Transaction` và `try-catch`
7. [ ] Sử dụng `A.Ed.WriteMessage()` để thông báo lỗi
8. [ ] **AI AGENT BẮT BUỘC PHẢI TỰ ĐỘNG CHẠY**: Sử dụng công cụ `run_command` để chạy `dotnet build` ngay lập tức bên trong thư mục `MyFirstProject` sau khi viết đoạn mã xong. Nếu phát sinh lỗi biên dịch, AI phải tự tìm hiểu và sửa cho tới khi thành công (0 errors) mà không cần hỏi người dùng!

## 🎀 Quản lý giao diện Ribbon (MenuConfig.xlsx)

Toàn bộ hệ thống Ribbon Menu của dự án được nạp động từ file Excel cấu hình tại:
`z:\Z.FORM MAU LAM VIEC\1. BIM\2.MAU C3D\1.LISP\0.CIVIL TOOL\Excel file\MenuConfig.xlsx`.

> [!IMPORTANT]
> - Sheet chứa cấu hình BẮT BUỘC phải có tên là `Ribbon` (ExcelMenuReader.cs sẽ tìm kiếm đích danh tên này).
> - File này sau khi cập nhật lệnh mới cần phải chạy script format màu nền để người dùng dễ nhìn. Cột phân loại bao gồm: `Tab`, `Panel`, `Loại`, `Tên hiển thị`, `Tên lệnh`, `Kích thước`, `Mô tả`.

### Cấu trúc `Loại` trên Ribbon
- **`button`**: Nút bấm độc lập thông thường. Cần có `Tên lệnh`.
- **`split`**: Nút Dropdown/Split, gồm 1 danh mục chính. Dòng `split` có thể không cần `Tên lệnh` nếu nó thuần túy là Drop-down menu con.
- **`row`**: Stack chứa tối đa 3 nút dạng nằm ngang. Dòng này bắt buộc để trống `Tên lệnh`.
- **`sub`**: Các item con nằm dưới `split` hoặc `row`. Thụt lề và có nút lệnh rõ ràng.
- Bố trí logic tiêu chuẩn: 3 items mỗi `row` để tiết kiệm khoảng không. 

### Quy định Format Màu sắc tự động
Để làm đẹp giao diện file Excel sau mỗi lần chỉnh sửa (thêm lệnh), AI hãy lưu trữ vòng lặp Python sau và tự động chạy nó dưới nền nếu user yêu cầu định dạng / hoặc có thay đổi lớn:

```python
# Script: format_colors.py
import openpyxl
from openpyxl.styles import PatternFill, Font, Alignment, Border, Side
excel_path = r'z:\Z.FORM MAU LAM VIEC\1. BIM\2.MAU C3D\1.LISP\0.CIVIL TOOL\Excel file\MenuConfig.xlsx'
wb = openpyxl.load_workbook(excel_path)
ws = wb["Ribbon"]

# Style definitions
h_fill = PatternFill(start_color="1F4E78", end_color="1F4E78", fill_type="solid")
c_fill = PatternFill(start_color="DDEBF7", end_color="DDEBF7", fill_type="solid")
a_fill = PatternFill(start_color="E2EFDA", end_color="E2EFDA", fill_type="solid")
sub_fill = PatternFill(start_color="F2F2F2", end_color="F2F2F2", fill_type="solid")
border = Border(left=Side(style='thin', color='A6A6A6'), right=Side(style='thin', color='A6A6A6'),
                top=Side(style='thin', color='A6A6A6'), bottom=Side(style='thin', color='A6A6A6'))
c_align = Alignment(horizontal="center", vertical="center")

# Apply Header
for cell in ws[1]:
    cell.fill, cell.font, cell.alignment, cell.border = h_fill, Font(color="FFFFFF", bold=True), c_align, border

# Apply Rows
curr_tab = None
for row in ws.iter_rows(min_row=2):
    tab_val, row_type = row[0].value, row[2].value
    if tab_val and "civil" in str(tab_val).lower(): curr_tab = "civil"
    elif tab_val and "acad" in str(tab_val).lower(): curr_tab = "acad"
    
    fill = openpyxl.styles.PatternFill(fill_type=None)
    if row_type == "sub": fill = sub_fill
    elif curr_tab == "civil": fill = c_fill
    elif curr_tab == "acad": fill = a_fill
    
    for idx, cell in enumerate(row):
        if fill.fill_type: cell.fill = fill
        cell.border = border
        cell.alignment = c_align if idx in [0, 1, 2, 5] else Alignment(horizontal="left", vertical="center")

wb.save(excel_path)
```

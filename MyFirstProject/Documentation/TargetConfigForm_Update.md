# C?p nh?t l?nh CTC_TaoCooridor_DuongDoThi_RePhai

## T?ng quan
L?nh `CTC_TaoCooridor_DuongDoThi_RePhai` ?ã ???c c?p nh?t ?? bao g?m form l?a ch?n g?n k?t gi?a các `subassemblyTargetInfoCollection` và `TagetIds_0`, `TagetIds_1`, `TagetIds_3`.

## Tính n?ng m?i

### 1. SubassemblyTargetConfigForm
- **M?c ?ích**: Cho phép ng??i dùng c?u hình cách g?n k?t gi?a các subassembly targets và target collections
- **V? trí**: `MyFirstProject\Civil Tool\SubassemblyTargetConfigForm.cs`

### 2. Tính n?ng chính c?a form:

#### Target Groups ???c h? tr?:
- **Target Group 0**: Alignments (Tim ???ng)
- **Target Group 1**: Profiles (H? s? tim ???ng)  
- **Target Group 3**: Polylines/Surfaces (???ng bao, b? m?t)

#### C?u hình cho m?i Subassembly Target:
- **Ch? s?**: V? trí c?a subassembly target trong collection
- **Tên Subassembly Target**: **? IMPROVED** - S? d?ng `targetInfo.SubassemblyName` v?i fallback thông minh
- **Lo?i Target**: **? IMPROVED** - Mô t? chi ti?t v?i ParameterName và TargetType
- **Target Group**: ComboBox ch?n nhóm target ?? g?n k?t
- **Tùy ch?n Target**: ComboBox c?u hình cách ch?n target (Nearest)

#### ? C?i ti?n hi?n th? tên SubassemblyTargetInfo:
**Ph??ng pháp m?i:**
1. **?u tiên 1**: S? d?ng `targetInfo.SubassemblyName` tr?c ti?p
2. **?u tiên 2**: S? d?ng Reflection ?? truy c?p properties
3. **?u tiên 3**: Phân tích t? TargetIds có s?n
4. **Fallback**: Tên m?c ??nh `SubassemblyTarget_[index]`

**Format hi?n th?:**
- `{SubassemblyName} (Target_{index})` - N?u có SubassemblyName
- `Target_{index} ([Type]: [Name])` - N?u nh?n di?n ???c t? objects
- `SubassemblyTarget_{index}` - Fallback

## Cách s? d?ng

### 1. Ch?y l?nh
```
CTC_TaoCooridor_DuongDoThi_RePhai
```

### 2. Th?c hi?n các b??c trong form chính:
1. Ch?n Corridor g?c
2. Ch?n Target Alignments (Target 1 và Target 2)
3. Ch?n Assembly
4. C?u hình s? l??ng corridor r? ph?i c?n t?o
5. Ch?n Alignment và Polyline cho t?ng c?p

### 3. Form c?u hình Target s? hi?n th?:
- Form `SubassemblyTargetConfigForm` s? t? ??ng hi?n th? sau khi t?o baseline region
- **? Debug information**: In ra t?t c? properties có s?n c?a SubassemblyTargetInfo
- Hi?n th? DataGridView v?i các c?t ???c c?i ti?n:
  - **Ch? s?**: Index c?a subassembly target
  - **Tên Subassembly Target**: **Tên th?c t? SubassemblyName**
  - **Lo?i Target**: Thông tin chi ti?t v? target collection
  - **Target Group**: ComboBox ?? ch?n nhóm g?n k?t
  - **Tùy ch?n Target**: ComboBox ?? ch?n option

### 4. Tùy ch?n Target Group:
- **Không g?n k?t**: B? qua target này
- **Group 0 (Alignments)**: G?n v?i TagetIds_0
- **Group 1 (Profiles)**: G?n v?i TagetIds_1  
- **Group 3 (Polylines/Surfaces)**: G?n v?i TagetIds_3

### 5. K?t qu?:
- **Áp d?ng**: S? d?ng c?u hình t? form
- **H?y**: S? d?ng c?u hình m?c ??nh (logic c?)
- Log chi ti?t v? quá trình áp d?ng c?u hình

## C?u hình m?c ??nh
N?u ng??i dùng h?y form c?u hình, h? th?ng s? áp d?ng logic m?c ??nh:
- Subassembly index 0 ? Target Group 1 (Profiles)
- Subassembly index 1 ? Target Group 0 (Alignments) 
- Subassembly index 3 ? Target Group 3 (Polylines/Surfaces)

## C?i ti?n k? thu?t

### 1. Method ???c c?p nh?t:
- `TaoCooridorDuongDoThiWithAssembly`: Thêm logic hi?n th? form c?u hình target
- `TaoCooridorDuongDoThi`: V?n t??ng thích v?i logic c?

### 2. Class m?i:
- `SubassemblyTargetConfigForm`: Form c?u hình target connections
- `TargetConnection`: Class ch?a thông tin g?n k?t target
- `TargetGroupItem`: Class cho ComboBox target groups
- `TargetOptionItem`: Class cho ComboBox target options

### 3. ? C?i ti?n naming SubassemblyTargetInfo:

#### Method `GetSubassemblyTargetName`:
```csharp
private string GetSubassemblyTargetName(SubassemblyTargetInfo targetInfo, int index)
{
    // 1. Try direct access to SubassemblyName
    string subassemblyName = targetInfo.SubassemblyName;
    
    // 2. Use reflection if direct access fails
    // 3. Analyze from TargetIds if available
    // 4. Fallback to default naming
}
```

#### Method `GetTargetTypeDescription`:
```csharp
private string GetTargetTypeDescription(SubassemblyTargetInfo targetInfo)
{
    // 1. Try to get TargetType property
    // 2. Try to get ParameterName property  
    // 3. Fallback to target count description
}
```

### 4. ? Debug và Reflection Features:
- **Property inspection**: In ra t?t c? properties c?a SubassemblyTargetInfo
- **Direct property access**: Th? truy c?p `SubassemblyName` tr?c ti?p
- **Reflection fallback**: S? d?ng reflection n?u direct access th?t b?i
- **Error handling**: Comprehensive error handling cho m?i tr??ng h?p

## Debugging và Logging

### 1. Debug SubassemblyTargetInfo Properties:
```
=== Debug: SubassemblyTargetInfo Properties ===
Property: SubassemblyName = LaneSuperelevationAOR (Type: String)
Property: TargetIds = System.Collections.ObjectModel.Collection`1[...] (Type: ObjectIdCollection)
Property: TargetToOption = Nearest (Type: SubassemblyTargetToOption)
=== End Debug Info ===

Target 0: Name='LaneSuperelevationAOR (Target_0)', Type='Parameter: Alignment (2 ??i t??ng)'
Target 1: Name='ProfileGrade (Target_1)', Type='Parameter: Profile (2 ??i t??ng)'
```

### 2. Command Line Output:
```
=== Áp d?ng c?u hình Target Connections ===
S? l??ng subassembly targets: 4
S? l??ng connections c?u hình: 4

Target 0 (LaneSuperelevationAOR (Target_0)):
  - G?n k?t v?i: Profiles (2 ??i t??ng)
  - Tùy ch?n: Nearest

Target 1 (ProfileGrade (Target_1)):
  - G?n k?t v?i: Alignments (2 ??i t??ng)
  - Tùy ch?n: Nearest

? ?ã áp d?ng c?u hình target cho 4 subassembly targets.
```

## Test Commands

### 1. `TestTargetConfigForm` - Test c? b?n
### 2. `TestTargetConfigFormWithRealData` - Test v?i d? li?u th?c + Debug info:
```csharp
// Debug: Print information about each target
for (int i = 0; i < targets.Count; i++)
{
    var target = targets[i];
    string subassemblyName = target.SubassemblyName; // Direct access test
    // Print all available information
}
```

## L?u ý
- ? **SubassemblyName Access**: ?ã implement truy c?p tr?c ti?p `targetInfo.SubassemblyName`
- ? **Reflection Support**: Fallback s? d?ng reflection n?u c?n
- ? **Debug Information**: In ra t?t c? properties có s?n
- ? **Error Handling**: Comprehensive error handling
- Form yêu c?u d? li?u th?c t? t? subassembly targets ?? ho?t ??ng ??y ??
- C?u hình s? ???c áp d?ng ngay l?p t?c cho baseline region
- M?i thay ??i ??u ???c ghi log vào AutoCAD command line

## Các v?n ?? ?ã gi?i quy?t
1. ? **DataGridView ComboBox Error**: S? d?ng proper DataSource binding
2. ? **SubassemblyTargetInfo Naming**: **MAJOR IMPROVEMENT** - S? d?ng `targetInfo.SubassemblyName`
3. ? **Property Access**: Direct access + Reflection fallback
4. ? **Debug Information**: Comprehensive property inspection
5. ? **Better User Experience**: Hi?n th? tên th?c c?a subassembly targets
6. ? **Error Handling**: Robust error handling cho t?t c? scenarios

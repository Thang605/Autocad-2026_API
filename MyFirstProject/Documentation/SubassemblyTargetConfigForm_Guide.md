# H??ng d?n s? d?ng SubassemblyTargetConfigForm

## T?ng quan

`SubassemblyTargetConfigForm` là m?t Windows Form cho phép ng??i dùng c?u hình target cho t?ng subassembly trong m?t Assembly c?a Civil 3D Corridor m?t cách linh ho?t và tr?c quan.

## V?n ?? gi?i quy?t

Trong m?t Assembly, có nhi?u subassembly, m?i subassembly c?n target ??n các ??i t??ng khác nhau:
- **Offset targets**: C?n alignments
- **Elevation targets**: C?n profiles  
- **Surface targets**: C?n surfaces
- **Custom targets**: Có th? c?n polylines ho?c các ??i t??ng khác

Form này cho phép b?n:
1. Xem t?t c? subassembly targets và lo?i c?a chúng
2. Ch?n Target Group phù h?p cho t?ng subassembly
3. Thi?t l?p Target Option (Nearest, Left, Right)
4. T? ??ng g?i ý target group d?a trên lo?i subassembly
5. Áp d?ng c?u hình m?t cách d? dàng

## Cách s? d?ng

### 1. S? d?ng trong l?nh t?o Corridor

Khi ch?y l?nh `CTC_TaoCooridor_DuongDoThi_RePhai`, form s? t? ??ng hi?n th? sau khi t?o baseline region.

```
Command: CTC_TaoCooridor_DuongDoThi_RePhai
```

Th?c hi?n các b??c:
1. Ch?n Corridor g?c
2. Ch?n 2 Target Alignments (trái và ph?i)
3. Ch?n Assembly
4. Nh?p s? l??ng alignment r? ph?i
5. Ch?n t?ng c?p Alignment-Polyline

**Form s? t? ??ng hi?n th?** ?? c?u hình targets.

### 2. Test Form v?i d? li?u th?c

S? d?ng l?nh test ?? th? nghi?m form v?i corridor có s?n:

```
Command: TestTargetConfigForm
```

Các b??c:
1. Ch?n corridor ?? test
2. Form s? hi?n th? v?i d? li?u th?c t? corridor
3. C?u hình targets
4. Ch?n có/không áp d?ng vào baseline region

### 3. Debug thông tin Corridor

?? xem chi ti?t thông tin v? subassembly targets:

```
Command: TestTargetConfigFormDebug
```

## Giao di?n Form

### Ph?n Header
- **Tiêu ??**: "C?U HÌNH TARGET CHO SUBASSEMBLIES"
- **H??ng d?n**: Mô t? ng?n g?n cách s? d?ng
- **Thông tin Targets**: Hi?n th? s? l??ng targets kh? d?ng cho m?i lo?i

### DataGridView (B?ng chính)

Các c?t:

1. **Ch? s?**: S? th? t? c?a subassembly (1, 2, 3, ...)
2. **Tên Subassembly**: Tên th?c c?a subassembly (n?u có)
3. **Lo?i Target**: Lo?i target và s? l??ng ??i t??ng hi?n t?i
4. **Target Group** (ComboBox): Ch?n nhóm target ?? g?n k?t
   - ? Không g?n k?t
   - ? Alignments (n)
   - ? Profiles (n)
   - ? Surfaces (n)
   - ? Polylines/Other (n)
5. **Tùy ch?n Target** (ComboBox): Cách ch?n target
   - Nearest (G?n nh?t)
   - Left (Bên trái)
   - Right (Bên ph?i)

### Nút ?i?u khi?n

1. **T? ??ng c?u hình** (màu vàng)
   - T? ??ng ch?n target group phù h?p cho t?t c? subassemblies
   - D?a trên lo?i target type c?a t?ng subassembly

2. **Áp d?ng** (màu xanh lá)
   - Áp d?ng c?u hình ?ã ch?n
   - ?óng form và tr? v? k?t qu?

3. **H?y** (màu ??)
   - H?y b? thay ??i
   - S? d?ng c?u hình m?c ??nh

## Quy trình ho?t ??ng

### 1. G?i ý t? ??ng (Auto-suggestion)

Khi form ???c m?, h? th?ng s? t? ??ng g?i ý Target Group d?a trên lo?i target:

- **Elevation targets** ? G?i ý: Profiles
- **Offset targets** ? G?i ý: Alignments
- **Surface targets** ? G?i ý: Surfaces
- **Khác** ? G?i ý: Polylines (n?u có)

### 2. C?u hình th? công

Ng??i dùng có th? thay ??i g?i ý b?ng cách:
1. Click vào cell trong c?t "Target Group"
2. Ch?n nhóm target mong mu?n t? dropdown
3. Click vào cell trong c?t "Tùy ch?n Target"
4. Ch?n option mong mu?n

### 3. T? ??ng c?u hình toàn b?

Click nút "T? ??ng c?u hình" ??:
- Áp d?ng g?i ý t? ??ng cho t?t c? subassemblies
- ??t Target Option = Nearest cho t?t c?
- Ti?t ki?m th?i gian khi có nhi?u subassemblies

### 4. Áp d?ng c?u hình

Khi click "Áp d?ng":
1. Form thu th?p t?t c? c?u hình ?ã ch?n
2. T?o danh sách TargetConnection
3. Gán targets cho t?ng SubassemblyTargetInfo
4. Áp d?ng vào BaselineRegion
5. ?óng form và tr? v? DialogResult.OK

## Target Groups

### Group 0: Alignments
- Dùng cho: Offset targets, Width targets
- Ch?a: T?t c? alignments trong b?n v?
- Ví d?: Target boundaries cho lane widening

### Group 1: Profiles  
- Dùng cho: Elevation targets, Slope targets
- Ch?a: T?t c? profiles t? các alignments
- Ví d?: Target cao ?? cho profile grading

### Group 2: Surfaces
- Dùng cho: Surface targets, Daylight targets
- Ch?a: T?t c? surfaces trong b?n v?
- Ví d?: Target taluy, daylight to surface

### Group 3: Polylines/Other
- Dùng cho: Custom targets, Feature lines
- Ch?a: Polylines và các ??i t??ng khác
- Ví d?: Target biên gi?i, ranh gi?i khu v?c

## Target Options

### Nearest
- Ch?n target g?n nh?t v?i subassembly
- Phù h?p: H?u h?t các tr??ng h?p
- M?c ??nh: Yes

### Left
- Ch?n target bên trái
- Phù h?p: Khi c?n xác ??nh rõ phía trái
- Ví d?: Ranh gi?i trái c?a ???ng

### Right
- Ch?n target bên ph?i  
- Phù h?p: Khi c?n xác ??nh rõ phía ph?i
- Ví d?: Ranh gi?i ph?i c?a ???ng

## Các tr??ng h?p s? d?ng th?c t?

### Tr??ng h?p 1: ???ng ?ô th? v?i v?a hè

Assembly bao g?m:
- Lane (làn ???ng chính)
- Curb (Rìa v?a hè)
- Sidewalk (V?a hè)
- Daylight (Taluy)

C?u hình:
1. Lane ? Profiles (cho cao ?? n?n ???ng)
2. Curb ? Alignments (cho v? trí rìa)
3. Sidewalk ? Alignments (cho chi?u r?ng)
4. Daylight ? Surfaces (cho taluy xu?ng m?t ??t t? nhiên)

### Tr??ng h?p 2: Ngã r? v?i biên gi?i tùy ch?nh

Assembly bao g?m:
- Turn lane (Làn r?)
- Boundary (Biên gi?i)
- Grading (N?n)

C?u hình:
1. Turn lane ? Profiles (cao ??)
2. Boundary ? Polylines (polyline biên gi?i tùy ch?nh)
3. Grading ? Surfaces (grading xu?ng surface)

### Tr??ng h?p 3: ???ng cao t?c ph?c t?p

Assembly bao g?m:
- Multiple lanes (Nhi?u làn)
- Median (D?i phân cách)
- Shoulders (L? ???ng)
- Side slopes (Taluy)

C?u hình:
1. Lanes ? Profiles (cao ?? thi?t k?)
2. Median ? Alignments (v? trí d?i phân cách)
3. Shoulders ? Alignments (chi?u r?ng l?)
4. Side slopes ? Surfaces (taluy t? nhiên)

## L?u ý k? thu?t

### Yêu c?u t?i thi?u
- M?i subassembly target c?n **ít nh?t 2 targets** ?? ho?t ??ng
- N?u ch? có 1 target, h? th?ng s? t? ??ng nhân ?ôi

### X? lý l?i
- N?u không có target nào kh? d?ng cho m?t group, subassembly s? b? b? qua
- Form hi?n th? c?nh báo khi có l?i c?u hình
- Log chi ti?t ???c ghi vào AutoCAD command line

### T??ng thích
- Civil 3D 2024 tr? lên
- .NET 8.0
- Windows Forms

## Troubleshooting

### Form không hi?n th?
**Nguyên nhân**: Không có subassembly targets trong baseline region  
**Gi?i pháp**: Ki?m tra assembly có subassembly targets không

### Không gán ???c targets
**Nguyên nhân**: Target collection r?ng  
**Gi?i pháp**: ??m b?o có alignments, profiles, surfaces trong b?n v?

### Targets không ho?t ??ng sau khi áp d?ng
**Nguyên nhân**: Lo?i target không phù h?p  
**Gi?i pháp**: Ki?m tra l?i target type và ch?n group phù h?p

### L?i "SubassemblyName not available"
**Nguyên nhân**: Assembly c? không có property SubassemblyName  
**Gi?i pháp**: H? th?ng s? t? ??ng fallback sang tên m?c ??nh

## Best Practices

1. **Luôn ch?y Debug command tr??c** ?? hi?u c?u trúc corridor
2. **S? d?ng Auto-config** cho l?n c?u hình ??u tiên
3. **Ki?m tra k?t qu?** trong Corridor Properties c?a Civil 3D
4. **L?u template** cho các assembly th??ng dùng
5. **Test v?i corridor ??n gi?n** tr??c khi áp d?ng cho d? án l?n

## Tài li?u tham kh?o

- Civil 3D API Documentation: SubassemblyTargetInfo
- Civil 3D API Documentation: BaselineRegion.SetTargets()
- Autodesk Knowledge Network: Corridor Targets

## Tác gi? & H? tr?

- **Tác gi?**: AI Agent Development Team
- **Version**: 1.0.0
- **Ngày t?o**: 2024
- **H? tr?**: Xem documentation trong source code

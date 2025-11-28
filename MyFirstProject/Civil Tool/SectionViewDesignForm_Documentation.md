# Tài li?u Form thi?t l?p v? tr?c ngang thi?t k? (SectionViewDesignForm)

## T?ng quan
Form `SectionViewDesignForm` ???c t?o ra ?? thay th? vi?c nh?p li?u th? công trong l?nh `CTSV_VeTracNgangThietKe`. Form này cung c?p giao di?n ??y ?? và thân thi?n v?i ng??i dùng ?? thi?t l?p t?t c? các tham s? c?n thi?t cho vi?c t?o tr?c ngang thi?t k?.

## Các nhóm input chính

### 1. Ch?n tim ???ng (Alignment Selection)
- **Tim ???ng**: Ch?n alignment ?? v? tr?c ngang
- **Nút "Ch?n"**: Cho phép ng??i dùng ch?n alignment tr?c ti?p t? model

### 2. V? trí ??t và Layout (Placement and Layout)
- **?i?m ??t**: Ch?n v? trí ?? ??t tr?c ngang trong drawing
- **Layout Template**: ???ng d?n ??n file template layout (m?c ??nh: LAYOUT CIVIL 3D.dwt)
- **Layout Name**: Tên layout s? s? d?ng (A3-TN-1-200, A3-TN-1-500, A3-TN-1-1000, A4-TN-1-200)

### 3. Ngu?n d? li?u tr?c ngang (Section Sources)
- **T? ??ng c?u hình**: T? ??ng ch?n và c?u hình section sources d?a trên tên
- **DataGrid**: Hi?n th? danh sách t?t c? section sources v?i các c?t:
  - **S? d?ng**: Checkbox ?? b?t/t?t section source
  - **Lo?i ngu?n**: Lo?i source (TinSurface, Corridor, CorridorSurface, Material)
  - **Tên ngu?n**: Tên c?a source
  - **Style**: Dropdown ?? ch?n style phù h?p

### 4. Styles (Styles Configuration)
- **Section View Style**: Style cho section view
- **Plot Style**: Style cho plot layout

### 5. V?t li?u (Material Configuration)
- **T?o material list**: Checkbox ?? t?o material list
- **Tên material list**: Tên c?a material list (m?c ??nh: "B?ng ?ào ??p")

### 6. B?ng kh?i l??ng (Volume Table)
- **T?o b?ng kh?i l??ng**: Checkbox ?? t?o b?ng kh?i l??ng
- **V? trí b?ng**: V? trí ??t b?ng (TopLeft, TopRight, BottomLeft, BottomRight)

### 7. Bands và Text (Bands and Text)
- **Thêm elevation bands**: Thêm bands hi?n th? cao ??
- **Thêm distance bands**: Thêm bands hi?n th? kho?ng cách
- **Thêm material text**: Thêm text hi?n th? kh?i l??ng v?t li?u

## Các input b? sung ?? xu?t

### Input hi?n t?i trong form:
1. **Alignment selection** - Ch?n tim ???ng
2. **Placement point** - V? trí ??t tr?c ngang
3. **Layout configuration** - C?u hình layout template và name
4. **Section sources management** - Qu?n lý ngu?n d? li?u v?i style
5. **Style configuration** - C?u hình các style
6. **Material list settings** - Thi?t l?p material list
7. **Volume table settings** - Thi?t l?p b?ng kh?i l??ng
8. **Bands and text options** - Tùy ch?n bands và text

### Các input b? sung có th? thêm vào:

#### A. Station Range Configuration
- **Start Station**: Lý trình b?t ??u (hi?n t?i dùng alignment start station)
- **End Station**: Lý trình k?t thúc (hi?n t?i dùng alignment end station)
- **Station Interval**: Kho?ng cách gi?a các station

#### B. Section View Appearance
- **Section View Height**: Chi?u cao c?a section view
- **Section View Width**: Chi?u r?ng c?a section view
- **Vertical Exaggeration**: T? l? phóng ??i theo chi?u d?c
- **Grid Display**: Hi?n th? l??i trong section view

#### C. Label Configuration
- **Grade Break Labels**: C?u hình labels cho grade breaks
- **Station Labels**: C?u hình labels cho station
- **Elevation Labels**: C?u hình labels cho elevation
- **Weeding Distance**: Kho?ng cách t?i thi?u gi?a các labels

#### D. Band Configuration
- **Band Height**: Chi?u cao c?a bands
- **Band Position**: V? trí c?a bands (top/bottom)
- **Custom Band Styles**: Ch?n custom styles cho t?ng lo?i band

#### E. Material Configuration Advanced
- **Cut Material Name**: Tên v?t li?u ?ào (m?c ??nh: "?ào n?n")
- **Fill Material Name**: Tên v?t li?u ??p (m?c ??nh: "??p n?n")
- **Material Shape Styles**: Style cho hình d?ng v?t li?u
- **Material Criteria**: Tiêu chí tính toán v?t li?u

#### F. Output Configuration
- **Create Summary Table**: T?o b?ng t?ng h?p kh?i l??ng
- **Table Format Options**: Tùy ch?n format b?ng
- **Export Options**: Tùy ch?n xu?t d? li?u
- **Layer Management**: Qu?n lý layer cho các elements

## ?u ?i?m c?a form hi?n t?i

1. **Giao di?n tr?c quan**: Form ???c chia thành các nhóm logic rõ ràng
2. **Validation**: Form có validation ??u vào ?? tránh l?i
3. **Auto-configuration**: T? ??ng c?u hình section sources d?a trên tên
4. **Style management**: Tích h?p qu?n lý styles t? document
5. **Flexible options**: Nhi?u tùy ch?n cho bands, tables, và text
6. **Error handling**: X? lý l?i và thông báo ng??i dùng

## Cách s? d?ng

1. Ch?y l?nh `CTSV_VeTracNgangThietKe`
2. Form s? hi?n th?
3. Ch?n alignment và ?i?m ??t
4. C?u hình section sources và styles
5. Thi?t l?p các tùy ch?n khác
6. Nh?n OK ?? th?c hi?n

## L?u ý k? thu?t

- Form s? d?ng `System.Windows.Forms.Application.Run()` ?? hi?n th? modal
- T?t c? styles ???c load t? document hi?n t?i
- Auto-configuration d?a trên naming convention
- Form validate ??u vào tr??c khi th?c hi?n l?nh
- H? tr? error handling và user feedback

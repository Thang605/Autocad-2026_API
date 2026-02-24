---
description: Quy tắc chạy lệnh PowerShell phức tạp qua file T27_AutoRun.ps1
---

# Quy tắc chạy PowerShell qua T27_AutoRun.ps1

Đối với mọi lệnh PowerShell phức tạp hoặc có cấu trúc thay đổi, KHÔNG chạy trực tiếp trên Terminal. Thay vào đó, thực hiện đúng 2 bước:

## Bước 1: Ghi nội dung lệnh vào file tạm

- File: `c:\Dropbox\DATA\AI Agent\Autocad 2026_API\T27_AutoRun.ps1`
- Sử dụng `write_to_file` với `Overwrite: true` để ghi nội dung script PowerShell vào file này.
- File này là file tạm cố định, luôn được ghi đè mỗi lần sử dụng.

## Bước 2: Chạy file script

// turbo
- Chạy duy nhất lệnh: `powershell -File T27_AutoRun.ps1`
- Cwd: `c:\Dropbox\DATA\AI Agent\Autocad 2026_API`
- Đặt `SafeToAutoRun: true` vì user đã chọn "Always run" cho lệnh này.

## Lưu ý

- Chỉ áp dụng cho lệnh PowerShell **phức tạp** hoặc **có cấu trúc thay đổi** (multi-line, pipeline, loops, etc.)
- Lệnh đơn giản 1 dòng (ví dụ: `git status`, `dir`, `cat file.txt`) vẫn có thể chạy trực tiếp.
- User chỉ cần approve "Always run" một lần duy nhất cho `powershell -File T27_AutoRun.ps1`.

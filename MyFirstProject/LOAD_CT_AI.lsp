(defun c:LOAD_CT_AI ( / src dst rand ext)
  ;; Đường dẫn gốc file DLL
  (setq src "Y:/5.SOFT T27/1. FOR WORK/1. THIET KE DUONG/2.CIVIL 3D/2026/AutoCAD Civil 3D 2026 Win x64/x64/c3d/Civil3D2026.dll")

  (command "_SECURELOAD" "0")
  ;; Tạo chuỗi ngẫu nhiên (dựa trên số giây hiện tại)
  (setq rand (vl-string-translate ":" "" (rtos (getvar "DATE") 2 6)))
  (setq ext  (vl-filename-extension src))
  
  ;; Tạo tên file mới cùng thư mục, có chuỗi ngẫu nhiên
  (setq dst (strcat (vl-filename-directory src) "\\" rand ext))
  
  ;; Copy file sang tên mới
  (vl-file-copy src dst)
  
  ;; Chạy lệnh netload với file mới
  (command "_CLIPROMPTLINES" "0")
  (command "_NETLOAD" dst)
  (command "_SECURELOAD" "1")
  (command "_CLIPROMPTLINES" "2")

  ;; In ra thông báo giả lập load
  (repeat 250 
    (princ "\Loading AECC Land Manager Items...
Loading AECC Hydrology Manager Items...
Loading AECC Pipe Network Manager Items...
Loading AECC Pressure Pipes Manager Items...
Loading AECC Roadway Manager Items...
Loading AECC Survey Manager Items...
Loading AECC Plan Production Manager Items...
Loading AECC Building Site Manager Items..."))
  ;; Gọi SHOW_MENU một lần nữa sau khi hiển thị thông báo (nếu bạn muốn đảm bảo menu hiển thị sau cùng)
  (command "SHOW_MENU")
  
  (princ)
)

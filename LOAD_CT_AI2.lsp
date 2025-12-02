(defun c:LOAD_CT_AI2 ( / src dst rand ext)
  ;; Đường dẫn gốc file DLL
  (setq src "c:\\Dropbox\\DATA\\AI Agent\\Autocad 2026_API\\MyFirstProject\\bin\\Debug\\Civil3D_Tools.dll")

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

  ;; In ra thông báo
  (princ "\nDLL Loaded successfully via Shadow Copy (Civil3D_Tools.dll).")
  (command "SHOW_MENU")
  
  (princ)
)
(c:LOAD_CT_AI2)

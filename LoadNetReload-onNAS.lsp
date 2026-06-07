;;; LoadNetReload-onNAS.lsp
;;; Tự động load NetReload.dll từ NAS mà KHÔNG khoá file gốc
;;; Copy DLL sang file tạm với tên ngẫu nhiên rồi load file tạm đó
;;; Đặt file này vào thư mục Support của AutoCAD hoặc load thủ công bằng APPLOAD

(defun C:LOAD_CT_AI ( / srcDll tempDir randName tempDll
                        _oldCmdEcho _oldNoMutt _oldSecure)
  ;; === Lưu và tắt echo NGAY TỪ ĐẦU để ẩn mọi output ===
  (setq _oldCmdEcho (getvar "CMDECHO")
        _oldNoMutt  (getvar "NOMUTT")
        _oldSecure  (getvar "SECURELOAD"))
  (setvar "CMDECHO" 0)
  (setvar "NOMUTT" 1)
  (setvar "SECURELOAD" 0)

  ;; Đường dẫn file DLL gốc trên NAS
  (setq srcDll "Z:\\Z.FORM MAU LAM VIEC\\1. BIM\\2.MAU C3D\\1.LISP\\0.CIVIL TOOL\\civil3d2026.dll")

  ;; Kiểm tra file gốc tồn tại
  (if (not (findfile srcDll))
    (progn
      (setvar "CMDECHO" _oldCmdEcho)
      (setvar "NOMUTT" _oldNoMutt)
      (setvar "SECURELOAD" _oldSecure)
      (princ "\n*** ERROR: Không tìm thấy file DLL ***")
      (princ)
      (exit)
    )
  )

  ;; Tạo tên file tạm ngẫu nhiên CÙNG thư mục với file DLL gốc
  (setq tempDir (vl-filename-directory srcDll)
        randName (strcat "NRL_"
                   (itoa (fix (getvar "CDATE"))) "_"
                   (itoa (fix (* (rem (getvar "CDATE") 1) 1000000))) "_"
                   (itoa (fix (* (rem (getvar "TDUSRTIMER") 1000) 1000))))
        tempDll (strcat tempDir "\\" randName ".dll"))

  ;; Copy đồng bộ bằng vl-file-copy — không cần spawn cmd.exe, không cần busy-wait
  (if (vl-file-copy srcDll tempDll)
    (progn
      ;; Load DLL
      (command "._NETLOAD" tempDll)
      ;; Load NetReload.dll để có lệnh NRL và RELOAD
      (setq localNrlDll "c:\\OneDrive\\0.AI AGENT\\6.C#\\Autocad 2026_API\\NetReload\\bin\\Debug\\NetReload.dll")
      (if (findfile localNrlDll)
        (command "._NETLOAD" localNrlDll)
        (princ "\n*** WARNING: Không tìm thấy file NetReload.dll tại C:\\ ***")
      )
      ;; Đếm số lần load
      (if (not *nrl-load-count*) (setq *nrl-load-count* 0))
      (setq *nrl-load-count* (1+ *nrl-load-count*))
      (princ (strcat "\n*** Load thành công (lần " (itoa *nrl-load-count*) ") ***"))
      (princ "\nAvailable commands: NRL, RELOAD")
    )
    (princ "\n*** ERROR: Copy file DLL thất bại! ***")
  )

  ;; === Khôi phục biến hệ thống ===
  (setvar "SECURELOAD" _oldSecure)
  (setvar "NOMUTT" _oldNoMutt)
  (setvar "CMDECHO" _oldCmdEcho)
  (princ)
)


;;; Lệnh CLS để xoá lịch sử dòng lệnh (Command Line)
(defun C:CLS ()
  (textscr)
  (graphscr)
  (princ "\n--- Command Line Cleared ---")
  (princ)
)

;;; Lệnh CLEANNRL để xoá các file DLL tạm cùng thư mục với file gốc
(defun C:CLEANNRL ( / tempDir files f)
  (setq tempDir "Z:\\Z.FORM MAU LAM VIEC\\1. BIM\\2.MAU C3D\\1.LISP\\0.CIVIL TOOL")
  (princ (strcat "\nĐang xoá file tạm NRL_*.dll trong: " tempDir))
  ;; Lấy danh sách file NRL_*.dll và xoá đồng bộ
  (setq files (vl-directory-files tempDir "NRL_*.dll" 1))
  (if files
    (progn
      (foreach f files
        (vl-file-delete (strcat tempDir "\\" f))
      )
      (princ (strcat "\n*** Đã xoá " (itoa (length files)) " file tạm (file đang dùng sẽ bị bỏ qua). ***"))
    )
    (princ "\n*** Không có file tạm NRL_*.dll nào. ***")
  )
  (princ)
)

;;; Load ngay khi file LISP được load lần đầu
(C:LOAD_CT_AI)

(princ "\n*** LoadNetReload.lsp loaded ***")
(princ "\n*** Type LOAD_CT_AI to reload | CLEANNRL to clean | CLS to clear ***")
(princ)

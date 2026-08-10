;;; LoadCivil3D2026.lsp
;;; AutoLISP Script để tự động load file Civil3D2026.dll (hoặc Civil3D_Tools.dll)
;;; Cách sử dụng: Kéo thả file này vào AutoCAD/Civil 3D, hoặc dùng lệnh APPLOAD chọn file này.

(defun C:LOADC3D ()
  (setq dllPath "Y:\\5.SOFT T27\\1. FOR WORK\\1. THIET KE DUONG\\2.CIVIL 3D\\2026\\AutoCAD Civil 3D 2026 Win x64\\x64\\c3d\\Civil3D2026.dll")
  (if (findfile dllPath)
    (progn
      (princ "\n[C3D] Đang nạp Civil3D2026.dll từ ổ Y:...")
      (command "._NETLOAD" dllPath)
      (princ "\n[C3D] Nạp thành công Civil3D2026.dll!")
    )
    (progn
      (setq localPath "C:\\Dropbox\\0.AI AGENT\\6.C#\\Autocad 2026_API\\MyFirstProject\\bin\\Release\\Civil3D_Tools.dll")
      (if (findfile localPath)
        (progn
          (princ "\n[C3D] Đang nạp Civil3D_Tools.dll từ máy cục bộ...")
          (command "._NETLOAD" localPath)
          (princ "\n[C3D] Nạp thành công Civil3D_Tools.dll!")
        )
        (princ "\n[C3D] ❌ Không tìm thấy file DLL phát hành hoặc file cục bộ!")
      )
    )
  )
  (princ)
)

;;; Tự động nạp ngay khi mở bản vẽ (nếu chưa nạp)
(defun s::startup ()
  (C:LOADC3D)
)

(C:LOADC3D)

(princ "\n*** Đã nạp script LoadCivil3D2026.lsp ***")
(princ "\n*** Gõ LOADC3D để nạp thủ công file DLL ***")
(princ)

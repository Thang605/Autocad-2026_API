(defun c:LOAD_NETRELOAD ( / src)
  ;; Đường dẫn file DLL NetReload
  (setq src "c:\\Dropbox\\DATA\\AI Agent\\Autocad 2026_API\\NetReload\\bin\\Debug\\NetReload.dll")

  (command "_SECURELOAD" "0")
  (command "_NETLOAD" src)
  (command "_SECURELOAD" "1")

  (princ "\nNetReload Tool loaded successfully. Type NRL to reload MyFirstProject.")
  (princ)
)
(c:LOAD_NETRELOAD)

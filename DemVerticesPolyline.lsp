;;; ==========================================================================
;;; Tên LISP: DemVerticesPolyline.lsp
;;; Chức năng: Đếm số lượng đỉnh của từng Polyline và tổng số đỉnh của các Polyline được chọn.
;;; Lệnh: DPL hoặc CVP
;;; ==========================================================================

(defun c:DPL ( / ss i ent totalPlines totalVertices plVertices ans pt txtHeight)
  (vl-load-com)
  (princ "\n--- LISP ĐẾM ĐỈNH POLYLINE ---")
  
  ;; 1. Cho phép người dùng chọn các đối tượng Polyline
  (setq ss (ssget '((0 . "POLYLINE,LWPOLYLINE"))))
  
  (if ss
    (progn
      (setq totalPlines (sslength ss)
            totalVertices 0
            i 0
      )
      
      (princ "\n------------------------------------------------")
      (princ (strcat "\nĐã chọn " (itoa totalPlines) " đối tượng Polyline."))
      (princ "\nChi tiết từng Polyline:")
      
      ;; 2. Lặp qua từng đối tượng trong bộ chọn
      (while (< i totalPlines)
        (setq ent (ssname ss i))
        (setq plVertices (get-polyline-vertex-count ent))
        (setq totalVertices (+ totalVertices plVertices))
        (princ (strcat "\n  + Polyline " (itoa (1+ i)) ": " (itoa plVertices) " đỉnh"))
        (setq i (1+ i))
      )
      
      ;; 3. In kết quả tổng hợp ra Command Line
      (princ "\n------------------------------------------------")
      (princ (strcat "\n=> TỔNG SỐ POLYLINE : " (itoa totalPlines)))
      (princ (strcat "\n=> TỔNG SỐ ĐỈNH     : " (itoa totalVertices)))
      (princ "\n------------------------------------------------")
      
      ;; 4. Hỏi người dùng có muốn chèn kết quả vào bản vẽ hay không
      (initget "C K Yes No")
      (setq ans (getkword "\nBạn có muốn chèn text kết quả vào bản vẽ? [Có/Không] <K>: "))
      (if (or (= ans "C") (= ans "Yes") (= ans "c") (= ans "y"))
        (progn
          (setq pt (getpoint "\nChọn điểm chèn text kết quả: "))
          (if pt
            (progn
              ;; Lấy chiều cao chữ mặc định hiện hành
              (setq txtHeight (getvar "TEXTSIZE"))
              ;; Chèn MTEXT chứa kết quả
              (entmake
                (list
                  '(0 . "MTEXT")
                  '(100 . "AcDbEntity")
                  '(100 . "AcDbMText")
                  (cons 10 pt)
                  (cons 40 txtHeight)
                  (cons 1 (strcat "Tong so Polyline: " (itoa totalPlines) "\\PTong so dinh: " (itoa totalVertices)))
                )
              )
              (princ "\nĐã chèn text kết quả thành công!")
            )
          )
        )
      )
    )
    (princ "\nKhông có Polyline nào được chọn.")
  )
  (princ)
)

;;; Định nghĩa thêm lệnh phụ CVP (Count Vertices Polyline) để người dùng dễ nhớ
(defun c:CVP ()
  (c:DPL)
)

;;; ==========================================================================
;;; Hàm hỗ trợ: Lấy số lượng đỉnh của 1 đối tượng Polyline (LWPOLYLINE hoặc POLYLINE)
;;; ==========================================================================
(defun get-polyline-vertex-count (ent / ed type count sub)
  (setq ed (entget ent))
  (setq type (cdr (assoc 0 ed)))
  (cond
    ;; Lightweight Polyline (2D Polyline thông dụng)
    ((= type "LWPOLYLINE")
     (cdr (assoc 90 ed))
    )
    ;; Heavyweight Polyline (2D cũ hoặc 3D Polyline, Mesh)
    ((= type "POLYLINE")
     (setq count 0)
     (setq sub (entnext ent))
     ;; Lặp qua các VERTEX cho đến khi gặp SEQEND
     (while (and sub (/= (cdr (assoc 0 (entget sub))) "SEQEND"))
       (if (= (cdr (assoc 0 (entget sub))) "VERTEX")
         (setq count (1+ count))
       )
       (setq sub (entnext sub))
     )
     count
    )
    (t 0) ; Trường hợp không phải Polyline
  )
)

(princ "\n[LISP loaded] Gõ DPL hoặc CVP để đếm số lượng đỉnh Polyline.")
(princ)

/**
 * AutoCAD Voice Chatbot - Main Application
 * MediaRecorder + Gemini STT + WebSocket + OpenAI API
 */

// ===== CONFIGURATION =====
const CONFIG = {
    wsPort: 8765,
    openaiApiKey: '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',
    openaiModel: 'gpt-4o-mini',
    speechLang: 'vi-VN',
    maxHistory: 100,
};

// ===== STATE =====
let ws = null;
let mediaRecorder = null;
let audioChunks = [];
let isRecording = false;
let recordingStartTime = null;
let commandList = {};
let conversationHistory = [];

// ===== DOM ELEMENTS =====
const $ = (sel) => document.querySelector(sel);
const $$ = (sel) => document.querySelectorAll(sel);

const elements = {
    chatMessages: $('#chatMessages'),
    messageInput: $('#messageInput'),
    sendBtn: $('#sendBtn'),
    micBtn: $('#micBtn'),
    voiceIndicator: $('#voiceIndicator'),
    statusDot: $('#statusDot'),
    statusText: $('#statusText'),
    welcomeScreen: $('#welcomeScreen'),
    clearChat: $('#clearChat'),
    connectBtn: $('#connectBtn'),
    portInput: $('#portInput'),
    sidebar: $('#sidebar'),
    sidebarToggle: $('#sidebarToggle'),
    sidebarOpenBtn: $('#sidebarOpenBtn'),
};

// ===== INITIALIZATION =====
document.addEventListener('DOMContentLoaded', () => {
    initWebSocket();
    initAudioRecording();
    initEventListeners();
    autoResizeTextarea();
});

// ===== WEBSOCKET =====
function initWebSocket() {
    const port = elements.portInput.value || CONFIG.wsPort;
    updateStatus('connecting', 'Đang kết nối...');

    ws = new WebSocket(`ws://localhost:${port}/`);

    ws.onopen = () => {
        updateStatus('connected', 'Đã kết nối AutoCAD');
        addBotMessage('🟢 Đã kết nối thành công với AutoCAD! Hãy nói hoặc gõ lệnh bằng tiếng Việt.', 'success');
    };

    ws.onclose = () => {
        updateStatus('disconnected', 'Mất kết nối');
    };

    ws.onerror = () => {
        updateStatus('disconnected', 'Lỗi kết nối');
        addBotMessage('🔴 Không thể kết nối AutoCAD.\n\n**Hướng dẫn:**\n1. Mở AutoCAD\n2. Gõ lệnh `CHATBOT_START`\n3. Nhấn nút "Kết nối" bên sidebar', 'error');
    };

    ws.onmessage = (event) => {
        try {
            const data = JSON.parse(event.data);
            handleServerMessage(data);
        } catch (e) {
            console.error('Parse error:', e);
        }
    };
}

function handleServerMessage(data) {
    switch (data.type) {
        case 'commands':
            commandList = data.commands || {};
            console.log(`📋 Đã nhận ${Object.keys(commandList).length} lệnh từ AutoCAD`);
            break;
        case 'result':
            if (data.status === 'ok') {
                addBotMessage(data.message, 'success');
            } else {
                addBotMessage(data.message, 'error');
            }
            break;
        case 'pong':
            console.log('Pong received');
            break;
        case 'error':
            addBotMessage(`❌ ${data.message}`, 'error');
            break;
    }
}

function sendToAutoCAD(command) {
    if (!ws || ws.readyState !== WebSocket.OPEN) {
        addBotMessage('⚠️ Chưa kết nối AutoCAD. Vui lòng kết nối trước.', 'error');
        return;
    }
    ws.send(JSON.stringify({ type: 'execute', command }));
}

function updateStatus(state, text) {
    elements.statusDot.className = `status-dot ${state}`;
    elements.statusText.textContent = text;
}

// ===== AUDIO RECORDING + GEMINI SPEECH-TO-TEXT =====

/**
 * Khởi tạo mic - kiểm tra quyền truy cập microphone
 */
async function initAudioRecording() {
    try {
        // Kiểm tra trình duyệt hỗ trợ MediaRecorder
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            elements.micBtn.style.display = 'none';
            console.warn('MediaRecorder API not supported');
            return;
        }
        console.log('🎤 Microphone ready');
    } catch (e) {
        console.error('Audio init error:', e);
        elements.micBtn.style.display = 'none';
    }
}

/**
 * Bắt đầu ghi âm
 */
async function startRecording() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            audio: {
                channelCount: 1,
                sampleRate: 16000,
                echoCancellation: true,
                noiseSuppression: true,
            }
        });

        audioChunks = [];
        mediaRecorder = new MediaRecorder(stream, {
            mimeType: MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
                ? 'audio/webm;codecs=opus'
                : 'audio/webm'
        });

        mediaRecorder.ondataavailable = (event) => {
            if (event.data.size > 0) {
                audioChunks.push(event.data);
            }
        };

        mediaRecorder.onstop = async () => {
            // Dừng tất cả tracks của stream
            stream.getTracks().forEach(t => t.stop());

            if (audioChunks.length === 0) {
                addBotMessage('🎤 Không ghi được âm thanh. Hãy thử lại.', 'error');
                return;
            }

            // Chuyển audio sang Gemini để nhận dạng
            const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
            const durationSec = (Date.now() - recordingStartTime) / 1000;
            elements.messageInput.placeholder = '🔄 Đang nhận dạng giọng nói...';

            addBotMessage(`🎤 Đã ghi ${durationSec.toFixed(1)}s. Đang nhận dạng...`, '');

            try {
                const text = await transcribeWithWhisper(audioBlob);
                if (text && text.trim()) {
                    elements.messageInput.value = text;
                    processUserInput(text);
                } else {
                    addBotMessage('🎤 Không nhận diện được giọng nói. Hãy nói rõ hơn.', 'error');
                }
            } catch (err) {
                addBotMessage(`❌ Lỗi nhận dạng: ${err.message}`, 'error');
            }

            elements.messageInput.placeholder = 'Nhập lệnh hoặc nhấn mic để nói...';
        };

        mediaRecorder.start(250); // Thu chunk mỗi 250ms
        isRecording = true;
        recordingStartTime = Date.now();

        elements.micBtn.classList.add('active');
        elements.voiceIndicator.classList.add('active');
        elements.messageInput.placeholder = '🎤 Đang ghi âm... Nhấn mic để dừng';
        elements.messageInput.value = '';

    } catch (err) {
        console.error('Recording error:', err);
        if (err.name === 'NotAllowedError') {
            addBotMessage('🎤 Cần cấp quyền microphone. Kiểm tra cài đặt trình duyệt.', 'error');
        } else {
            addBotMessage(`🎤 Lỗi ghi âm: ${err.message}`, 'error');
        }
    }
}

/**
 * Dừng ghi âm
 */
function stopRecording() {
    if (mediaRecorder && mediaRecorder.state !== 'inactive') {
        mediaRecorder.stop();
    }
    isRecording = false;
    elements.micBtn.classList.remove('active');
    elements.voiceIndicator.classList.remove('active');
}

/**
 * Toggle ghi âm: nhấn lần 1 = bắt đầu, nhấn lần 2 = dừng & gửi
 */
function toggleRecording() {
    if (isRecording) {
        stopRecording();
    } else {
        startRecording();
    }
}

/**
 * Gửi audio lên OpenAI Whisper API để chuyển thành text (Speech-to-Text)
 */
async function transcribeWithWhisper(audioBlob) {
    const formData = new FormData();
    formData.append('file', audioBlob, 'recording.webm');
    formData.append('model', 'whisper-1');
    formData.append('language', 'vi');

    const response = await fetch('https://api.openai.com/v1/audio/transcriptions', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${CONFIG.openaiApiKey}`,
        },
        body: formData
    });

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(`Whisper API ${response.status}: ${err.error?.message || 'Unknown'}`);
    }

    const data = await response.json();
    return (data.text || '').trim();
}

// ===== OPENAI API - COMMAND INTERPRETER =====

/**
 * Xây dựng danh sách lệnh gồm tên lệnh + mô tả tiếng Việt
 */
function buildCommandContext() {
    const entries = Object.entries(commandList);
    if (entries.length === 0) {
        // Fallback nếu chưa nhận commandList từ server
        return BUILTIN_COMMANDS.map(c => `${c.cmd}: ${c.desc}`).join('\n');
    }
    return entries.map(([cmd, desc]) => `${cmd}: ${desc}`).join('\n');
}

/**
 * Gửi đến OpenAI API để phân tích lệnh
 */
async function interpretCommand(userText) {
    const cmdContext = buildCommandContext();

    const systemPrompt = `Bạn là trợ lý chuyên gia AutoCAD và Civil 3D. Người dùng sẽ nói bằng tiếng Việt, nhiệm vụ của bạn là tạo ra CHUỖI LỆNH AUTOCAD ĐẦY ĐỦ để gửi đến command line của AutoCAD.

DANH SÁCH LỆNH ĐƯỢC HỖ TRỢ:
${cmdContext}

QUY TẮC QUAN TRỌNG:
1. Trả về JSON: {"command": "CHUỖI_LỆNH_ĐẦY_ĐỦ", "description": "Giải thích ngắn gọn bằng tiếng Việt"}
2. "command" PHẢI là chuỗi lệnh hoàn chỉnh có thể paste trực tiếp vào command line AutoCAD
3. Lệnh phải THAY ĐỔI tùy thuộc vào yêu cầu cụ thể của user — KHÔNG chỉ trả tên lệnh cố định
4. Nếu user cung cấp tọa độ, kích thước, tham số → đưa vào chuỗi lệnh
5. Dùng ký tự xuống dòng \\n để ngăn cách các bước nhập trong AutoCAD
6. Nếu không tìm được lệnh phù hợp: {"command": null, "description": "Lý do", "suggestion": "Gợi ý"}

CÁCH TẠO CHUỖI LỆNH:
- AutoCAD đọc lệnh từ command line, mỗi \\n là một lần nhấn Enter
- Tọa độ dùng dấu phẩy: x,y hoặc x,y,z
- Khoảng trống giữa tọa độ = nhấn space/Enter

VÍ DỤ CHUỖI LỆNH ĐỘNG:
| User nói | command |
|----------|---------|
| "vẽ đường thẳng" | "LINE\\n" |
| "vẽ đường thẳng từ 0,0 đến 100,100" | "LINE\\n0,0\\n100,100\\n" |
| "vẽ hình tròn" | "CIRCLE\\n" |
| "vẽ hình tròn bán kính 5" | "CIRCLE\\n0,0\\n5\\n" |
| "vẽ hình tròn tâm 10,20 bán kính 15" | "CIRCLE\\n10,20\\n15\\n" |
| "vẽ hình chữ nhật 100x50" | "RECTANGLE\\n0,0\\n100,50\\n" |
| "vẽ hình chữ nhật từ 10,10 đến 60,40" | "RECTANGLE\\n10,10\\n60,40\\n" |
| "zoom toàn bộ" | "ZOOM\\nE\\n" |
| "zoom tỉ lệ 2" | "ZOOM\\n2x\\n" |
| "offset khoảng cách 5" | "OFFSET\\n5\\n" |
| "fillet bán kính 3" | "FILLET\\nR\\n3\\n" |
| "chamfer khoảng cách 2 và 3" | "CHAMFER\\nD\\n2\\n3\\n" |
| "vẽ đa giác 6 cạnh" | "POLYGON\\n6\\n" |
| "undo 3 lần" | "UNDO\\n3\\n" |
| "lưu bản vẽ" | "QSAVE\\n" |
| "hoàn tác" | "UNDO\\n" |
| "xóa đối tượng" | "ERASE\\n" |
| "xóa tất cả" | "ERASE\\nALL\\n\\n" |
| "sao chép tất cả" | "COPY\\nALL\\n\\n" |
| "di chuyển tất cả" | "MOVE\\nALL\\n\\n" |
| "phát sinh cọc" | "CTS_PhatSinhCoc\\n" |
| "tạo corridor" | "CTC_TaoCorridor_ChoTuyenDuong\\n" |
| "tổng diện tích" | "AT_TongDienTich_Full\\n" |
| "vẽ text nội dung ABC tại 10,20" | "TEXT\\n10,20\\n2.5\\n0\\nABC\\n" |
| "tạo layer tên DuongTim" | "LAYER\\nM\\nDuongTim\\n\\n" |

QUAN TRỌNG - TỪ KHÓA CHỌN ĐỐI TƯỢNG:
- Chọn tất cả → "ALL" (KHÔNG ĐƯỢC viết tắt thành "A")
- Chọn cửa sổ → "W", crossing → "C", cuối → "L", trước → "P"
- Sau khi chọn xong, PHẢI thêm \\n (Enter) để xác nhận
- KHÔNG BAO GIỜ viết tắt từ khóa AutoCAD. Dùng đầy đủ: ALL, CIRCLE, v.v.

LƯU Ý:
- Nếu user không cho tham số → chỉ gửi tên lệnh + \\n
- Nếu user cho đầy đủ tham số → tạo chuỗi lệnh hoàn chỉnh
- Luôn kết thúc chuỗi lệnh bằng \\n
- Sau khi chọn đối tượng, thêm \\n xác nhận`;

    const userMessage = `Người dùng nói: "${userText}"`;

    try {
        const response = await fetch('https://api.openai.com/v1/chat/completions', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${CONFIG.openaiApiKey}`,
            },
            body: JSON.stringify({
                model: CONFIG.openaiModel,
                messages: [
                    { role: 'system', content: systemPrompt },
                    { role: 'user', content: userMessage }
                ],
                temperature: 0.1,
                max_tokens: 200,
                response_format: { type: 'json_object' }
            })
        });

        if (!response.ok) {
            const errData = await response.json().catch(() => ({}));
            throw new Error(`API ${response.status}: ${errData.error?.message || 'Unknown error'}`);
        }

        const data = await response.json();
        const text = data.choices?.[0]?.message?.content || '';

        try {
            return JSON.parse(text);
        } catch {
            const jsonMatch = text.match(/\{[\s\S]*\}/);
            if (jsonMatch) return JSON.parse(jsonMatch[0]);
            throw new Error('Invalid JSON response');
        }
    } catch (error) {
        console.error('OpenAI API error:', error);
        addBotMessage(`⚠️ AI tạm thời không khả dụng (${error.message}). Dùng nhận diện từ khóa.`, '');
        return fallbackInterpret(userText);
    }
}

// ===== FALLBACK KEYWORD MATCHING (offline) =====

/** Danh sách lệnh tĩnh cho fallback + hiển thị sidebar */
const BUILTIN_COMMANDS = [
    // === VẼ ===
    { vi: ['vẽ đường thẳng', 'kẻ đường', 'vẽ line', 'đường thẳng'], cmd: 'LINE', desc: 'Vẽ đường thẳng' },
    { vi: ['vẽ đường cong', 'polyline', 'vẽ polyline', 'vẽ đường'], cmd: 'PLINE', desc: 'Vẽ polyline' },
    { vi: ['vẽ hình tròn', 'vẽ tròn', 'hình tròn'], cmd: 'CIRCLE', desc: 'Vẽ hình tròn' },
    { vi: ['vẽ cung', 'cung tròn'], cmd: 'ARC', desc: 'Vẽ cung tròn' },
    { vi: ['vẽ hình chữ nhật', 'vẽ chữ nhật', 'vẽ ô vuông', 'hình chữ nhật'], cmd: 'RECTANGLE', desc: 'Vẽ hình chữ nhật' },
    { vi: ['vẽ đa giác', 'đa giác'], cmd: 'POLYGON', desc: 'Vẽ đa giác đều' },
    { vi: ['vẽ elip', 'hình elip'], cmd: 'ELLIPSE', desc: 'Vẽ hình elip' },
    { vi: ['vẽ spline', 'đường cong tự do'], cmd: 'SPLINE', desc: 'Vẽ đường cong spline' },
    { vi: ['tô vùng', 'hatch', 'tô hatch'], cmd: 'HATCH', desc: 'Tô hatch vùng kín' },
    { vi: ['vẽ điểm', 'chấm điểm'], cmd: 'POINT', desc: 'Vẽ điểm' },

    // === CHỈNH SỬA ===
    { vi: ['di chuyển', 'dời', 'dời đi', 'dịch chuyển'], cmd: 'MOVE', desc: 'Di chuyển đối tượng' },
    { vi: ['sao chép', 'copy', 'chép', 'nhân bản'], cmd: 'COPY', desc: 'Sao chép đối tượng' },
    { vi: ['xoay', 'quay', 'xoay đối tượng'], cmd: 'ROTATE', desc: 'Xoay đối tượng' },
    { vi: ['thu phóng', 'scale', 'phóng to', 'thu nhỏ', 'thay đổi kích thước'], cmd: 'SCALE', desc: 'Thu phóng đối tượng' },
    { vi: ['đối xứng', 'lật', 'lật gương', 'mirror'], cmd: 'MIRROR', desc: 'Lấy đối xứng' },
    { vi: ['offset', 'dịch song song'], cmd: 'OFFSET', desc: 'Offset đối tượng' },
    { vi: ['cắt', 'cắt bớt', 'trim'], cmd: 'TRIM', desc: 'Cắt bớt đối tượng' },
    { vi: ['kéo dài', 'extend', 'nối dài'], cmd: 'EXTEND', desc: 'Kéo dài đối tượng' },
    { vi: ['bo tròn', 'bo góc', 'fillet'], cmd: 'FILLET', desc: 'Bo tròn góc' },
    { vi: ['vát góc', 'chamfer'], cmd: 'CHAMFER', desc: 'Vát góc' },
    { vi: ['tạo mảng', 'nhân bản hàng loạt', 'array'], cmd: 'ARRAY', desc: 'Tạo mảng đối tượng' },
    { vi: ['kéo giãn', 'stretch'], cmd: 'STRETCH', desc: 'Kéo giãn đối tượng' },
    { vi: ['bẻ gãy', 'break', 'ngắt'], cmd: 'BREAK', desc: 'Bẻ gãy đối tượng' },
    { vi: ['nối', 'nối lại', 'join', 'ghép'], cmd: 'JOIN', desc: 'Nối các đối tượng' },
    { vi: ['phá khối', 'tháo rời', 'explode', 'nổ'], cmd: 'EXPLODE', desc: 'Phá khối đối tượng' },
    { vi: ['xóa', 'xóa đi', 'xóa đối tượng', 'bỏ đi'], cmd: 'ERASE', desc: 'Xóa đối tượng' },
    { vi: ['chỉnh polyline', 'sửa polyline'], cmd: 'PEDIT', desc: 'Chỉnh sửa polyline' },

    // === XEM ===
    { vi: ['zoom tất cả', 'zoom toàn bộ', 'zoom hết', 'xem toàn bộ', 'nhìn hết'], cmd: 'ZOOM E', desc: 'Zoom toàn bộ bản vẽ' },
    { vi: ['zoom', 'phóng to', 'thu nhỏ bản vẽ'], cmd: 'ZOOM', desc: 'Zoom bản vẽ' },
    { vi: ['kéo bản vẽ', 'pan', 'dịch chuyển bản vẽ'], cmd: 'PAN', desc: 'Pan bản vẽ' },
    { vi: ['làm mới', 'vẽ lại', 'regen'], cmd: 'REGEN', desc: 'Làm mới bản vẽ' },

    // === FILE ===
    { vi: ['lưu', 'lưu lại', 'lưu bản vẽ', 'save', 'lưu file'], cmd: 'QSAVE', desc: 'Lưu nhanh bản vẽ' },
    { vi: ['lưu thành', 'lưu mới', 'lưu file mới'], cmd: 'SAVEAS', desc: 'Lưu thành file mới' },
    { vi: ['mở file', 'mở bản vẽ'], cmd: 'OPEN', desc: 'Mở file bản vẽ' },
    { vi: ['tạo mới', 'bản vẽ mới', 'file mới'], cmd: 'NEW', desc: 'Tạo bản vẽ mới' },
    { vi: ['đóng', 'đóng file', 'đóng bản vẽ'], cmd: 'CLOSE', desc: 'Đóng bản vẽ' },
    { vi: ['in', 'in bản vẽ', 'in ấn', 'xuất pdf'], cmd: 'PLOT', desc: 'In bản vẽ' },

    // === HOÀN TÁC ===
    { vi: ['hoàn tác', 'undo', 'quay lại', 'bỏ đi', 'ctrl z'], cmd: 'UNDO', desc: 'Hoàn tác thao tác' },
    { vi: ['làm lại', 'redo'], cmd: 'REDO', desc: 'Làm lại thao tác' },
    { vi: ['khôi phục', 'oops', 'lấy lại'], cmd: 'OOPS', desc: 'Khôi phục đối tượng vừa xóa' },

    // === TEXT & ĐO LƯỜNG ===
    { vi: ['viết chữ', 'tạo text', 'gõ chữ', 'thêm text'], cmd: 'TEXT', desc: 'Tạo text' },
    { vi: ['viết đoạn văn', 'multiline text', 'text nhiều dòng'], cmd: 'MTEXT', desc: 'Tạo text nhiều dòng' },
    { vi: ['đo kích thước', 'ghi kích thước', 'dim'], cmd: 'DIM', desc: 'Ghi kích thước' },
    { vi: ['đo thẳng', 'kích thước ngang', 'kích thước dọc'], cmd: 'DIMLINEAR', desc: 'Đo kích thước thẳng' },
    { vi: ['đo xiên', 'kích thước xiên'], cmd: 'DIMALIGNED', desc: 'Đo kích thước xiên' },
    { vi: ['đo bán kính'], cmd: 'DIMRADIUS', desc: 'Đo bán kính' },
    { vi: ['đo đường kính'], cmd: 'DIMDIAMETER', desc: 'Đo đường kính' },
    { vi: ['đo góc'], cmd: 'DIMANGULAR', desc: 'Đo góc' },
    { vi: ['đo khoảng cách', 'khoảng cách', 'bao xa'], cmd: 'DIST', desc: 'Đo khoảng cách 2 điểm' },
    { vi: ['đo diện tích', 'diện tích bao nhiêu'], cmd: 'AREA', desc: 'Đo diện tích' },
    { vi: ['xem thông tin', 'thông tin đối tượng', 'list'], cmd: 'LIST', desc: 'Xem thông tin đối tượng' },
    { vi: ['tọa độ điểm', 'xem tọa độ'], cmd: 'ID', desc: 'Hiển thị tọa độ điểm' },

    // === LAYER ===
    { vi: ['quản lý layer', 'mở layer', 'layer'], cmd: 'LAYER', desc: 'Quản lý layer' },
    { vi: ['tắt layer', 'ẩn layer'], cmd: 'LAYOFF', desc: 'Tắt layer' },
    { vi: ['bật layer', 'hiện layer'], cmd: 'LAYON', desc: 'Bật tất cả layer' },
    { vi: ['cô lập layer', 'chỉ hiện layer này'], cmd: 'LAYISO', desc: 'Cô lập layer' },

    // === BLOCK ===
    { vi: ['tạo block', 'gộp thành block'], cmd: 'BLOCK', desc: 'Tạo block' },
    { vi: ['chèn block', 'thêm block'], cmd: 'INSERT', desc: 'Chèn block' },
    { vi: ['xuất block', 'write block'], cmd: 'WBLOCK', desc: 'Xuất block ra file' },

    // === THUỘC TÍNH ===
    { vi: ['mở properties', 'xem thuộc tính', 'thuộc tính'], cmd: 'PROPERTIES', desc: 'Mở bảng thuộc tính' },
    { vi: ['sao chép thuộc tính', 'copy thuộc tính'], cmd: 'MATCHPROP', desc: 'Sao chép thuộc tính' },

    // === 3D ===
    { vi: ['đùn', 'extrude', 'đùn 3d'], cmd: 'EXTRUDE', desc: 'Đùn 3D' },
    { vi: ['xoay tròn 3d', 'revolve'], cmd: 'REVOLVE', desc: 'Xoay tròn tạo khối 3D' },
    { vi: ['hợp nhất', 'union', 'gộp solid'], cmd: 'UNION', desc: 'Hợp nhất solid' },
    { vi: ['trừ solid', 'subtract', 'khoét'], cmd: 'SUBTRACT', desc: 'Trừ solid' },

    // === TIỆN ÍCH ===
    { vi: ['dọn dẹp', 'dọn bản vẽ', 'purge'], cmd: 'PURGE', desc: 'Dọn dẹp bản vẽ' },
    { vi: ['kiểm tra', 'audit'], cmd: 'AUDIT', desc: 'Kiểm tra lỗi bản vẽ' },

    // === ACAD TOOL (PLUGIN) ===
    { vi: ['tổng diện tích', 'tính tổng diện tích', 'cộng diện tích'], cmd: 'AT_TongDienTich_Full', desc: 'Tính tổng diện tích' },
    { vi: ['tổng độ dài', 'tính tổng độ dài', 'cộng độ dài'], cmd: 'AT_TongDoDai_Full', desc: 'Tính tổng độ dài' },
    { vi: ['offset 2 bên', 'offset hai bên'], cmd: 'AT_Offset_2Ben', desc: 'Offset 2 bên cùng lúc' },
    { vi: ['đánh số thứ tự', 'đánh số'], cmd: 'AT_DanhSoThuTu', desc: 'Đánh số thứ tự' },
    { vi: ['liên kết text', 'link text'], cmd: 'AT_TextLink', desc: 'Liên kết nội dung text' },
    { vi: ['tạo outline', 'đường bao'], cmd: 'AT_TaoOutline', desc: 'Tạo đường bao' },
    { vi: ['copy nội dung text', 'chép nội dung'], cmd: 'CT_Copy_NoiDung_Text', desc: 'Copy nội dung text' },
    { vi: ['đo độ dốc', 'tính độ dốc'], cmd: 'AT_DoDoc', desc: 'Đo độ dốc' },
    { vi: ['xref tất cả', 'xref all'], cmd: 'AT_XrefAll', desc: 'Xref tất cả file' },
    { vi: ['xoay theo viewport', 'xoay viewport'], cmd: 'AT_XoayDoiTuong_TheoViewport', desc: 'Xoay đối tượng theo viewport' },
    { vi: ['bố trí viewport', 'tạo viewport'], cmd: 'AT_BoTri_ViewPort_TheoHinh', desc: 'Bố trí viewport theo hình' },
    { vi: ['in hàng loạt', 'in model hàng loạt'], cmd: 'AT_InModel_HangLoat', desc: 'In model hàng loạt' },
    { vi: ['in theo block', 'in bản vẽ block'], cmd: 'AT_InBanVe_TheoBlock', desc: 'In bản vẽ theo block' },
    { vi: ['dim đường cong'], cmd: 'AT_DIM_DUONGCONG', desc: 'Dim đường cong' },
    { vi: ['xuất bảng excel', 'xuất sang excel'], cmd: 'AT_XuatBang_SangExcel', desc: 'Xuất bảng sang Excel' },
    { vi: ['cập nhật layout'], cmd: 'AT_UpdateLayout', desc: 'Cập nhật tất cả layout' },
    { vi: ['xuất tọa độ polyline'], cmd: 'XUATBANG_ToaDoPolyline', desc: 'Xuất bảng tọa độ polyline' },
    { vi: ['tạo solid từ text', 'text thành solid'], cmd: 'AT_TextToSolid', desc: 'Chuyển text thành solid' },
    { vi: ['tạo block từng cái', 'block từng đối tượng'], cmd: 'AT_TAOBLOCK_TUNGDOITUONG', desc: 'Tạo block từng đối tượng' },
    { vi: ['đánh số block'], cmd: 'AT_DanhSoThuTu_ChoBlock', desc: 'Đánh số thứ tự cho block' },

    // === CIVIL 3D - CORRIDOR ===
    { vi: ['tạo corridor', 'tạo hành lang', 'corridor'], cmd: 'CTC_TaoCorridor_ChoTuyenDuong', desc: 'Tạo corridor cho tuyến đường' },
    { vi: ['điều chỉnh phân đoạn', 'chỉnh region corridor'], cmd: 'CTC_DieuChinh_PhanDoan', desc: 'Điều chỉnh phân đoạn corridor' },
    { vi: ['thêm section corridor', 'add section corridor'], cmd: 'CTC_AddAllSection', desc: 'Thêm tất cả section vào corridor' },
    { vi: ['set target corridor'], cmd: 'CTPI_Corridor_SetTargets', desc: 'Thiết lập targets cho corridor' },
    { vi: ['tạo corridor surface', 'bề mặt corridor'], cmd: 'CTSV_TaoCorridorSurface', desc: 'Tạo corridor surface' },

    // === CIVIL 3D - CỌC (SAMPLE LINE) ===
    { vi: ['phát sinh cọc', 'tạo cọc', 'sinh cọc'], cmd: 'CTS_PhatSinhCoc', desc: 'Phát sinh cọc tự động' },
    { vi: ['phát sinh cọc theo bảng'], cmd: 'CTS_PhatSinhCoc_TheoBang', desc: 'Phát sinh cọc theo bảng' },
    { vi: ['phát sinh cọc thủ công'], cmd: 'CTS_PhatSinhCoc_ThuCong', desc: 'Phát sinh cọc thủ công' },
    { vi: ['phát sinh cọc từ điểm', 'cọc từ cogopoint'], cmd: 'CTS_PhatSinhCoc_TuCogoPoint', desc: 'Phát sinh cọc từ CogoPoint' },
    { vi: ['đổi tên cọc', 'đặt tên cọc'], cmd: 'CTS_DoiTenCoc', desc: 'Đổi tên cọc' },
    { vi: ['dịch cọc', 'dời cọc'], cmd: 'CTS_DichCoc_TinhTien', desc: 'Dịch cọc tịnh tiến' },
    { vi: ['copy nhóm cọc', 'sao chép nhóm cọc'], cmd: 'CTS_Copy_NhomCoc', desc: 'Copy nhóm cọc' },
    { vi: ['đồng bộ cọc', 'đồng bộ 2 nhóm cọc'], cmd: 'CTS_DongBo_2_NhomCoc', desc: 'Đồng bộ 2 nhóm cọc' },
    { vi: ['chèn cọc trắc dọc', 'thêm cọc trắc dọc'], cmd: 'CTS_ChenCoc_TrenTracDoc', desc: 'Chèn cọc trên trắc dọc' },
    { vi: ['chèn cọc trắc ngang', 'thêm cọc trắc ngang'], cmd: 'CTS_CHENCOC_TRENTRACNGANG', desc: 'Chèn cọc trên trắc ngang' },
    { vi: ['bảng tọa độ cọc', 'xuất tọa độ cọc'], cmd: 'CTS_TaoBang_ToaDoCoc', desc: 'Tạo bảng tọa độ cọc' },
    { vi: ['thay đổi bề rộng cọc', 'bề rộng sample line'], cmd: 'CTS_ThayDoi_BeRong_Sampleline', desc: 'Thay đổi bề rộng sample line' },
    { vi: ['hiệu chỉnh khoảng cách cọc'], cmd: 'CTS_HieuChinh_KhoangCachCoc', desc: 'Hiệu chỉnh khoảng cách cọc' },

    // === CIVIL 3D - TRẮC DỌC ===
    { vi: ['vẽ trắc dọc', 'trắc dọc tự nhiên'], cmd: 'CTP_VeTracDoc_TuNhien', desc: 'Vẽ trắc dọc tự nhiên' },
    { vi: ['trắc dọc tất cả tuyến', 'vẽ trắc dọc tất cả'], cmd: 'CTP_VeTracDoc_TuNhien_TatCaTuyen', desc: 'Vẽ trắc dọc tất cả tuyến' },
    { vi: ['fix đường tự nhiên', 'sửa đường tự nhiên'], cmd: 'CTP_Fix_DuongTuNhien_TheoCoc', desc: 'Fix đường tự nhiên theo cọc' },
    { vi: ['nhãn nút giao', 'gán nhãn nút giao'], cmd: 'CTP_GanNhanNutGiao_LenTracDoc', desc: 'Gán nhãn nút giao lên trắc dọc' },
    { vi: ['thay đổi profile band'], cmd: 'CTP_ThayDoi_profile_Band', desc: 'Thay đổi profile band' },
    { vi: ['polyline thành profile', 'chuyển polyline profile'], cmd: 'CTP_Polyline_To_Profile', desc: 'Chuyển polyline thành profile' },

    // === CIVIL 3D - TRẮC NGANG ===
    { vi: ['vẽ trắc ngang', 'trắc ngang thiết kế'], cmd: 'CTSV_VeTracNgangThietKe', desc: 'Vẽ trắc ngang thiết kế' },
    { vi: ['đánh cấp', 'đánh cấp taluy'], cmd: 'CTSV_DanhCap', desc: 'Đánh cấp taluy' },
    { vi: ['cập nhật đánh cấp'], cmd: 'CTSV_DanhCap_CapNhat', desc: 'Cập nhật đánh cấp' },
    { vi: ['hiệu chỉnh section', 'sửa cắt ngang'], cmd: 'CTSV_HieuChinh_Section', desc: 'Hiệu chỉnh section' },
    { vi: ['hiệu chỉnh section động', 'sửa section dynamic'], cmd: 'CTSV_HieuChinh_Section_Dynamic', desc: 'Hiệu chỉnh section (dynamic)' },
    { vi: ['thay đổi giới hạn', 'giới hạn trái phải'], cmd: 'CTSV_ThayDoi_GioiHan_traiPhai', desc: 'Thay đổi giới hạn trái/phải' },
    { vi: ['khung in', 'thay đổi khung in'], cmd: 'CTSV_ThayDoi_KhungIn', desc: 'Thay đổi khung in section view' },
    { vi: ['fit khung in'], cmd: 'CTSV_fit_KhungIn', desc: 'Fit khung in tự động' },
    { vi: ['khối lượng cắt ngang', 'tính khối lượng'], cmd: 'CTSV_KhoiLuongCatNgang', desc: 'Tính khối lượng cắt ngang' },
    { vi: ['xuất khối lượng', 'xuất khối lượng excel'], cmd: 'CTSV_XuatKhoiLuongRaExcel', desc: 'Xuất khối lượng ra Excel' },
    { vi: ['thêm bảng khối lượng'], cmd: 'CTSV_Them_BangKL_CatNgang', desc: 'Thêm bảng khối lượng cắt ngang' },
    { vi: ['điều chỉnh đường tự nhiên'], cmd: 'CTSV_DieuChinh_DuongTuNhien', desc: 'Điều chỉnh đường tự nhiên' },
    { vi: ['thêm vật liệu cắt ngang'], cmd: 'CTSV_ThemVatLieu_TrenCatNgang', desc: 'Thêm vật liệu trên cắt ngang' },
    { vi: ['ẩn đường địa chất'], cmd: 'CTSV_An_DuongDiaChat', desc: 'Ẩn đường địa chất' },

    // === CIVIL 3D - ĐIỂM ===
    { vi: ['tạo điểm', 'tạo cogopoint', 'thêm điểm'], cmd: 'CTPO_TaoCogoPoint_CaoDo_FromSurface', desc: 'Tạo CogoPoint cao độ từ Surface' },
    { vi: ['tạo điểm từ text'], cmd: 'CTPO_CreateCogopointFromText', desc: 'Tạo CogoPoint từ Text' },
    { vi: ['tạo điểm từ excel'], cmd: 'CTPO_TaoCogoPoint_FromExcel', desc: 'Tạo CogoPoint từ Excel' },
    { vi: ['đổi tên điểm', 'đổi tên cogopoint'], cmd: 'CTPO_DoiTen_Cogopoint', desc: 'Đổi tên CogoPoint' },
    { vi: ['ẩn điểm', 'ẩn cogopoint'], cmd: 'CTPO_An_CogoPoint', desc: 'Ẩn CogoPoint' },
    { vi: ['cập nhật nhóm điểm'], cmd: 'CTPO_UpdateAllPointGroup', desc: 'Update tất cả Point Group' },

    // === CIVIL 3D - CỐNG ===
    { vi: ['thay đổi đường kính cống'], cmd: 'CTPI_ThayDoi_DuongKinhCong', desc: 'Thay đổi đường kính cống' },
    { vi: ['thay đổi độ dốc cống'], cmd: 'CTPI_ThayDoi_DoanDocCong', desc: 'Thay đổi độ dốc cống' },
    { vi: ['cao độ đáy cống', 'thay đổi cao độ cống'], cmd: 'CTPi_ThayDoi_CaoDo_DayCong', desc: 'Thay đổi cao độ đáy cống' },
    { vi: ['xoay hố thu'], cmd: 'CTPI_XoayHoThu_Theo2diem', desc: 'Xoay hố thu theo 2 điểm' },

    // === CIVIL 3D - BỀ MẶT ===
    { vi: ['cao độ mặt phẳng', 'cao độ surface'], cmd: 'CTSU_CaoDoMatPhang_TaiCogopoint', desc: 'Cao độ mặt phẳng tại CogoPoint' },
    { vi: ['offset tuyến', 'offset alignment'], cmd: 'AT_OffsetAlignment', desc: 'Offset alignment' },
    { vi: ['thống kê tuyến đường', 'bảng thống kê tuyến'], cmd: 'CTA_BangThongKeCacTuyenDuong', desc: 'Bảng thống kê các tuyến đường' },

    // === CIVIL 3D - PARCEL ===
    { vi: ['tạo thửa đất', 'tạo parcel'], cmd: 'CTPA_TaoParcel_CacLoaiNha', desc: 'Tạo parcel các loại nhà' },
    { vi: ['đổi tên thửa đất', 'đổi tên parcel'], cmd: 'CTPA_DoiTen_Parcel', desc: 'Đổi tên parcel' },

    // === CIVIL 3D - CHUNG ===
    { vi: ['thông tin civil 3d', 'xem thông tin đối tượng'], cmd: 'CT_ThongTinDoiTuong', desc: 'Thông tin đối tượng Civil 3D' },
    { vi: ['polyline từ section'], cmd: 'AT_PolylineFromSection', desc: 'Tạo polyline từ section' },
    { vi: ['material list', 'thêm vật liệu'], cmd: 'CTS_Them_MaterialList', desc: 'Thêm material list' },

    // === MENU & TRỢ GIÚP ===
    { vi: ['mở menu', 'hiện menu', 'menu'], cmd: 'SHOW_MENU', desc: 'Hiển thị menu' },
    { vi: ['danh sách lệnh', 'xem lệnh', 'trợ giúp', 'help'], cmd: 'AT_HelpList', desc: 'Danh sách tất cả lệnh' },
    { vi: ['tìm lệnh', 'tìm kiếm lệnh'], cmd: 'AT_HelpSearch', desc: 'Tìm kiếm lệnh' },
    { vi: ['quản lý phím tắt', 'phím tắt'], cmd: 'SHORTCUT_MANAGER', desc: 'Quản lý phím tắt' },
];

function fallbackInterpret(text) {
    const normalized = text.toLowerCase().trim();

    // 1. Direct command match (user gõ tên lệnh trực tiếp)
    for (const cmd of Object.keys(commandList)) {
        if (normalized === cmd.toLowerCase()) {
            return { command: cmd, description: commandList[cmd] };
        }
    }

    // 2. Vietnamese keyword matching
    for (const entry of BUILTIN_COMMANDS) {
        for (const pattern of entry.vi) {
            if (normalized.includes(pattern)) {
                return { command: entry.cmd, description: entry.desc };
            }
        }
    }

    // 3. Fuzzy: match description in command list
    for (const [cmd, desc] of Object.entries(commandList)) {
        if (desc && normalized.includes(desc.toLowerCase())) {
            return { command: cmd, description: desc };
        }
    }

    return {
        command: null,
        description: 'Không nhận diện được lệnh',
        suggestion: 'Hãy thử nói rõ hơn, ví dụ: "vẽ đường thẳng", "xóa đối tượng", "lưu bản vẽ"...'
    };
}

// ===== PROCESS USER INPUT =====
async function processUserInput(text) {
    if (!text.trim()) return;

    // Hide welcome screen
    if (elements.welcomeScreen) {
        elements.welcomeScreen.style.display = 'none';
    }

    addUserMessage(text);
    elements.messageInput.value = '';
    autoResizeTextarea();

    const typingEl = showTypingIndicator();

    // Check if user typed a direct command name (ALL CAPS or known prefix)
    const trimmed = text.trim();
    const isDirectCommand = commandList[trimmed] ||
        commandList[trimmed.toUpperCase()] ||
        /^[A-Z_]{2,}(\s|$)/.test(trimmed);

    let result;

    if (isDirectCommand) {
        const cmd = commandList[trimmed] ? trimmed : trimmed.toUpperCase();
        result = { command: cmd, description: commandList[cmd] || 'Lệnh AutoCAD' };
    } else {
        // Send to OpenAI for interpretation
        result = await interpretCommand(trimmed);
    }

    typingEl.remove();

    if (result.command) {
        sendToAutoCAD(result.command);
        // Hiển thị lệnh đẹp: thay \n thành dấu mũi tên
        const displayCmd = result.command.replace(/\\n/g, ' ').replace(/\n/g, ' ').trim();
        addBotMessage(
            `🎯 **${result.description}**\n\nĐã gửi lệnh đến AutoCAD:`,
            'success',
            displayCmd
        );
    } else {
        addBotMessage(
            `🤔 ${result.description}\n\n${result.suggestion || 'Thử nói: "vẽ đường thẳng", "zoom toàn bộ", "tạo corridor"...'}`,
            'error'
        );
    }
}

// ===== UI HELPERS =====
function addUserMessage(text) {
    const html = `
        <div class="message user">
            <div class="message-avatar">👤</div>
            <div class="message-content">${escapeHtml(text)}</div>
        </div>
    `;
    appendMessage(html);
}

function addBotMessage(text, type = '', command = '') {
    const formattedText = formatMarkdown(text);
    const commandHtml = command ? `<div class="message-command">${escapeHtml(command)}</div>` : '';
    const html = `
        <div class="message bot ${type}">
            <div class="message-avatar">🤖</div>
            <div class="message-content">
                ${formattedText}
                ${commandHtml}
            </div>
        </div>
    `;
    appendMessage(html);
}

function appendMessage(html) {
    if (elements.welcomeScreen) elements.welcomeScreen.style.display = 'none';
    elements.chatMessages.insertAdjacentHTML('beforeend', html);
    scrollToBottom();
}

function showTypingIndicator() {
    const html = `
        <div class="message bot typing-msg">
            <div class="message-avatar">🤖</div>
            <div class="message-content">
                <div class="typing-indicator">
                    <span></span><span></span><span></span>
                </div>
            </div>
        </div>
    `;
    elements.chatMessages.insertAdjacentHTML('beforeend', html);
    scrollToBottom();
    return elements.chatMessages.querySelector('.typing-msg:last-child');
}

function scrollToBottom() {
    requestAnimationFrame(() => {
        elements.chatMessages.scrollTop = elements.chatMessages.scrollHeight;
    });
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function formatMarkdown(text) {
    return text
        .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
        .replace(/`(.*?)`/g, '<code>$1</code>')
        .replace(/\n/g, '<br>');
}

// ===== EVENT LISTENERS =====
function initEventListeners() {
    elements.sendBtn.addEventListener('click', () => {
        processUserInput(elements.messageInput.value);
    });

    elements.messageInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            processUserInput(elements.messageInput.value);
        }
    });

    elements.micBtn.addEventListener('click', () => {
        toggleRecording();
    });

    // Click vào voice indicator overlay cũng dừng ghi âm
    elements.voiceIndicator.addEventListener('click', () => {
        if (isRecording) stopRecording();
    });

    document.addEventListener('keydown', (e) => {
        if (e.ctrlKey && e.key === 'm') {
            e.preventDefault();
            toggleRecording();
        }
    });

    elements.clearChat.addEventListener('click', () => {
        elements.chatMessages.innerHTML = '';
        if (elements.welcomeScreen) {
            elements.chatMessages.appendChild(elements.welcomeScreen);
            elements.welcomeScreen.style.display = '';
        }
    });

    elements.connectBtn.addEventListener('click', () => {
        if (ws) ws.close();
        CONFIG.wsPort = elements.portInput.value;
        initWebSocket();
    });

    document.querySelectorAll('.cmd-chip').forEach(chip => {
        chip.addEventListener('click', () => {
            const cmd = chip.dataset.cmd;
            if (cmd) processUserInput(cmd);
        });
    });

    document.querySelectorAll('.example-card').forEach(card => {
        card.addEventListener('click', () => {
            const text = card.dataset.text;
            if (text) processUserInput(text);
        });
    });

    elements.sidebarToggle.addEventListener('click', () => {
        elements.sidebar.classList.add('collapsed');
    });

    elements.sidebarOpenBtn.addEventListener('click', () => {
        elements.sidebar.classList.remove('collapsed');
    });
}

function autoResizeTextarea() {
    const ta = elements.messageInput;
    ta.addEventListener('input', () => {
        ta.style.height = 'auto';
        ta.style.height = Math.min(ta.scrollHeight, 120) + 'px';
    });
}

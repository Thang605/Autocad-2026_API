// Lệnh AutoCAD để điều khiển Chatbot Server
// CHATBOT_START, CHATBOT_STOP, CHATBOT_STATUS

using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using MyFirstProject.Extensions;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(Civil3DCsharp.Chatbot.ChatbotCommand))]

namespace Civil3DCsharp.Chatbot
{
    public class ChatbotCommand
    {
        [CommandMethod("CHATBOT_START")]
        public static void ChatbotStart()
        {
            try
            {
                A.Ed.WriteMessage("\n🚀 Đang khởi động Chatbot Server...");
                _ = ChatbotServer.Instance.StartAsync();
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n❌ Lỗi khởi động chatbot: {ex.Message}");
            }
        }

        [CommandMethod("CHATBOT_STOP")]
        public static void ChatbotStop()
        {
            ChatbotServer.Instance.Stop();
        }

        [CommandMethod("CHATBOT_STATUS")]
        public static void ChatbotStatus()
        {
            var server = ChatbotServer.Instance;
            if (server.IsRunning)
            {
                A.Ed.WriteMessage($"\n✅ Chatbot Server đang chạy trên port {server.Port}");
                A.Ed.WriteMessage($"\n🌐 WebUI: http://localhost:{server.Port}/");
                A.Ed.WriteMessage($"\n🔌 WebSocket: ws://localhost:{server.Port}/");
            }
            else
            {
                A.Ed.WriteMessage("\n⚫ Chatbot Server chưa chạy. Gõ CHATBOT_START để khởi động.");
            }
        }

        [CommandMethod("CHATBOT_OPEN")]
        public static void ChatbotOpen()
        {
            var server = ChatbotServer.Instance;
            if (!server.IsRunning)
            {
                A.Ed.WriteMessage("\n⚫ Server chưa chạy. Đang khởi động...");
                _ = server.StartAsync();
            }

            // Mở trình duyệt
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"http://localhost:{server.Port}/",
                    UseShellExecute = true
                });
                A.Ed.WriteMessage($"\n🌐 Đã mở chatbot trong trình duyệt!");
            }
            catch (System.Exception ex)
            {
                A.Ed.WriteMessage($"\n❌ Không thể mở trình duyệt: {ex.Message}");
                A.Ed.WriteMessage($"\n💡 Mở thủ công: http://localhost:{server.Port}/");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Windows.Input;
using Autodesk.Windows;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyFirstProject
{
    /// <summary>
    /// Engine tạo Ribbon Tab/Panel/Button từ JSON/Excel definitions
    /// </summary>
    public static class RibbonLoader
    {
        private const string ID_PREFIX = "C3DTools_";

        // Giữ reference để không bị GC
        private static readonly List<RibbonCommandHandler> _handlers = new();

        // ═══════════════════════════════════════════════════════════════
        //  TẠO RIBBON TAB
        // ═══════════════════════════════════════════════════════════════

        public static int CreateRibbonTab(RibbonTabDef tabDef)
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon == null) return 0;

            string uniqueSuffix = Guid.NewGuid().ToString("N");
            string tabId = ID_PREFIX + tabDef.Tab.Replace(" ", "_") + "_" + uniqueSuffix;
            RemoveExistingTab(ribbon, tabId);

            var tab = new RibbonTab
            {
                Title = tabDef.Tab,
                Id = tabId,
                Name = tabId
            };

            int commandCount = 0;
            foreach (var panelDef in tabDef.Panels)
            {
                var panel = CreatePanel(panelDef, tabId, ref commandCount);
                if (panel != null)
                    tab.Panels.Add(panel);
            }

            ribbon.Tabs.Add(tab);
            tab.IsActive = false;

            return commandCount;
        }

        public static void RemoveAllCustomTabs()
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon == null) return;

            var toRemove = new List<RibbonTab>();
            foreach (var tab in ribbon.Tabs)
            {
                if (tab.Id != null && tab.Id.StartsWith(ID_PREFIX))
                    toRemove.Add(tab);
            }
            foreach (var tab in toRemove)
                ribbon.Tabs.Remove(tab);

            // Xóa handlers cũ
            _handlers.Clear();
        }

        // ═══════════════════════════════════════════════════════════════
        //  TẠO PANEL
        // ═══════════════════════════════════════════════════════════════

        private static RibbonPanel CreatePanel(RibbonPanelDef panelDef, string tabId, ref int commandCount)
        {
            var panelSource = new RibbonPanelSource
            {
                Title = panelDef.Name,
                Id = $"{tabId}_{panelDef.Name.Replace(" ", "_")}_{Guid.NewGuid():N}"
            };

            foreach (var itemDef in panelDef.Items)
            {
                var ribbonItem = CreateRibbonItem(itemDef, panelSource.Id, ref commandCount);
                if (ribbonItem != null)
                    panelSource.Items.Add(ribbonItem);
            }

            var panel = new RibbonPanel();
            panel.Source = panelSource;
            return panel;
        }

        // ═══════════════════════════════════════════════════════════════
        //  TẠO RIBBON ITEM
        // ═══════════════════════════════════════════════════════════════

        private static RibbonItem CreateRibbonItem(RibbonItemDef itemDef, string parentId, ref int commandCount)
        {
            switch (itemDef.Type?.ToLower())
            {
                case "separator":
                    return new RibbonSeparator();
                case "row":
                    return CreateRowPanel(itemDef, parentId, ref commandCount);
                case "split":
                    return CreateSplitButton(itemDef, parentId, ref commandCount);
                case "button":
                default:
                    return CreateButton(itemDef, ref commandCount);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  CÁC LOẠI BUTTON
        // ═══════════════════════════════════════════════════════════════

        private static RibbonButton CreateButton(RibbonItemDef itemDef, ref int commandCount)
        {
            // Mỗi button có handler RIÊNG với lệnh baked-in
            var handler = new RibbonCommandHandler(itemDef.Command);
            _handlers.Add(handler); // Giữ reference

            var btn = new RibbonButton();
            btn.Text = itemDef.Label;
            btn.ShowText = true;
            btn.ShowImage = false;
            btn.Size = GetSize(itemDef.Size);
            btn.Orientation = GetOrientation(itemDef.Size);
            btn.Id = $"{ID_PREFIX}btn_{itemDef.Command}_{commandCount}_{Guid.NewGuid():N}";
            btn.CommandParameter = itemDef.Command;
            btn.CommandHandler = handler;

            if (!string.IsNullOrEmpty(itemDef.Description))
                btn.ToolTip = itemDef.Description;

            commandCount++;
            return btn;
        }

        private static RibbonSplitButton CreateSplitButton(RibbonItemDef itemDef, string parentId, ref int commandCount)
        {
            var split = new RibbonSplitButton
            {
                Text = itemDef.Label,
                ShowText = true,
                ShowImage = false,
                Size = GetSize(itemDef.Size),
                Id = $"{parentId}_split_{itemDef.Label.Replace(" ", "_")}_{Guid.NewGuid():N}",
                IsSplit = true
            };

            if (!string.IsNullOrEmpty(itemDef.Command))
            {
                var mainBtn = CreateButton(itemDef, ref commandCount);
                split.Items.Add(mainBtn);
            }

            if (itemDef.Items != null)
            {
                foreach (var subItem in itemDef.Items)
                {
                    var subBtn = CreateButton(subItem, ref commandCount);
                    split.Items.Add(subBtn);
                }
            }

            if (split.Items.Count > 0)
                split.Current = split.Items[0] as RibbonButton;

            return split;
        }

        private static RibbonRowPanel CreateRowPanel(RibbonItemDef itemDef, string parentId, ref int commandCount)
        {
            var rowPanel = new RibbonRowPanel();

            int itemIndex = 0;
            if (itemDef.Items != null)
            {
                foreach (var subItem in itemDef.Items)
                {
                    var subBtn = CreateButton(subItem, ref commandCount);
                    subBtn.Size = RibbonItemSize.Standard;
                    subBtn.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    rowPanel.Items.Add(subBtn);

                    itemIndex++;
                    if (itemIndex < itemDef.Items.Count)
                        rowPanel.Items.Add(new RibbonRowBreak());
                }
            }

            return rowPanel;
        }

        // ═══════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static RibbonItemSize GetSize(string size) =>
            size?.ToLower() == "large"
                ? RibbonItemSize.Large
                : RibbonItemSize.Standard;

        private static System.Windows.Controls.Orientation GetOrientation(string size) =>
            size?.ToLower() == "large"
                ? System.Windows.Controls.Orientation.Vertical
                : System.Windows.Controls.Orientation.Horizontal;

        private static void RemoveExistingTab(RibbonControl ribbon, string tabId)
        {
            RibbonTab existing = null;
            foreach (var tab in ribbon.Tabs)
            {
                if (tab.Id == tabId) { existing = tab; break; }
            }
            if (existing != null)
                ribbon.Tabs.Remove(existing);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  COMMAND HANDLER — mỗi button 1 instance
    // ═══════════════════════════════════════════════════════════════

    public class RibbonCommandHandler : ICommand
    {
        private readonly string _command;

        public RibbonCommandHandler(string command)
        {
            _command = command ?? "";
        }

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            string cmd = (parameter as string) ?? _command;
            if (string.IsNullOrEmpty(cmd)) return;

            try
            {
                // Cách 1: COM SendCommand (đáng tin cậy nhất)
                dynamic acadApp = AcadApp.AcadApplication;
                acadApp.ActiveDocument.SendCommand(cmd + "\n");
            }
            catch
            {
                try
                {
                    // Cách 2: Managed API
                    var doc = AcadApp.DocumentManager.MdiActiveDocument;
                    doc?.SendStringToExecute(cmd + "\n", true, false, true);
                }
                catch { }
            }
        }
    }

}

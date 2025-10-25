using HotelMgt.Services;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient; // Use Microsoft.Data.SqlClient only
using HotelMgt.Utilities;
using HotelMgt.UserControls.Employee;
using HotelMgt.UserControls.Admin;
using System.Drawing.Drawing2D;
using HotelMgt.Custom;          // RoundedPanel
using System.IO;
using System.Drawing.Text;
using System.Collections.Generic; // + for filter datasource

namespace HotelMgt.Forms
{
    public partial class AdminDashboardForm : Form
    {
        private readonly AuthenticationService _authService;
        private readonly DatabaseService _dbService;

        // Overview stats labels
        private Label lblAvailableRooms = null!;
        private Label lblOccupiedRooms = null!;
        private Label lblReservedRooms = null!;
        private Label lblActiveCheckIns = null!;

        // Header logo elements
        private RoundedPanel? _headerLogoPanel;
        private PictureBox? _headerLogoPictureBox;

        // Admin overview grids
        private DataGridView dgvCurrentOccupancy = null!;

        private Image? _employeeLogoImage; // cache
        private bool _suppressLogoutPrompt;
        private ImageList? _tabImages; // icons (optional)
        private Panel? _tabHost;

        // NEW: Activity Logs controls (Overview)
        private RoundedPanel? pnlActivityLogs;
        private DateTimePicker dtpLogDate = null!;
        private ComboBox cboLogEmployee = null!;
        private ComboBox cboLogType = null!;
        private Label lblLogSummary = null!;
        private DataGridView dgvActivityLogs = null!;
        private Label lblActivityLogsEmpty = null!; // Add this field near other Activity Logs controls

        public AdminDashboardForm()
        {
            InitializeComponent();

            _authService = new AuthenticationService();
            _dbService = new DatabaseService();

            panelHeader.Resize += PanelHeader_Resize;
            tabControl.SelectedIndexChanged += tabControl_SelectedIndexChanged;
            btnLogout.Click += btnLogout_Click;

            EnsureHeaderLogo();
            LoadUserControls();

            // Left gutter for tab bar + softer pages
            EmbedTabControlInHost(tabControl, leftPadding: 20);
            StyleTabControl(tabControl, isAdmin: true);
            SoftenTabPages(tabControl);
        }

        private void AdminDashboardForm_Load(object sender, EventArgs e)
        {
            this.Text = $"Hotel Management - {CurrentUser.FullName} ({CurrentUser.Role})";
            lblWelcome.Text = $"Welcome, {CurrentUser.FullName}";
            lblRole.Text = CurrentUser.Role?.ToUpper() ?? string.Empty;

            lblRole.AutoSize = true;
            lblRole.BorderStyle = BorderStyle.None;
            lblRole.Padding = new Padding(10, 4, 10, 4);
            ApplyRoundedRegion(lblRole, 10);
            lblRole.SizeChanged -= (_, __) => { };
            lblRole.SizeChanged += (_, __) => ApplyRoundedRegion(lblRole, 10);

            LayoutHeaderStack();
            AlignHeaderRightControls();

            LoadOverviewStats();
        }

        // EnsureHeaderLogo: set BackColor to RGB(37,99,160) and hook Paint
        private void EnsureHeaderLogo()
        {
            if (_headerLogoPanel != null) return;

            _headerLogoPanel = new RoundedPanel
            {
                BorderRadius = 10,
                BackColor = Color.FromArgb(37, 99, 160), // updated blue
                Size = new Size(44, 44),
                Margin = Padding.Empty
            };

            LoadEmployeeLogoImage(); // load Properties.Resources.employeeLogo or fallback

            _headerLogoPanel.Paint -= HeaderLogoPanel_Paint;
            _headerLogoPanel.Paint += HeaderLogoPanel_Paint;

            panelHeader.Controls.Add(_headerLogoPanel);
            _headerLogoPanel.BringToFront();

            LayoutHeaderStack();
        }

        // Painter: draw inner rounded white tile and clip image inside
        private void HeaderLogoPanel_Paint(object? sender, PaintEventArgs e)
        {
            var pnl = (Control)sender!;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            // Outer ring is panel BackColor (blue). We draw an inner rounded white tile.
            var outerRect = pnl.ClientRectangle;
            outerRect.Width -= 1; outerRect.Height -= 1;

            // Inner inset to expose blue ring (~4px)
            const int inset = 4;
            var innerRect = Rectangle.Inflate(outerRect, -inset, -inset);

            // Draw inner white rounded tile
            using (var innerPath = GetRoundedRectPath(innerRect, 8))
            using (var fill = new SolidBrush(Color.White))
            {
                e.Graphics.FillPath(fill, innerPath);

                // Clip image to inner rounded shape
                var prevClip = e.Graphics.Clip;
                e.Graphics.SetClip(innerPath);

                if (_employeeLogoImage != null && innerRect.Width > 0 && innerRect.Height > 0)
                {
                    // Contain fit (preserve aspect)
                    float imgW = _employeeLogoImage.Width;
                    float imgH = _employeeLogoImage.Height;
                    float scale = Math.Min(innerRect.Width / imgW, innerRect.Height / imgH);
                    int drawW = (int)Math.Round(imgW * scale);
                    int drawH = (int)Math.Round(imgH * scale);
                    int drawX = innerRect.X + (innerRect.Width - drawW) / 2;
                    int drawY = innerRect.Y + (innerRect.Height - drawH) / 2;

                    e.Graphics.DrawImage(_employeeLogoImage, new Rectangle(drawX, drawY, drawW, drawH));
                }

                // Restore clip
                e.Graphics.SetClip(prevClip, CombineMode.Replace);
            }

            // Outer subtle border following outer rounded outline
            using (var outerPath = GetRoundedRectPath(outerRect, 10))
            using (var pen = new Pen(Color.FromArgb(220, 225, 235), 1.5f))
            {
                e.Graphics.DrawPath(pen, outerPath);
            }
        }

        // 3) Loader: typed resource first, Images fallback
        private void LoadEmployeeLogoImage()
        {
            var logo = Properties.Resources.employeeLogo;
            if (logo != null)
            {
                _employeeLogoImage = logo;
                return;
            }

            var filePath = Path.Combine(AppContext.BaseDirectory, "Images", "employeeLogo.png");
            if (File.Exists(filePath))
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var tmp = Image.FromStream(fs);
                _employeeLogoImage = new Bitmap(tmp);
            }
        }

        private void LayoutHeaderStack()
        {
            if (_headerLogoPanel == null) return;

            int marginLeft = 20;
            int spacingX = 10;
            int spacingY = 2;

            // Logo
            _headerLogoPanel.Location = new Point(marginLeft, (panelHeader.Height - _headerLogoPanel.Height) / 2);

            // Measure combined height of Title + Welcome
            lblTitle.AutoSize = true;
            lblWelcome.AutoSize = true;

            int combinedH = lblTitle.Height + spacingY + lblWelcome.Height;
            int blockTop = (panelHeader.Height - combinedH) / 2;

            int textLeft = _headerLogoPanel.Right + spacingX;

            // Title on top
            lblTitle.Location = new Point(textLeft, blockTop);

            // Welcome under Title
            lblWelcome.Location = new Point(textLeft, lblTitle.Bottom + spacingY);
        }

        private void AlignHeaderRightControls()
        {
            const int paddingRight = 20;
            const int spacing = 10;

            btnLogout.Location = new Point(
                panelHeader.ClientSize.Width - paddingRight - btnLogout.Width,
                (panelHeader.ClientSize.Height - btnLogout.Height) / 2);

            lblRole.Location = new Point(
                btnLogout.Left - spacing - lblRole.Width,
                btnLogout.Top + (btnLogout.Height - lblRole.Height) / 2);
        }

        private void PanelHeader_Resize(object? sender, EventArgs e)
        {
            LayoutHeaderStack();
            AlignHeaderRightControls();
        }

        private void LoadUserControls()
        {
            CreateOverviewTab();

            var checkInControl = new CheckInControl();
            tabCheckIn.Controls.Clear();
            checkInControl.Dock = DockStyle.Fill;
            tabCheckIn.Controls.Add(checkInControl);

            var checkOutControl = new CheckOutControl();
            tabCheckOut.Controls.Clear();
            checkOutControl.Dock = DockStyle.Fill;
            tabCheckOut.Controls.Add(checkOutControl);

            var reservationControl = new ReservationControl();
            tabReservations.Controls.Clear();
            reservationControl.Dock = DockStyle.Fill;
            tabReservations.Controls.Add(reservationControl);

            var availableRoomsControl = new AvailableRoomsControl();
            tabAvailableRooms.Controls.Clear();
            availableRoomsControl.Dock = DockStyle.Fill;
            tabAvailableRooms.Controls.Add(availableRoomsControl);

            var guestSearchControl = new GuestSearchControl();
            tabGuestSearch.Controls.Clear();
            guestSearchControl.Dock = DockStyle.Fill;
            tabGuestSearch.Controls.Add(guestSearchControl);

            var employeeManagementControl = new EmployeeManagementControl();
            tabEmployeeManagement.Controls.Clear();
            employeeManagementControl.Dock = DockStyle.Fill;
            tabEmployeeManagement.Controls.Add(employeeManagementControl);

            var roomManagementControl = new RoomManagementControl();
            tabRoomManagement.Controls.Clear();
            roomManagementControl.Dock = DockStyle.Fill;
            tabRoomManagement.Controls.Add(roomManagementControl);

            var revenueReportControl = new RevenueReportControl();
            tabRevenueReport.Controls.Clear();
            revenueReportControl.Dock = DockStyle.Fill;
            tabRevenueReport.Controls.Add(revenueReportControl);
        }

        // Replace CreateOverviewTab() with this version
        private void CreateOverviewTab()
        {
            var overviewTab = tabOverview;
            overviewTab.BackColor = Color.FromArgb(240, 244, 248);
            overviewTab.AutoScroll = true;
            overviewTab.Controls.Clear();

            Panel containerPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(1300, 1000),
                AutoScroll = true
            };
            overviewTab.Controls.Add(containerPanel);

            Label lblOverviewTitle = new Label
            {
                Text = "Dashboard Overview",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Location = new Point(10, 10)
            };
            containerPanel.Controls.Add(lblOverviewTitle);

            var borderColor = Color.FromArgb(215, 220, 230);
            const float borderThickness = 3f;

            // Stat cards (top row)
            const int statsTop = 60;
            const int statCardHeight = 120;
            const int sectionGap = 20;   // tightened vertical gap between sections

            CreateStatCard(containerPanel, "Available Rooms", 20,  statsTop, Color.FromArgb(34, 197, 94),  borderColor, borderThickness, out lblAvailableRooms);
            CreateStatCard(containerPanel, "Occupied Rooms", 340, statsTop, Color.FromArgb(59, 130, 246), borderColor, borderThickness, out lblOccupiedRooms);
            CreateStatCard(containerPanel, "Reserved Rooms", 660, statsTop, Color.FromArgb(234, 179, 8),  borderColor, borderThickness, out lblReservedRooms);
            CreateStatCard(containerPanel, "Active Check-Ins", 980, statsTop, Color.FromArgb(168, 85, 247), borderColor, borderThickness, out lblActiveCheckIns);

            // Current Occupancy (raised closer to stat cards)
            int occTop = statsTop + statCardHeight + sectionGap; // 60 + 120 + 20 = 200
            var occPanel = new RoundedPanel
            {
                BorderRadius = 12,
                BackColor = Color.White,
                Location = new Point(20, occTop),
                Size = new Size(1240, 280)
            };
            AttachRoundedBorder(occPanel, 12, borderColor, borderThickness);
            containerPanel.Controls.Add(occPanel);

            var lblOccTitle = new Label
            {
                Text = "Current Occupancy",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Location = new Point(15, 12)
            };
            occPanel.Controls.Add(lblOccTitle);

            var lblOccSub = new Label
            {
                Text = "Guests currently checked in at the hotel",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(15, 40)
            };
            occPanel.Controls.Add(lblOccSub);

            dgvCurrentOccupancy = new DataGridView
            {
                Location = new Point(15, 65),
                Size = new Size(1210, 190),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ScrollBars = ScrollBars.Vertical
            };
            dgvCurrentOccupancy.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 244, 246);
            dgvCurrentOccupancy.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
            dgvCurrentOccupancy.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvCurrentOccupancy.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgvCurrentOccupancy.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            dgvCurrentOccupancy.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgvCurrentOccupancy.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 41, 59);
            occPanel.Controls.Add(dgvCurrentOccupancy);

            // Activity Logs panel (raised by reducing spacing after occupancy)
            int logsTop = occPanel.Bottom + 12; // reduced from 20 to 12
            pnlActivityLogs = new RoundedPanel
            {
                BorderRadius = 12,
                BackColor = Color.White,
                Location = new Point(20, logsTop),
                Size = new Size(1240, 300)
            };
            AttachRoundedBorder(pnlActivityLogs, 12, borderColor, borderThickness);
            containerPanel.Controls.Add(pnlActivityLogs);

            var lblLogTitle = new Label
            {
                Text = "Activity Logs",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Location = new Point(15, 12)
            };
            pnlActivityLogs.Controls.Add(lblLogTitle);

            var lblLogSub = new Label
            {
                Text = "Track all employee activities and transactions",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(15, 40)
            };
            pnlActivityLogs.Controls.Add(lblLogSub);

            // Filters: Date | Employee | Activity Type
            int fx = 15; int fy = 70; int fw = 380; int gap = 15;

            var lblDate = new Label { Text = "Date", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(100,116,139), AutoSize = true, Location = new Point(fx, fy) };
            pnlActivityLogs.Controls.Add(lblDate);
            dtpLogDate = new DateTimePicker
            {
                Location = new Point(fx, fy + 18),
                Size = new Size(fw, 25),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            dtpLogDate.ValueChanged += (_, __) => LoadActivityLogs();
            pnlActivityLogs.Controls.Add(dtpLogDate);

            var lblEmp = new Label { Text = "Employee", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(100,116,139), AutoSize = true, Location = new Point(fx + fw + gap, fy) };
            pnlActivityLogs.Controls.Add(lblEmp);
            cboLogEmployee = new ComboBox
            {
                Location = new Point(fx + fw + gap, fy + 18),
                Size = new Size(fw, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            cboLogEmployee.SelectedIndexChanged += (_, __) => LoadActivityLogs();
            pnlActivityLogs.Controls.Add(cboLogEmployee);

            var lblType = new Label { Text = "Activity Type", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(100,116,139), AutoSize = true, Location = new Point(fx + (fw + gap) * 2, fy) };
            pnlActivityLogs.Controls.Add(lblType);
            cboLogType = new ComboBox
            {
                Location = new Point(fx + (fw + gap) * 2, fy + 18),
                Size = new Size(fw, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            cboLogType.SelectedIndexChanged += (_, __) => LoadActivityLogs();
            pnlActivityLogs.Controls.Add(cboLogType);

            lblLogSummary = new Label
            {
                Text = $"Showing 0 activities for {DateTime.Today:yyyy-MM-dd}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(15, fy + 18 + 32)
            };
            pnlActivityLogs.Controls.Add(lblLogSummary);

            dgvActivityLogs = new DataGridView
            {
                Location = new Point(15, lblLogSummary.Bottom + 10),
                Size = new Size(1210, 150),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };
            dgvActivityLogs.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 244, 246);
            dgvActivityLogs.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
            dgvActivityLogs.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvActivityLogs.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgvActivityLogs.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            dgvActivityLogs.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            pnlActivityLogs.Controls.Add(dgvActivityLogs);

            lblActivityLogsEmpty = new Label
            {
                Text = "No activities found for the selected filters",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.White,
                Location = dgvActivityLogs.Location,
                Size = dgvActivityLogs.Size,
                Visible = false
            };
            pnlActivityLogs.Controls.Add(lblActivityLogsEmpty);

            pnlActivityLogs.Resize += (_, __) =>
            {
                if (lblActivityLogsEmpty != null && dgvActivityLogs != null)
                {
                    lblActivityLogsEmpty.Location = dgvActivityLogs.Location;
                    lblActivityLogsEmpty.Size = dgvActivityLogs.Size;
                }
            };

            LoadEmployeesForLogFilter();
            LoadActivityTypesForLogFilter();
            LoadActivityLogs();
        }

        private void CreateStatCard(Panel parent, string title, int x, int y, Color color, Color borderColor, float borderThickness, out Label valueLabel)
        {
            var card = new RoundedPanel
            {
                BorderRadius = 12,
                BackColor = Color.White,
                Location = new Point(x, y),
                Size = new Size(300, 120)
            };
            AttachRoundedBorder(card, 12, borderColor, borderThickness);
            parent.Controls.Add(card);

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(15, 15),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            valueLabel = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = color,
                Location = new Point(15, 45),
                AutoSize = true
            };
            card.Controls.Add(valueLabel);
        }

        private void AttachRoundedBorder(Control ctrl, int radius, Color borderColor, float thickness)
        {
            ctrl.Paint -= Control_DrawRoundedBorder;
            ctrl.Paint += Control_DrawRoundedBorder;

            void Control_DrawRoundedBorder(object? sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(borderColor, thickness);
                var rect = ctrl.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using var path = GetRoundedRectPath(rect, radius);
                e.Graphics.DrawPath(pen, path);
            }
        }

        private void StyleTabControl(TabControl tc, bool isAdmin)
        {
            // Colors
            tc.BackColor = Color.FromArgb(244, 246, 250); // bar background
            tc.DrawMode = TabDrawMode.OwnerDrawFixed;
            tc.SizeMode = TabSizeMode.Fixed;
            tc.ItemSize = new Size(150, 36); // width auto adjusted by text; height fixed
            tc.Padding = new Point(18, 6);   // inner padding
            tc.HotTrack = true;

            // Optional images
            _tabImages = BuildTabImageList();
            if (_tabImages != null)
            {
                tc.ImageList = _tabImages;
                TryAssignImageKey(tabOverview, "ic_home");
                TryAssignImageKey(tabCheckIn, "ic_checkin");
                TryAssignImageKey(tabCheckOut, "ic_checkout");
                TryAssignImageKey(tabReservations, "ic_calendar");
                TryAssignImageKey(tabAvailableRooms, "ic_bed");
                TryAssignImageKey(tabGuestSearch, "ic_search");
                // Admin pages
                TryAssignImageKey(tabEmployeeManagement, "ic_users");
                TryAssignImageKey(tabRoomManagement, "ic_settings");
                TryAssignImageKey(tabRevenueReport, "ic_chart");
            }

            tc.DrawItem -= TabControl_DrawItem;
            tc.DrawItem += TabControl_DrawItem;
            tc.ControlAdded -= (_, __) => tc.Invalidate();
            tc.ControlAdded += (_, __) => tc.Invalidate();
        }

        private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            var tc = (TabControl)sender!;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var selected = (e.Index == tc.SelectedIndex);
            var page = tc.TabPages[e.Index];
            var rect = GetTabBounds(e.Bounds);

            // Colors
            var pillBack = selected ? Color.White : Color.FromArgb(244, 246, 250); // bar color for unselected
            var pillBorder = selected ? Color.FromArgb(215, 220, 230) : Color.FromArgb(215, 220, 230);
            var textColor = selected ? Color.FromArgb(17, 24, 39) : Color.FromArgb(55, 65, 81);

            using var path = GetRoundedRectPath(rect, 14);
            using var back = new SolidBrush(pillBack);
            using var pen = new Pen(pillBorder, selected ? 2f : 1f);

            g.FillPath(back, path);
            g.DrawPath(pen, path);

            // Icon + text
            var imgX = rect.X + 12;
            var textX = rect.X + 14;
            var textY = rect.Y + (rect.Height - e.Font.Height) / 2;

            if (tc.ImageList != null && page.ImageIndex >= 0 && page.ImageIndex < tc.ImageList.Images.Count)
            {
                var img = tc.ImageList.Images[page.ImageIndex];
                var imgRect = new Rectangle(imgX, rect.Y + (rect.Height - 16) / 2, 16, 16);
                g.DrawImage(img, imgRect);
                textX = imgRect.Right + 8;
            }

            TextRenderer.DrawText(
                g,
                page.Text,
                e.Font,
                new Point(textX, textY),
                textColor,
                TextFormatFlags.NoClipping | TextFormatFlags.EndEllipsis);
        }

        private static Rectangle GetTabBounds(Rectangle bounds)
        {
            // Inflate/deflate to create spacing between pills
            var r = bounds;
            r.Inflate(-6, -6);
            return r;
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            var arc = new Rectangle(rect.X, rect.Y, d, d);
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private ImageList? BuildTabImageList()
        {
            // Create if you have icons in Resources with these names; otherwise returns null and draws text only
            var list = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(16, 16) };
            bool any = false;
            any |= TryAddImage(list, "ic_home", Properties.Resources.ResourceManager.GetObject("ic_home") as Image);
            any |= TryAddImage(list, "ic_checkin", Properties.Resources.ResourceManager.GetObject("ic_checkin") as Image);
            any |= TryAddImage(list, "ic_checkout", Properties.Resources.ResourceManager.GetObject("ic_checkout") as Image);
            any |= TryAddImage(list, "ic_calendar", Properties.Resources.ResourceManager.GetObject("ic_calendar") as Image);
            any |= TryAddImage(list, "ic_bed", Properties.Resources.ResourceManager.GetObject("ic_bed") as Image);
            any |= TryAddImage(list, "ic_search", Properties.Resources.ResourceManager.GetObject("ic_search") as Image);
            any |= TryAddImage(list, "ic_users", Properties.Resources.ResourceManager.GetObject("ic_users") as Image);
            any |= TryAddImage(list, "ic_settings", Properties.Resources.ResourceManager.GetObject("ic_settings") as Image);
            any |= TryAddImage(list, "ic_chart", Properties.Resources.ResourceManager.GetObject("ic_chart") as Image);
            return any ? list : null;
        }

        private static bool TryAddImage(ImageList list, string key, Image? img)
        {
            if (img == null) return false;
            list.Images.Add(key, img);
            return true;
        }

        private void TryAssignImageKey(TabPage? page, string key)
        {
            if (page == null || _tabImages == null) return;
            int idx = _tabImages.Images.IndexOfKey(key);
            if (idx >= 0) page.ImageIndex = idx;
        }

        // Replace the whole LoadOverviewStats() method (Occupied Rooms from active check-ins)
        private void LoadOverviewStats()
        {
            try
            {
                using var conn = _dbService.GetConnection();
                conn.Open();

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Rooms WHERE Status = 'Available'", conn))
                    lblAvailableRooms.Text = Convert.ToString(cmd.ExecuteScalar()) ?? "0";

                // Occupied Rooms = distinct rooms in active CheckIns (accurate even if Rooms.Status lags)
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(DISTINCT RoomID) FROM CheckIns WHERE ActualCheckOutDateTime IS NULL", conn))
                    lblOccupiedRooms.Text = Convert.ToString(cmd.ExecuteScalar()) ?? "0";

                using (var cmd = new SqlCommand(@"
                    SELECT COUNT(DISTINCT RoomID)
                    FROM Reservations
                    WHERE ReservationStatus IN ('Confirmed', 'Pending')
                      AND CheckInDate >= CAST(GETDATE() AS DATE)", conn))
                    lblReservedRooms.Text = Convert.ToString(cmd.ExecuteScalar()) ?? "0";

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM CheckIns WHERE ActualCheckOutDateTime IS NULL", conn))
                    lblActiveCheckIns.Text = Convert.ToString(cmd.ExecuteScalar()) ?? "0";

                if (dgvCurrentOccupancy != null)
                    LoadCurrentOccupancy(conn);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statistics: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Existing LoadCurrentOccupancy method remains unchanged...

        // NEW: Fill Employee filter (All Employees + list)
        private void LoadEmployeesForLogFilter()
        {
            try
            {
                var items = new List<KeyValuePair<int?, string>>
                {
                    new KeyValuePair<int?, string>(null, "All Employees")
                };

                using var conn = _dbService.GetConnection();
                conn.Open();
                using var cmd = new SqlCommand(
                    "SELECT EmployeeID, FirstName, LastName FROM Employees WHERE Role = @Role ORDER BY FirstName, LastName",
                    conn);
                cmd.Parameters.AddWithValue("@Role", "Employee");

                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var id = rdr.GetInt32(0);
                    var name = $"{rdr.GetString(1)} {rdr.GetString(2)}";
                    items.Add(new KeyValuePair<int?, string>(id, name));
                }

                cboLogEmployee.DataSource = items;
                cboLogEmployee.DisplayMember = "Value";
                cboLogEmployee.ValueMember = "Key";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading employees: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Fallback
                cboLogEmployee.DataSource = null;
                cboLogEmployee.Items.Clear();
                cboLogEmployee.Items.Add("All Employees");
                cboLogEmployee.SelectedIndex = 0;
            }
        }

        // NEW: Fill Activity Type filter
        private void LoadActivityTypesForLogFilter()
        {
            var items = new List<KeyValuePair<string?, string>>
            {
                new KeyValuePair<string?, string>(null, "All Types"),
                new KeyValuePair<string?, string>("Login", "Login"),
                new KeyValuePair<string?, string>("Check-In", "Check-In"),
                new KeyValuePair<string?, string>("Check-Out", "Check-Out"),
                new KeyValuePair<string?, string>("Reservation", "Reservation"),
                new KeyValuePair<string?, string>("Payment", "Payment")
            };
            cboLogType.DataSource = items;
            cboLogType.DisplayMember = "Value";
            cboLogType.ValueMember = "Key";
        }

        // NEW: Load Activity Logs with filters
        private void LoadActivityLogs()
        {
            if (pnlActivityLogs == null) return;

            try
            {
                using var conn = _dbService.GetConnection();
                conn.Open();

                var sql = @"
                    SELECT 
                        al.ActivityDateTime,                              -- Time
                        (e.FirstName + ' ' + e.LastName) AS Employee,
                        al.ActivityType,
                        al.ActivityDescription
                    FROM ActivityLog al
                    INNER JOIN Employees e ON al.EmployeeID = e.EmployeeID
                    WHERE e.Role = 'Employee'                               -- exclude Admins
                      AND CAST(al.ActivityDateTime AS DATE) = @Date
                      AND (@EmpId IS NULL OR al.EmployeeID = @EmpId)
                      AND (@Type IS NULL OR al.ActivityType = @Type)
                    ORDER BY al.ActivityDateTime DESC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Date", dtpLogDate.Value.Date);

                var empId = (cboLogEmployee.SelectedValue as int?) ?? (cboLogEmployee.SelectedItem is KeyValuePair<int?, string> kv ? kv.Key : null);
                cmd.Parameters.AddWithValue("@EmpId", (object?)empId ?? DBNull.Value);

                var typeVal = (cboLogType.SelectedValue as string) ?? (cboLogType.SelectedItem is KeyValuePair<string?, string> kv2 ? kv2.Key : null);
                cmd.Parameters.AddWithValue("@Type", (object?)typeVal ?? DBNull.Value);

                using var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                dgvActivityLogs.DataSource = dt;

                if (dgvActivityLogs.Columns.Contains("ActivityDateTime"))
                {
                    dgvActivityLogs.Columns["ActivityDateTime"].HeaderText = "Time";
                    dgvActivityLogs.Columns["ActivityDateTime"].DefaultCellStyle.Format = "HH:mm";
                    dgvActivityLogs.Columns["ActivityDateTime"].FillWeight = 70;
                }
                if (dgvActivityLogs.Columns.Contains("Employee"))
                {
                    dgvActivityLogs.Columns["Employee"].HeaderText = "Employee";
                    dgvActivityLogs.Columns["Employee"].FillWeight = 160;
                }
                if (dgvActivityLogs.Columns.Contains("ActivityType"))
                {
                    dgvActivityLogs.Columns["ActivityType"].HeaderText = "Activity Type";
                    dgvActivityLogs.Columns["ActivityType"].FillWeight = 120;
                }
                if (dgvActivityLogs.Columns.Contains("ActivityDescription"))
                {
                    dgvActivityLogs.Columns["ActivityDescription"].HeaderText = "Description";
                    dgvActivityLogs.Columns["ActivityDescription"].FillWeight = 450;
                    dgvActivityLogs.Columns["ActivityDescription"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                }

                dgvActivityLogs.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                lblLogSummary.Text = $"Showing {dt.Rows.Count} activities for {dtpLogDate.Value:yyyy-MM-dd}";

                bool empty = dt.Rows.Count == 0;
                lblActivityLogsEmpty.Visible = empty;
                if (empty) lblActivityLogsEmpty.BringToFront();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading activity logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabOverview)
            {
                LoadOverviewStats();
                LoadActivityLogs(); // refresh logs when returning to Overview
            }
        }

        // Helpers (place anywhere inside the class)
        private void EmbedTabControlInHost(TabControl tc, int leftPadding)
        {
            if (tc.Parent == null || _tabHost != null) return;

            var parent = tc.Parent;
            parent.SuspendLayout();

            _tabHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(leftPadding, 0, 0, 0),
                BackColor = Color.FromArgb(244, 246, 250) // same as bar/color we paint for tabs
            };

            int z = parent.Controls.GetChildIndex(tc);
            parent.Controls.Remove(tc);

            _tabHost.Controls.Add(tc);
            tc.Dock = DockStyle.Fill;

            parent.Controls.Add(_tabHost);
            parent.Controls.SetChildIndex(_tabHost, z);

            parent.ResumeLayout(performLayout: true);
        }

        private void SoftenTabPages(TabControl tc)
        {
            // Make TabPages blend with the bar so only inner curved panels are visible
            var pageBack = Color.FromArgb(244, 246, 250);
            foreach (TabPage page in tc.TabPages)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = pageBack;
            }
        }

        // Rounded region helper for lblRole pill
        private static void ApplyRoundedRegion(Control control, int radius)
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            using var path = GetRoundedRectPath(new Rectangle(Point.Empty, control.Size), radius);
            control.Region = new Region(path);
        }

        private void btnLogout_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                _authService.Logout();

                this.Hide();
                var login = new LoginForm { StartPosition = FormStartPosition.CenterScreen };
                login.FormClosed += (_, __) =>
                {
                    _suppressLogoutPrompt = true; // suppress prompt when closing from LoginForm close
                    this.Close();
                };
                login.Show();
            }
        }

        private void AdminDashboardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_suppressLogoutPrompt)
            {
                // Allow closing silently (triggered by LoginForm closing)
                return;
            }

            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                e.Cancel = true;
                _authService.Logout();

                this.Hide();
                var login = new LoginForm { StartPosition = FormStartPosition.CenterScreen };
                login.FormClosed += (_, __) =>
                {
                    _suppressLogoutPrompt = true; // prevent prompt when we finally close
                    this.Close();
                };
                login.Show();
            }
            else
            {
                e.Cancel = true;
            }
        }

        // Place inside AdminDashboardForm class
private void LoadCurrentOccupancy(SqlConnection existingConn)
{
    try
    {
        const string query = @"
            SELECT
                rm.RoomNumber AS [Room],
                (g.FirstName + ' ' + g.LastName) AS [GuestName],
                g.PhoneNumber AS [Contact],
                CAST(c.CheckInDateTime AS DATE) AS [Check In],
                c.ExpectedCheckOutDate AS [Expected Check Out],
                c.NumberOfGuests AS [Guests],
                ISNULL(c.Notes, '') AS [Notes]
            FROM CheckIns c
            INNER JOIN Rooms rm ON c.RoomID = rm.RoomID
            INNER JOIN Guests g ON c.GuestID = g.GuestID
            WHERE c.ActualCheckOutDateTime IS NULL
            ORDER BY c.CheckInDateTime DESC";

        using var adapter = new SqlDataAdapter(query, existingConn);
        var dt = new DataTable();
        adapter.Fill(dt);

        dgvCurrentOccupancy.DataSource = dt;

        if (dgvCurrentOccupancy.Columns.Contains("GuestName"))
            dgvCurrentOccupancy.Columns["GuestName"].HeaderText = "Guest";

        if (dgvCurrentOccupancy.Columns.Contains("Check In"))
            dgvCurrentOccupancy.Columns["Check In"].DefaultCellStyle.Format = "yyyy-MM-dd";

        if (dgvCurrentOccupancy.Columns.Contains("Expected Check Out"))
            dgvCurrentOccupancy.Columns["Expected Check Out"].DefaultCellStyle.Format = "yyyy-MM-dd";

        if (dgvCurrentOccupancy.Columns.Contains("Room"))
            dgvCurrentOccupancy.Columns["Room"].FillWeight = 70;

        if (dgvCurrentOccupancy.Columns.Contains("Contact"))
            dgvCurrentOccupancy.Columns["Contact"].FillWeight = 120;

        if (dgvCurrentOccupancy.Columns.Contains("Guests"))
            dgvCurrentOccupancy.Columns["Guests"].FillWeight = 60;

        if (dgvCurrentOccupancy.Columns.Contains("Notes"))
        {
            dgvCurrentOccupancy.Columns["Notes"].FillWeight = 220;
            dgvCurrentOccupancy.Columns["Notes"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        }

        dgvCurrentOccupancy.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error loading current occupancy: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
    }
}

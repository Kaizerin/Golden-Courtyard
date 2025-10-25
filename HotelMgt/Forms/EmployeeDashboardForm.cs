using HotelMgt.Services;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient; // Use Microsoft.Data.SqlClient only
using HotelMgt.UserControls.Employee;
using HotelMgt.Utilities;
using HotelMgt.Custom; // RoundedPanel
using System.Diagnostics.CodeAnalysis; // ADD
using System.Drawing.Drawing2D; // ADD
using System.IO; // ADD
using System.Drawing.Text;

namespace HotelMgt.Forms
{
    public partial class EmployeeDashboardForm : Form
    {
        private readonly AuthenticationService _authService;
        private readonly DatabaseService _dbService;

        // Overview tab UI
        private Label lblAvailableRooms = null!;
        private Label lblOccupiedRooms = null!;
        private Label lblReservedRooms = null!;
        private Label lblActiveCheckIns = null!;

        private DataGridView dgvCurrentOccupancy = null!;

        // Header logo elements (like Admin)
        private RoundedPanel? _headerLogoPanel;    // ADD
        private PictureBox? _headerLogoPictureBox; // ADD
        private Image? _employeeLogoImage; // cache

        private bool _suppressLogoutPrompt; // ADD
        private ImageList? _tabImages; // icons (optional)
        private Panel? _tabHost;

        public EmployeeDashboardForm()
        {
            InitializeComponent();

            // NEW: swap in a borderless TabControl (removes left/right page lines)
            tabControl = TabControlBorderless.Replace(tabControl);

            _authService = new AuthenticationService();
            _dbService = new DatabaseService();

            panelHeader.Resize += PanelHeader_Resize;
            btnLogout.Click += BtnLogout_Click;

            // Header: add logo panel (left) and layout Title + Welcome stacked
            EnsureHeaderLogo(); // ADD

            // Build Overview UI first, then load stats
            CreateOverviewTab();
            LoadUserControls();
            LoadOverviewStats();
            TrimEmployeeTabs();                 // remove admin-only pages

            // Left gutter for tab bar + softer pages
            EmbedTabControlInHost(tabControl, leftPadding: 20);
            StyleTabControl(tabControl, false);
            SoftenTabPages(tabControl);

            tabControl.SelectedIndexChanged += (s, e) =>
            {
                if (tabControl.SelectedTab == tabOverview)
                    LoadOverviewStats();
            };
        }

        private void EmployeeDashboardForm_Load(object sender, EventArgs e)
        {
            this.Text = $"Hotel Management - {CurrentUser.FullName} ({CurrentUser.Role})";
            lblWelcome.Text = $"Welcome, {CurrentUser.FullName}";
            lblRole.Text = CurrentUser.Role?.ToUpper() ?? "EMPLOYEE";

            // Style role badge as a rounded pill
            lblRole.AutoSize = true;
            lblRole.BorderStyle = BorderStyle.None; // remove boxy border
            lblRole.Padding = new Padding(10, 4, 10, 4); // horizontal padding for pill shape
            ApplyRoundedRegion(lblRole, 10);
            lblRole.SizeChanged -= (_, __) => { };
            lblRole.SizeChanged += (_, __) => ApplyRoundedRegion(lblRole, 10);

            LayoutHeaderStack();      // ensure left header layout
            AlignHeaderRightControls();
        }

        private void BtnLogout_Click(object? sender, EventArgs e)
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

        private void EmployeeDashboardForm_FormClosing(object sender, FormClosingEventArgs e)
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

        // Update: include stacked title/welcome layout on resize
        private void PanelHeader_Resize(object? sender, EventArgs e)
        {
            LayoutHeaderStack();      // keep left header in place
            AlignHeaderRightControls();
        }

        // Position the logo (left), and stack Title above Welcome, centered as a group
        private void LayoutHeaderStack()
        {
            if (_headerLogoPanel == null) return;

            int marginLeft = 20;
            int spacingX = 10;
            int spacingY = 2;

            // Logo at left
            _headerLogoPanel.Location = new Point(marginLeft, (panelHeader.Height - _headerLogoPanel.Height) / 2);

            // Title + Welcome stacked
            lblTitle.AutoSize = true;
            lblWelcome.AutoSize = true;

            int combinedH = lblTitle.Height + spacingY + lblWelcome.Height;
            int blockTop = (panelHeader.Height - combinedH) / 2;
            int textLeft = _headerLogoPanel.Right + spacingX;

            lblTitle.Location = new Point(textLeft, blockTop);
            lblWelcome.Location = new Point(textLeft, lblTitle.Bottom + spacingY);
        }

        // Header: add logo box and load employeeLogo (resource or file fallback)
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

            LoadEmployeeLogoImage();

            _headerLogoPanel.Paint -= HeaderLogoPanel_Paint;
            _headerLogoPanel.Paint += HeaderLogoPanel_Paint;

            panelHeader.Controls.Add(_headerLogoPanel);
            _headerLogoPanel.BringToFront();

            LayoutHeaderStack();
        }

        private void HeaderLogoPanel_Paint(object? sender, PaintEventArgs e)
        {
            var pnl = (Control)sender!;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            var outerRect = pnl.ClientRectangle;
            outerRect.Width -= 1; outerRect.Height -= 1;

            const int inset = 4;
            var innerRect = Rectangle.Inflate(outerRect, -inset, -inset);

            using (var innerPath = GetRoundedRectPath(innerRect, 8))
            using (var fill = new SolidBrush(Color.White))
            {
                e.Graphics.FillPath(fill, innerPath);

                var prevClip = e.Graphics.Clip;
                e.Graphics.SetClip(innerPath);

                if (_employeeLogoImage != null && innerRect.Width > 0 && innerRect.Height > 0)
                {
                    float imgW = _employeeLogoImage.Width;
                    float imgH = _employeeLogoImage.Height;
                    float scale = Math.Min(innerRect.Width / imgW, innerRect.Height / imgH);
                    int drawW = (int)Math.Round(imgW * scale);
                    int drawH = (int)Math.Round(imgH * scale);
                    int drawX = innerRect.X + (innerRect.Width - drawW) / 2;
                    int drawY = innerRect.Y + (innerRect.Height - drawH) / 2;

                    e.Graphics.DrawImage(_employeeLogoImage, new Rectangle(drawX, drawY, drawW, drawH));
                }

                e.Graphics.SetClip(prevClip, CombineMode.Replace);
            }

            using (var outerPath = GetRoundedRectPath(outerRect, 10))
            using (var pen = new Pen(Color.FromArgb(220, 225, 235), 1.5f))
            {
                e.Graphics.DrawPath(pen, outerPath);
            }
        }

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

        private static void LoadHeaderLogoImage(PictureBox pb)
        {
            var resObj = Properties.Resources.ResourceManager.GetObject("employeeLogo");
            if (resObj is Image resImage)
            {
                pb.Image = new Bitmap(resImage);
                return;
            }

            string imagePath = Path.Combine(AppContext.BaseDirectory, "Images", "employeeLogo.png");
            if (File.Exists(imagePath))
            {
                using var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var temp = Image.FromStream(fs);
                pb.Image = new Bitmap(temp); // clone to avoid locking
            }
        }

        // Ensure alignment helper can’t crash if designer fields were not yet created
        private void AlignHeaderRightControls()
        {
            if (panelHeader == null || btnLogout == null || lblRole == null) return;

            const int paddingRight = 20;
            const int spacing = 10;

            btnLogout.Location = new Point(
                panelHeader.ClientSize.Width - paddingRight - btnLogout.Width,
                (panelHeader.ClientSize.Height - btnLogout.Height) / 2);

            lblRole.Location = new Point(
                btnLogout.Left - spacing - lblRole.Width,
                btnLogout.Top + (btnLogout.Height - lblRole.Height) / 2);
        }

        // Tell the compiler that these members are initialized inside this method
        [MemberNotNull(nameof(lblAvailableRooms),
                       nameof(lblOccupiedRooms),
                       nameof(lblReservedRooms),
                       nameof(lblActiveCheckIns),
                       nameof(dgvCurrentOccupancy))]
        private void CreateOverviewTab()
        {
            // Ensure designer tab exists
            if (tabOverview == null) return;

            tabOverview.BackColor = Color.FromArgb(240, 244, 248);
            tabOverview.AutoScroll = true;
            tabOverview.Controls.Clear();

            var container = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(1300, 720),
                AutoScroll = true
            };
            tabOverview.Controls.Add(container);

            var lblTitle = new Label
            {
                Text = "Dashboard Overview",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Location = new Point(10, 10)
            };
            container.Controls.Add(lblTitle);

            // Subtle border color for outlines
            var borderColor = Color.FromArgb(220, 225, 235);

            // Stat cards - RoundedPanel
            CreateStatCardRounded(container, "Available Rooms", 20, 60, Color.FromArgb(34, 197, 94), borderColor, out lblAvailableRooms);
            CreateStatCardRounded(container, "Occupied Rooms", 340, 60, Color.FromArgb(59, 130, 246), borderColor, out lblOccupiedRooms);
            CreateStatCardRounded(container, "Reserved Rooms", 660, 60, Color.FromArgb(234, 179, 8), borderColor, out lblReservedRooms);
            CreateStatCardRounded(container, "Active Check-Ins", 980, 60, Color.FromArgb(168, 85, 247), borderColor, out lblActiveCheckIns);

            // Current Occupancy - Rounded container + grid (moved up since Refresh button is removed)
            var occPanel = new RoundedPanel
            {
                BorderRadius = 12,
                BackColor = Color.White,
                Location = new Point(20, 220),
                Size = new Size(1240, 280)
            };
            // Add grayish outline
            AttachRoundedBorder(occPanel, 12, borderColor);
            container.Controls.Add(occPanel);

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
                Text = "Guests currently checked in the hotel",
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
                ScrollBars = ScrollBars.Vertical,
                // New: make unresizable and fit content
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                DefaultCellStyle = { WrapMode = DataGridViewTriState.True }
            };
            dgvCurrentOccupancy.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 244, 246);
            dgvCurrentOccupancy.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
            dgvCurrentOccupancy.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvCurrentOccupancy.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgvCurrentOccupancy.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            dgvCurrentOccupancy.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgvCurrentOccupancy.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 41, 59);
            occPanel.Controls.Add(dgvCurrentOccupancy);
        }

        private void CreateStatCardRounded(Control parent, string title, int x, int y, Color highlightColor, Color borderColor, out Label valueLabel)
        {
            var card = new RoundedPanel
            {
                BorderRadius = 12,
                BackColor = Color.White,
                Location = new Point(x, y),
                Size = new Size(300, 120)
            };
            // Add grayish outline
            AttachRoundedBorder(card, 12, borderColor);
            parent.Controls.Add(card);

            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(15, 15),
                AutoSize = true
            };
            card.Controls.Add(lbl);

            valueLabel = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = highlightColor,
                Location = new Point(15, 48),
                AutoSize = true
            };
            card.Controls.Add(valueLabel);
        }

        // Draw a 1px rounded outline on a control
        private void AttachRoundedBorder(Control ctrl, int radius, Color borderColor)
        {
            ctrl.Paint -= Control_DrawRoundedBorder;
            ctrl.Paint += Control_DrawRoundedBorder;

            void Control_DrawRoundedBorder(object? sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(borderColor, 1f);
                var rect = ctrl.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using var path = GetRoundedRectPath(rect, radius);
                e.Graphics.DrawPath(pen, path);
            }
        }

        private void TrimEmployeeTabs()
        {
            // Remove admin-only tabs if present on Employee dashboard
            // Common admin tabs: Employees, Room Mgmt, Reports
            RemoveTabIfExists("tabEmployeeManagement");
            RemoveTabIfExists("tabRoomManagement");
            RemoveTabIfExists("tabRevenueReport");
            // Optionally remove other admin tabs if they exist
            RemoveTabIfExists("tabActivityLogs");
        }

        private void RemoveTabIfExists(string fieldName)
        {
            var field = GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field?.GetValue(this) is TabPage page && page.Parent == tabControl)
            {
                tabControl.TabPages.Remove(page);
            }
        }

        private void StyleTabControl(TabControl tc, bool isAdmin)
        {
            tc.BackColor = Color.FromArgb(244, 246, 250);
            tc.DrawMode = TabDrawMode.OwnerDrawFixed;
            tc.SizeMode = TabSizeMode.Fixed;
            tc.ItemSize = new Size(150, 36);
            tc.Padding = new Point(18, 6);
            tc.HotTrack = true;

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
                // Employee intentionally has no Employees/Room Mgmt/Reports tabs
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

            var pillBack = selected ? Color.White : Color.FromArgb(244, 246, 250);
            var pillBorder = Color.FromArgb(215, 220, 230);
            var textColor = selected ? Color.FromArgb(17, 24, 39) : Color.FromArgb(55, 65, 81);

            using var path = GetRoundedRectPath(rect, 14);
            using var back = new SolidBrush(pillBack);
            using var pen = new Pen(pillBorder, selected ? 2f : 1f);

            g.FillPath(back, path);
            g.DrawPath(pen, path);

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

        private static void ApplyRoundedRegion(Control control, int radius)
        {
            if (control == null) return;
            if (control.Width <= 0 || control.Height <= 0) return;

            using var path = GetRoundedRectPath(new Rectangle(Point.Empty, control.Size), radius);
            control.Region = new Region(path);
        }

        private ImageList? BuildTabImageList()
        {
            var list = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(16, 16) };
            bool any = false;
            any |= TryAddImage(list, "ic_home", Properties.Resources.ResourceManager.GetObject("ic_home") as Image);
            any |= TryAddImage(list, "ic_checkin", Properties.Resources.ResourceManager.GetObject("ic_checkin") as Image);
            any |= TryAddImage(list, "ic_checkout", Properties.Resources.ResourceManager.GetObject("ic_checkout") as Image);
            any |= TryAddImage(list, "ic_calendar", Properties.Resources.ResourceManager.GetObject("ic_calendar") as Image);
            any |= TryAddImage(list, "ic_bed", Properties.Resources.ResourceManager.GetObject("ic_bed") as Image);
            any |= TryAddImage(list, "ic_search", Properties.Resources.ResourceManager.GetObject("ic_search") as Image);
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

        private void LoadOverviewStats()
        {
            try
            {
                // Ensure labels are ready (compiler/runtime)
                if (lblAvailableRooms == null || lblOccupiedRooms == null ||
                    lblReservedRooms == null || lblActiveCheckIns == null)
                {
                    CreateOverviewTab();
                    if (lblAvailableRooms == null) return; // still not ready; avoid null ref
                }

                using var conn = _dbService.GetConnection();
                conn.Open();

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Rooms WHERE Status = 'Available'", conn))
                    lblAvailableRooms.Text = Convert.ToString(cmd.ExecuteScalar()) ?? "0";

                // FIX: Occupied Rooms = distinct rooms in active CheckIns (not Rooms.Status)
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

                dgvCurrentOccupancy.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading current occupancy: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadUserControls()
        {
            // Check-In
            var checkInControl = new CheckInControl();
            tabCheckIn.Controls.Clear();
            checkInControl.Dock = DockStyle.Fill;
            tabCheckIn.Controls.Add(checkInControl);

            // Check-Out
            var checkOutControl = new CheckOutControl();
            tabCheckOut.Controls.Clear();
            checkOutControl.Dock = DockStyle.Fill;
            tabCheckOut.Controls.Add(checkOutControl);

            // Reservations
            var reservationControl = new ReservationControl();
            tabReservations.Controls.Clear();
            reservationControl.Dock = DockStyle.Fill;
            tabReservations.Controls.Add(reservationControl);

            // Available Rooms
            var availableRoomsControl = new AvailableRoomsControl();
            tabAvailableRooms.Controls.Clear();
            availableRoomsControl.Dock = DockStyle.Fill;
            tabAvailableRooms.Controls.Add(availableRoomsControl);

            // Guest Search tab
            var guestSearchControl = new GuestSearchControl();
            tabGuestSearch.Controls.Clear();
            guestSearchControl.Dock = DockStyle.Fill;
            tabGuestSearch.Controls.Add(guestSearchControl);
        }

        private void EmbedTabControlInHost(TabControl tc, int leftPadding)
        {
            if (tc.Parent == null || _tabHost != null) return;

            var parent = tc.Parent;
            parent.SuspendLayout();

            _tabHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(leftPadding, 0, 0, 0),
                BackColor = Color.FromArgb(244, 246, 250)
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
            var pageBack = Color.FromArgb(244, 246, 250);
            foreach (TabPage page in tc.TabPages)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = pageBack;
            }
        }
    }
}
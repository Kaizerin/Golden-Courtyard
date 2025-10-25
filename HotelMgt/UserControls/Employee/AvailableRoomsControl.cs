using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Data.SqlClient; // Use Microsoft.Data.SqlClient only
using HotelMgt.Services;
using System.Globalization; // ADD
using System.Drawing.Drawing2D; // ADD
using HotelMgt.Custom; // ADD (RoundedPanel)

namespace HotelMgt.UserControls.Employee
{
    public partial class AvailableRoomsControl : UserControl
    {
        private readonly DatabaseService _dbService;
        private readonly CultureInfo _currencyCulture = CultureInfo.GetCultureInfo("en-PH"); // ADD

        // UI
        private Label lblTitle = null!, lblSubtitle = null!;
        private Label lblSummary = null!; // ADD: "Showing X of Y"
        private Label lblSearchTitle = null!, lblStatusTitle = null!, lblTypeTitle = null!; // section labels
        private TextBox txtSearch = null!; // ADD: Search by room number
        private ComboBox cboFilterStatus = null!, cboFilterType = null!;
        private DataGridView dgvRooms = null!;

        // NEW: Amenities side panel
        private RoundedPanel pnlAmenities = null!;
        private Label lblAmenitiesTitle = null!;
        private Label lblAmenitiesText = null!;

        public AvailableRoomsControl()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            this.Load += AvailableRoomsControl_Load;
        }

        private void AvailableRoomsControl_Load(object? sender, EventArgs e)
        {
            InitializeControls();
            LoadRoomTypes();     // Populate type filter
            LoadRooms();         // Initial grid load
        }

        private void InitializeControls()
        {
            SuspendLayout();
            Controls.Clear();

            BackColor = Color.White;
            Dock = DockStyle.Fill;

            // Title
            lblTitle = new Label
            {
                Text = "Room Inventory",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            lblSubtitle = new Label
            {
                Text = "View and filter all hotel rooms",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Location = new Point(20, 55),
                AutoSize = true
            };
            Controls.Add(lblSubtitle);

            // Header row labels: Search | Status | Type
            var y = 95;
            lblSearchTitle = new Label { Text = "Search Rooms", Font = new Font("Segoe UI", 9), Location = new Point(20, y), AutoSize = true };
            Controls.Add(lblSearchTitle);
            lblStatusTitle = new Label { Text = "Filter by Status", Font = new Font("Segoe UI", 9), Location = new Point(340, y), AutoSize = true };
            Controls.Add(lblStatusTitle);
            lblTypeTitle = new Label { Text = "Filter by Type", Font = new Font("Segoe UI", 9), Location = new Point(620, y), AutoSize = true };
            Controls.Add(lblTypeTitle);

            // Controls row: Search textbox | Status combo | Type combo
            txtSearch = new TextBox
            {
                Location = new Point(20, y + 22),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Search by room number"
            };
            txtSearch.TextChanged += (_, __) => LoadRooms();
            Controls.Add(txtSearch);

            cboFilterStatus = new ComboBox
            {
                Location = new Point(340, y + 22),
                Size = new Size(220, 25),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // Required order per design
            cboFilterStatus.Items.AddRange(new object[] { "All Statuses", "Available", "Occupied", "Reserved", "Maintenance" });
            cboFilterStatus.SelectedIndex = 0;
            cboFilterStatus.SelectedIndexChanged += (_, __) => LoadRooms();
            Controls.Add(cboFilterStatus);

            cboFilterType = new ComboBox
            {
                Location = new Point(620, y + 22),
                Size = new Size(220, 25),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // Values per design; includes "All Types" for convenience
            cboFilterType.Items.AddRange(new object[] { "All Types", "Single", "Double", "Suite", "Deluxe" });
            cboFilterType.SelectedIndex = 0;
            cboFilterType.SelectedIndexChanged += (_, __) => LoadRooms();
            Controls.Add(cboFilterType);

            // Summary: "Showing [Number] of [Number] rooms"
            lblSummary = new Label
            {
                Text = "Showing 0 of 0 rooms",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(20, y + 22 + 35),
                AutoSize = true
            };
            Controls.Add(lblSummary);

            // Layout numbers
            int gridX = 20;
            int gridY = y + 22 + 60;
            int gridWidth = 780;  // shrink table to leave space on the right
            int gridHeight = 520;
            int gap = 20;

            // Grid
            dgvRooms = new DataGridView
            {
                Location = new Point(gridX, gridY),
                Size = new Size(gridWidth, gridHeight),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, // hide the left indicator column
                RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing,
                DefaultCellStyle = { WrapMode = DataGridViewTriState.True },
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };
            dgvRooms.SelectionChanged -= DgvRooms_SelectionChanged;
            dgvRooms.SelectionChanged += DgvRooms_SelectionChanged;
            Controls.Add(dgvRooms);

            // Amenities panel (right)
            pnlAmenities = new RoundedPanel
            {
                BorderRadius = 12,
                BackColor = Color.White,
                Location = new Point(dgvRooms.Right + gap, gridY),
                Size = new Size(Math.Max(260, Width - (dgvRooms.Right + gap + 40)), gridHeight) // responsive width
            };
            AttachRoundedBorder(pnlAmenities, 12, Color.FromArgb(220, 225, 235));
            Controls.Add(pnlAmenities);

            lblAmenitiesTitle = new Label
            {
                Text = "Amenities",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(14, 12),
                AutoSize = true
            };
            pnlAmenities.Controls.Add(lblAmenitiesTitle);

            lblAmenitiesText = new Label
            {
                Text = "Select a room to view amenities.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(16, 44),
                AutoSize = true,
                MaximumSize = new Size(pnlAmenities.Width - 32, 0) // wrap inside panel
            };
            pnlAmenities.Controls.Add(lblAmenitiesText);

            // Keep amenities panel responsive on control resize
            this.Resize -= AvailableRoomsControl_Resize;
            this.Resize += AvailableRoomsControl_Resize;

            ResumeLayout(false);
            PerformLayout();
        }

        private void AvailableRoomsControl_Resize(object? sender, EventArgs e)
        {
            if (dgvRooms == null || pnlAmenities == null) return;

            // Keep a nice right panel width while grid stays fixed width
            int gap = 20;
            pnlAmenities.Location = new Point(dgvRooms.Right + gap, dgvRooms.Top);
            pnlAmenities.Size = new Size(Math.Max(260, this.Width - (pnlAmenities.Left + 20)), dgvRooms.Height);

            // Re-wrap amenities text
            if (lblAmenitiesText != null)
            {
                lblAmenitiesText.MaximumSize = new Size(pnlAmenities.Width - 32, 0);
            }
        }

        // Populate Type filter (per design)
        private void LoadRoomTypes()
        {
            // Using static list already set in InitializeControls; keep method for symmetry/extension.
            // If you want dynamic types from DB, replace items here accordingly.
        }

        // Load grid with filters applied + update summary
        private void LoadRooms()
        {
            try
            {
                using var conn = _dbService.GetConnection();
                conn.Open();

                var sb = new StringBuilder(@"
                    SELECT 
                        RoomID,
                        RoomNumber,
                        RoomType,
                        PricePerNight,
                        MaxOccupancy,
                        Status,
                        Floor,
                        Amenities
                    FROM Rooms
                    WHERE 1=1");

                using var cmd = new SqlCommand { Connection = conn };

                // Search by RoomNumber
                var term = txtSearch.Text.Trim();
                if (!string.IsNullOrEmpty(term))
                {
                    sb.Append(" AND RoomNumber LIKE @Search");
                    cmd.Parameters.AddWithValue("@Search", $"%{term}%");
                }

                // Status filter (skip when "All Statuses")
                if (cboFilterStatus.SelectedIndex > 0)
                {
                    sb.Append(" AND Status = @Status");
                    cmd.Parameters.AddWithValue("@Status", cboFilterStatus.SelectedItem!.ToString());
                }

                // Type filter (skip when "All Types")
                if (cboFilterType.SelectedIndex > 0)
                {
                    sb.Append(" AND RoomType = @RoomType");
                    cmd.Parameters.AddWithValue("@RoomType", cboFilterType.SelectedItem!.ToString());
                }

                sb.Append(" ORDER BY RoomNumber");
                cmd.CommandText = sb.ToString();

                using var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                dgvRooms.DataSource = dt;

                // Headers and formatting
                if (dgvRooms.Columns.Contains("RoomID"))
                    dgvRooms.Columns["RoomID"].Visible = false;

                if (dgvRooms.Columns.Contains("RoomNumber"))
                {
                    var c = dgvRooms.Columns["RoomNumber"];
                    c.HeaderText = "Room";
                    c.FillWeight = 85; // narrower
                    c.MinimumWidth = 90;
                }

                if (dgvRooms.Columns.Contains("RoomType"))
                {
                    var c = dgvRooms.Columns["RoomType"];
                    c.HeaderText = "Type";
                    c.FillWeight = 80; // narrower
                    c.MinimumWidth = 90;
                }

                if (dgvRooms.Columns.Contains("Floor"))
                {
                    var c = dgvRooms.Columns["Floor"];
                    c.HeaderText = "Floor";
                    c.FillWeight = 60; // narrow
                    c.MinimumWidth = 70;
                }

                if (dgvRooms.Columns.Contains("MaxOccupancy"))
                {
                    var c = dgvRooms.Columns["MaxOccupancy"];
                    c.HeaderText = "Max Guests";
                    c.FillWeight = 90; // narrow
                    c.MinimumWidth = 100;
                }

                if (dgvRooms.Columns.Contains("Status"))
                {
                    var c = dgvRooms.Columns["Status"];
                    c.HeaderText = "Status";
                    c.FillWeight = 110;
                }

                if (dgvRooms.Columns.Contains("PricePerNight"))
                {
                    var c = dgvRooms.Columns["PricePerNight"];
                    c.HeaderText = "Rate/Night";
                    c.DefaultCellStyle.Format = "C2";
                    c.DefaultCellStyle.FormatProvider = _currencyCulture; // ₱
                    c.FillWeight = 120;
                    c.MinimumWidth = 120;
                }

                // Hide Amenities in table; show beautifully on the right panel
                if (dgvRooms.Columns.Contains("Amenities"))
                {
                    dgvRooms.Columns["Amenities"].Visible = false;
                }

                // Update summary
                int totalRooms;
                using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM Rooms", conn))
                {
                    totalRooms = Convert.ToInt32(countCmd.ExecuteScalar());
                }
                lblSummary.Text = $"Showing {dt.Rows.Count} of {totalRooms} rooms";

                // Update amenities view for current selection (if any)
                UpdateAmenitiesFromSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rooms: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvRooms_SelectionChanged(object? sender, EventArgs e)
        {
            UpdateAmenitiesFromSelection();
        }

        private void UpdateAmenitiesFromSelection()
        {
            if (dgvRooms == null || dgvRooms.SelectedRows.Count == 0)
            {
                SetAmenitiesText("Select a room to view amenities.");
                return;
            }

            var row = dgvRooms.SelectedRows[0];
            string? amenitiesRaw = row.Cells["Amenities"]?.Value?.ToString();

            if (string.IsNullOrWhiteSpace(amenitiesRaw))
            {
                SetAmenitiesText("No amenities listed.");
                return;
            }

            // Beautify amenities: support comma-separated or semicolon-separated lists
            SetAmenitiesText(FormatAmenitiesList(amenitiesRaw));
        }

        private void SetAmenitiesText(string text)
        {
            if (lblAmenitiesText == null) return;

            lblAmenitiesText.Text = text;
            // ensure wrapping fits new width
            lblAmenitiesText.MaximumSize = new Size(pnlAmenities.Width - 32, 0);
        }

        private static string FormatAmenitiesList(string raw)
        {
            // Normalize delimiters
            var parts = raw
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder();
            foreach (var p in parts)
            {
                var item = p.Trim();
                if (item.Length == 0) continue;
                sb.AppendLine($"• {item}");
            }

            return sb.Length == 0 ? "No amenities listed." : sb.ToString().TrimEnd();
        }

        // Simple rounded outline like other screens
        private void AttachRoundedBorder(Control ctrl, int radius, Color borderColor)
        {
            ctrl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(borderColor, 1.5f);
                var rect = ctrl.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using var path = GetRoundedRectPath(rect, radius);
                e.Graphics.DrawPath(pen, path);
            };
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
    }
}

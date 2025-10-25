using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using Microsoft.Data.SqlClient; // Use Microsoft.Data.SqlClient only
using HotelMgt.Services;

namespace HotelMgt.UserControls.Employee
{
    public partial class GuestSearchControl : UserControl
    {
        private readonly DatabaseService _dbService;
        private readonly CultureInfo _currencyCulture = CultureInfo.GetCultureInfo("en-PH");

        // UI
        private Label lblTitle = null!, lblSubtitle = null!;
        private TextBox txtSearch = null!;
        private ComboBox cboSearchBy = null!;
        private Button btnSearch = null!;
        private DataGridView dgvGuests = null!;
        private DataGridView dgvGuestHistory = null!;

        public GuestSearchControl()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            this.Load += GuestSearchControl_Load;
        }

        private void GuestSearchControl_Load(object? sender, EventArgs e)
        {
            InitializeControls();
            SearchGuests(); // initial load (Figma: default list)
        }

        private void InitializeControls()
        {
            SuspendLayout();
            Controls.Clear();

            BackColor = Color.White;
            Dock = DockStyle.Fill;

            // Title (Figma: page header)
            lblTitle = new Label
            {
                Text = "Guest Search",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            lblSubtitle = new Label
            {
                Text = "Search guests by name, email or phone. Select a guest to view their history.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Location = new Point(20, 55),
                AutoSize = true
            };
            Controls.Add(lblSubtitle);

            // Search bar (Figma: search bar + filter)
            var y = 95;

            Controls.Add(new Label
            {
                Text = "Search",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, y),
                AutoSize = true
            });
            txtSearch = new TextBox
            {
                Location = new Point(20, y + 22),
                Size = new Size(360, 25),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "e.g., John, john@acme.com, +1 555..."
            };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SearchGuests(); } };
            Controls.Add(txtSearch);

            Controls.Add(new Label
            {
                Text = "Search by",
                Font = new Font("Segoe UI", 9),
                Location = new Point(400, y),
                AutoSize = true
            });
            cboSearchBy = new ComboBox
            {
                Location = new Point(400, y + 22),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // Figma: filter options
            cboSearchBy.Items.AddRange(new object[] { "Name", "Email", "Phone" });
            cboSearchBy.SelectedIndex = 0;
            Controls.Add(cboSearchBy);

            btnSearch = new Button
            {
                Text = "Search",
                Location = new Point(570, y + 20),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += (_, __) => SearchGuests();
            Controls.Add(btnSearch);

            // Guests grid (Figma: left/top table)
            dgvGuests = new DataGridView
            {
                Location = new Point(20, y + 70),
                Size = new Size(650, 250),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, // hide left selection indicator column
                RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing
            };
            dgvGuests.SelectionChanged += DgvGuests_SelectionChanged;
            Controls.Add(dgvGuests);

            // Guest history grid (Figma: bottom table)
            // Inside InitializeControls(), when creating dgvGuestHistory
            dgvGuestHistory = new DataGridView
            {
                Location = new Point(20, y + 340),
                Size = new Size(1100, 300),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, // hide left selection indicator column
                RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing
            };
            Controls.Add(dgvGuestHistory);

            ResumeLayout(false);
            PerformLayout();
        }

        // Figma: search executes with selected predicate
        private void SearchGuests()
        {
            try
            {
                using var conn = _dbService.GetConnection();
                conn.Open();

                var where = new StringBuilder("WHERE 1=1");
                var cmd = new SqlCommand { Connection = conn };

                string term = txtSearch.Text.Trim();
                string mode = (cboSearchBy.SelectedItem?.ToString() ?? "Name").ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    switch (mode)
                    {
                        case "email":
                            where.Append(" AND Email LIKE @Search");
                            cmd.Parameters.AddWithValue("@Search", $"%{term}%");
                            break;
                        case "phone":
                            where.Append(" AND PhoneNumber LIKE @Search");
                            cmd.Parameters.AddWithValue("@Search", $"%{term}%");
                            break;
                        default: // name
                            where.Append(" AND (FirstName LIKE @Name OR LastName LIKE @Name OR (FirstName + ' ' + LastName) LIKE @Name)");
                            cmd.Parameters.AddWithValue("@Name", $"%{term}%");
                            break;
                    }
                }

                cmd.CommandText = $@"
                    SELECT 
                        GuestID,
                        (FirstName + ' ' + LastName) AS GuestName,
                        Email,
                        PhoneNumber,
                        CreatedAt
                    FROM Guests
                    {where}
                    ORDER BY CreatedAt DESC";

                using var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                dgvGuests.DataSource = dt;

                if (dgvGuests.Columns.Contains("GuestID"))
                    dgvGuests.Columns["GuestID"].Visible = false;
                if (dgvGuests.Columns.Contains("GuestName"))
                    dgvGuests.Columns["GuestName"].HeaderText = "Guest";
                if (dgvGuests.Columns.Contains("Email"))
                    dgvGuests.Columns["Email"].HeaderText = "Email";
                if (dgvGuests.Columns.Contains("PhoneNumber"))
                    dgvGuests.Columns["PhoneNumber"].HeaderText = "Phone";
                if (dgvGuests.Columns.Contains("CreatedAt"))
                    dgvGuests.Columns["CreatedAt"].HeaderText = "Created";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching guests: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Figma: selecting a guest loads combined Reservations + CheckIns history
        private void DgvGuests_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvGuests.SelectedRows.Count == 0) return;
            var row = dgvGuests.SelectedRows[0];
            if (row.Cells["GuestID"].Value is null) return;

            int guestId = Convert.ToInt32(row.Cells["GuestID"].Value);
            ShowGuestHistory(guestId);
        }

        private void ShowGuestHistory(int guestId)
        {
            try
            {
                using var conn = _dbService.GetConnection();
                conn.Open();

                // Combine Reservations and CheckIns history (Figma: unified timeline)
                string query = @"
                    SELECT 
                        'Reservation' AS [Type],
                        r.ReservationID AS [RefId],
                        rm.RoomNumber,
                        r.CheckInDate,
                        r.CheckOutDate,
                        r.ReservationStatus AS [Status],
                        r.TotalAmount AS [Amount]
                    FROM Reservations r
                    INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                    WHERE r.GuestID = @GuestID

                    UNION ALL

                    SELECT 
                        'Stay' AS [Type],
                        c.CheckInID AS [RefId],
                        rm.RoomNumber,
                        CAST(c.CheckInDateTime AS DATE) AS CheckInDate,
                        c.ExpectedCheckOutDate AS CheckOutDate,
                        CASE WHEN c.ActualCheckOutDateTime IS NULL THEN 'Active' ELSE 'Completed' END AS [Status],
                        CAST(DATEDIFF(DAY, CAST(c.CheckInDateTime AS DATE), ISNULL(CAST(c.ActualCheckOutDateTime AS DATE), CAST(GETDATE() AS DATE))) AS DECIMAL(10,2)) * rm.PricePerNight AS [Amount]
                    FROM CheckIns c
                    INNER JOIN Rooms rm ON c.RoomID = rm.RoomID
                    WHERE c.GuestID = @GuestID

                    ORDER BY CheckInDate DESC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@GuestID", guestId);

                using var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                dgvGuestHistory.DataSource = dt;

                if (dgvGuestHistory.Columns.Contains("RefId"))
                    dgvGuestHistory.Columns["RefId"].Visible = false;
                if (dgvGuestHistory.Columns.Contains("Type"))
                    dgvGuestHistory.Columns["Type"].HeaderText = "Type";
                if (dgvGuestHistory.Columns.Contains("RoomNumber"))
                    dgvGuestHistory.Columns["RoomNumber"].HeaderText = "Room";
                if (dgvGuestHistory.Columns.Contains("CheckInDate"))
                    dgvGuestHistory.Columns["CheckInDate"].HeaderText = "Check-In";
                if (dgvGuestHistory.Columns.Contains("CheckOutDate"))
                    dgvGuestHistory.Columns["CheckOutDate"].HeaderText = "Check-Out";
                if (dgvGuestHistory.Columns.Contains("Status"))
                    dgvGuestHistory.Columns["Status"].HeaderText = "Status";
                if (dgvGuestHistory.Columns.Contains("Amount"))
                {
                    dgvGuestHistory.Columns["Amount"].HeaderText = "Amount";
                    dgvGuestHistory.Columns["Amount"].DefaultCellStyle.Format = "C2";
                    dgvGuestHistory.Columns["Amount"].DefaultCellStyle.FormatProvider = _currencyCulture; // ensure ₱
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading guest history: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

using System;
using System.Data;
using Microsoft.Data.SqlClient; // Use Microsoft.Data.SqlClient only
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using HotelMgt.Services;
using HotelMgt.Utilities;

namespace HotelMgt.UserControls.Employee
{
    public partial class CheckOutControl : UserControl
    {
        private readonly DatabaseService _dbService;
        private readonly ActivityLogService _logService;

        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private DataGridView dgvActiveCheckIns = null!;
        private Panel panelCheckOutDetails = null!;
        private Label lblGuestName = null!;
        private Label lblRoomInfo = null!;
        private Label lblCheckInDate = null!;
        private Label lblCheckOutDate = null!; // actual check-out date (today)
        private Label lblExpectedCheckOutDate = null!; // expected check-out date from DB
        private Label lblNights = null!;
        private Label lblRatePerNight = null!;
        private Label lblTotalAmount = null!;
        private ComboBox cboPaymentMethod = null!;
        private TextBox txtTransactionRef = null!;
        private Button btnProcessCheckOut = null!;

        private int selectedCheckInId = 0; // This is CheckIns.CheckInID
        private decimal totalAmount = 0;

        // Use Philippine Peso for all currency formatting
        private readonly CultureInfo _currencyCulture = CultureInfo.GetCultureInfo("en-PH");

        public CheckOutControl()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            _logService = new ActivityLogService();
            this.Load += CheckOutControl_Load;
        }

        private void CheckOutControl_Load(object? sender, EventArgs e)
        {
            InitializeControls();
            LoadActiveCheckIns();

            // Refresh when user switches to this tab
            this.VisibleChanged -= CheckOutControl_VisibleChanged;
            this.VisibleChanged += CheckOutControl_VisibleChanged;
        }

        // Add this handler inside the class
        private void CheckOutControl_VisibleChanged(object? sender, EventArgs e)
        {
            if (this.Visible)
            {
                LoadActiveCheckIns(txtSearch?.Text.Trim() ?? string.Empty);
            }
        }

        private void InitializeControls()
        {
            BackColor = Color.FromArgb(240, 244, 248);
            Dock = DockStyle.Fill;

            // Title
            var lblTitle = new Label
            {
                Text = "Guest Check-Out",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Process checkout and final payment",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Location = new Point(20, 55),
                AutoSize = true
            };
            Controls.Add(lblSubtitle);

            // Search
            var lblSearch = new Label
            {
                Text = "Search Check-In:",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 100),
                AutoSize = true
            };
            Controls.Add(lblSearch);

            txtSearch = new TextBox
            {
                Location = new Point(20, 125),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Room number or guest name"
            };
            Controls.Add(txtSearch);

            btnSearch = new Button
            {
                Text = "Search",
                Location = new Point(430, 125),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += BtnSearch_Click;
            Controls.Add(btnSearch);

            // Grid
            dgvActiveCheckIns = new DataGridView
            {
                Location = new Point(20, 170),
                Size = new Size(1200, 150),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                DefaultCellStyle = { WrapMode = DataGridViewTriState.True },
                RowHeadersVisible = false, // hide left selection indicator column
                RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing
            };
            dgvActiveCheckIns.SelectionChanged += DgvActiveCheckIns_SelectionChanged;
            Controls.Add(dgvActiveCheckIns);

            // Details panel (scrollable and taller so content isn't clipped)
            panelCheckOutDetails = new Panel
            {
                Location = new Point(20, 340),  // 170 + 150 + 20 spacing
                Size = new Size(1200, 420),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                AutoScroll = true,
                AutoScrollMargin = new Size(0, 12)
            };
            Controls.Add(panelCheckOutDetails);

            CreateCheckOutDetailsPanel();
        }

        private void CreateCheckOutDetailsPanel()
        {
            var lblPanelTitle = new Label
            {
                Text = "Check-Out Details",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true
            };
            panelCheckOutDetails.Controls.Add(lblPanelTitle);

            int yPos = 55;

            CreateDetailRow(panelCheckOutDetails, "Guest:", out lblGuestName, 20, yPos);
            yPos += 30;
            CreateDetailRow(panelCheckOutDetails, "Room:", out lblRoomInfo, 20, yPos);
            yPos += 30;
            CreateDetailRow(panelCheckOutDetails, "Check-In Date:", out lblCheckInDate, 20, yPos);
            yPos += 30;
            CreateDetailRow(panelCheckOutDetails, "Check-Out Date:", out lblCheckOutDate, 20, yPos); // actual (today)
            yPos += 30;
            CreateDetailRow(panelCheckOutDetails, "Expected Check-Out Date:", out lblExpectedCheckOutDate, 20, yPos); // expected from DB
            yPos += 30;
            CreateDetailRow(panelCheckOutDetails, "Nights Stayed:", out lblNights, 20, yPos);
            yPos += 30;
            CreateDetailRow(panelCheckOutDetails, "Rate Per Night:", out lblRatePerNight, 20, yPos);
            yPos += 30;
            CreateDetailRow(panelCheckOutDetails, "Total Amount:", out lblTotalAmount, 20, yPos);
            yPos += 40;

            var lblPayment = new Label
            {
                Text = "Payment Method *",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, yPos),
                AutoSize = true
            };
            panelCheckOutDetails.Controls.Add(lblPayment);

            cboPaymentMethod = new ComboBox
            {
                Location = new Point(20, yPos + 22),
                Size = new Size(300, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            cboPaymentMethod.Items.AddRange(new object[] { "Cash", "Credit Card", "Debit Card" });
            cboPaymentMethod.SelectedIndex = 0;
            panelCheckOutDetails.Controls.Add(cboPaymentMethod);

            var lblTransRef = new Label
            {
                Text = "Transaction Reference",
                Font = new Font("Segoe UI", 9),
                Location = new Point(340, yPos),
                AutoSize = true
            };
            panelCheckOutDetails.Controls.Add(lblTransRef);

            txtTransactionRef = new TextBox
            {
                Location = new Point(340, yPos + 22),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10)
            };
            panelCheckOutDetails.Controls.Add(txtTransactionRef);

            btnProcessCheckOut = new Button
            {
                Text = "Process Check-Out",
                Location = new Point(20, yPos + 60),
                Size = new Size(620, 45),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnProcessCheckOut.FlatAppearance.BorderSize = 0;
            btnProcessCheckOut.Click += BtnProcessCheckOut_Click;
            panelCheckOutDetails.Controls.Add(btnProcessCheckOut);
        }

        private void CreateDetailRow(Panel parent, string label, out Label valueLabel, int x, int y)
        {
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(x, y),
                Size = new Size(150, 20)
            };
            parent.Controls.Add(lbl);

            valueLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(x + 155, y),
                Size = new Size(450, 20)
            };
            parent.Controls.Add(valueLabel);
        }

        private void LoadActiveCheckIns(string searchTerm = "")
        {
            try
            {
                using var conn = _dbService.GetConnection();
                conn.Open();

                // Show ALL active check-ins (ActualCheckOutDateTime is null) without filtering by room status.
                string query = @"
                    SELECT 
                        ci.CheckInID AS CheckInId,
                        rm.RoomNumber,
                        g.FirstName + ' ' + g.LastName AS GuestName,
                        CAST(ci.CheckInDateTime AS DATE) AS CheckInDate,
                        ci.ExpectedCheckOutDate,
                        ci.NumberOfGuests,
                        rm.PricePerNight
                    FROM CheckIns ci
                    INNER JOIN Rooms rm ON ci.RoomID = rm.RoomID
                    INNER JOIN Guests g ON ci.GuestID = g.GuestID
                    WHERE ci.ActualCheckOutDateTime IS NULL";

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query += @" AND (rm.RoomNumber LIKE @Search OR g.FirstName + ' ' + g.LastName LIKE @Search)";
                }

                query += " ORDER BY ci.CheckInDateTime DESC";

                using var cmd = new SqlCommand(query, conn);
                if (!string.IsNullOrWhiteSpace(searchTerm))
                    cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

                using var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                dgvActiveCheckIns.DataSource = dt;

                if (dgvActiveCheckIns.Columns.Contains("CheckInId"))
                    dgvActiveCheckIns.Columns["CheckInId"].Visible = false;
                if (dgvActiveCheckIns.Columns.Contains("RoomNumber"))
                    dgvActiveCheckIns.Columns["RoomNumber"].HeaderText = "Room";
                if (dgvActiveCheckIns.Columns.Contains("GuestName"))
                    dgvActiveCheckIns.Columns["GuestName"].HeaderText = "Guest Name";
                if (dgvActiveCheckIns.Columns.Contains("CheckInDate"))
                    dgvActiveCheckIns.Columns["CheckInDate"].HeaderText = "Check-In";
                if (dgvActiveCheckIns.Columns.Contains("ExpectedCheckOutDate"))
                    dgvActiveCheckIns.Columns["ExpectedCheckOutDate"].HeaderText = "Expected Out";
                if (dgvActiveCheckIns.Columns.Contains("NumberOfGuests"))
                    dgvActiveCheckIns.Columns["NumberOfGuests"].HeaderText = "Guests";
                if (dgvActiveCheckIns.Columns.Contains("PricePerNight"))
                {
                    dgvActiveCheckIns.Columns["PricePerNight"].HeaderText = "Rate/Night";
                    dgvActiveCheckIns.Columns["PricePerNight"].DefaultCellStyle.Format = "C2";
                    dgvActiveCheckIns.Columns["PricePerNight"].DefaultCellStyle.FormatProvider = _currencyCulture;
                }

                // Ensure currency provider applies to all cells that use "C"
                dgvActiveCheckIns.DefaultCellStyle.FormatProvider = _currencyCulture;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading check-ins: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSearch_Click(object? sender, EventArgs e)
        {
            LoadActiveCheckIns(txtSearch.Text.Trim());
        }

        private void DgvActiveCheckIns_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvActiveCheckIns.SelectedRows.Count == 0) return;

            var row = dgvActiveCheckIns.SelectedRows[0];
            if (row.Cells["CheckInId"].Value is null) return;

            selectedCheckInId = Convert.ToInt32(row.Cells["CheckInId"].Value);
            string guestName = row.Cells["GuestName"].Value?.ToString() ?? "";
            string roomNumber = row.Cells["RoomNumber"].Value?.ToString() ?? "";

            DateTime checkInDate = Convert.ToDateTime(row.Cells["CheckInDate"].Value).Date;

            // Expected from DB (can be empty/null, handle gracefully)
            DateTime? expectedCheckOutDate = null;
            var expectedOutCell = row.Cells["ExpectedCheckOutDate"]?.Value;
            if (expectedOutCell is not null && expectedOutCell != DBNull.Value)
            {
                expectedCheckOutDate = Convert.ToDateTime(expectedOutCell).Date;
            }

            // Actual (today)
            DateTime checkOutDate = DateTime.Today;

            decimal ratePerNight = Convert.ToDecimal(row.Cells["PricePerNight"].Value);

            // Nights = actual stay: today - check-in; minimum 1
            int nightsStayed = Math.Max(1, (checkOutDate - checkInDate).Days);

            totalAmount = nightsStayed * ratePerNight;

            lblGuestName.Text = guestName;
            lblRoomInfo.Text = $"Room {roomNumber}";
            lblCheckInDate.Text = checkInDate.ToString("yyyy-MM-dd");
            lblCheckOutDate.Text = checkOutDate.ToString("yyyy-MM-dd");
            lblExpectedCheckOutDate.Text = expectedCheckOutDate?.ToString("yyyy-MM-dd") ?? "-";
            lblNights.Text = nightsStayed.ToString();
            lblRatePerNight.Text = ratePerNight.ToString("C2", _currencyCulture);
            lblTotalAmount.Text = totalAmount.ToString("C2", _currencyCulture);

            panelCheckOutDetails.Visible = true;
        }

        private void BtnProcessCheckOut_Click(object? sender, EventArgs e)
        {
            if (selectedCheckInId == 0)
            {
                MessageBox.Show("Please select a check-in to process.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboPaymentMethod.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a payment method.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Process check-out with payment of {totalAmount.ToString("C2", _currencyCulture)}?",
                "Confirm Check-Out",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            if (confirm != DialogResult.Yes) return;

            try
            {
                using var conn = _dbService.GetConnection();
                conn.Open();
                using var tx = conn.BeginTransaction();

                try
                {
                    // Fetch needed data from CheckIns for Payments insert
                    int roomId, reservationId, guestId;
                    using (var cmd = new SqlCommand(
                        "SELECT RoomId, ReservationID, GuestID FROM CheckIns WHERE CheckInId = @Id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", selectedCheckInId);
                        using var rdr = cmd.ExecuteReader();
                        if (!rdr.Read())
                            throw new InvalidOperationException("Selected check-in not found.");

                        roomId = rdr.GetInt32(0);
                        reservationId = rdr.GetInt32(1);
                        guestId = rdr.GetInt32(2);
                    }

                    // Mark check-out
                    using (var cmd = new SqlCommand(@"
                        UPDATE CheckIns 
                        SET ActualCheckOutDateTime = @Out
                        WHERE CheckInId = @Id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Out", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Id", selectedCheckInId);
                        cmd.ExecuteNonQuery();
                    }

                    // Insert payment (aligns with Payments schema)
                    using (var cmd = new SqlCommand(@"
                        INSERT INTO Payments (ReservationID, GuestID, EmployeeID, PaymentDate, Amount, PaymentMethod, TransactionReference)
                        VALUES (@ReservationID, @GuestID, @EmployeeID, @PaymentDate, @Amount, @PaymentMethod, @TransactionReference)", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@ReservationID", reservationId);
                        cmd.Parameters.AddWithValue("@GuestID", guestId);
                        cmd.Parameters.AddWithValue("@EmployeeID", CurrentUser.EmployeeId);
                        cmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Amount", totalAmount);
                        cmd.Parameters.AddWithValue("@PaymentMethod", cboPaymentMethod.SelectedItem!.ToString());
                        cmd.Parameters.AddWithValue("@TransactionReference",
                            string.IsNullOrWhiteSpace(txtTransactionRef.Text) ? (object)DBNull.Value : txtTransactionRef.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }

                    // Make room Available after checkout (valid per CHECK constraint)
                    using (var cmd = new SqlCommand(@"
                        UPDATE Rooms SET Status = 'Available', UpdatedAt = @Now
                        WHERE RoomId = @RoomId", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Now", DateTime.Now);
                        cmd.Parameters.AddWithValue("@RoomId", roomId);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }

                // Log activity after successful commit
                try
                {
                    _logService.LogActivity(
                        CurrentUser.EmployeeId,
                        "CheckOut",
                        $"Guest {lblGuestName.Text} checked out from {lblRoomInfo.Text}. Payment: {totalAmount.ToString("C2", _currencyCulture)}",
                        selectedCheckInId
                    );
                }
                catch { /* don't block UX on log failure */ }

                MessageBox.Show(
                    $"Check-out processed successfully!\nTotal payment: {totalAmount.ToString("C2", _currencyCulture)}",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                panelCheckOutDetails.Visible = false;
                selectedCheckInId = 0;
                LoadActiveCheckIns();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing check-out: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

using System;
using System.Data;
using Microsoft.Data.SqlClient; // Use Microsoft.Data.SqlClient only
using System.Drawing;
using System.Windows.Forms;
using HotelMgt.Models;
using HotelMgt.Services;
using HotelMgt.Utilities;

namespace HotelMgt.UserControls.Employee
{
    public partial class CheckInControl : UserControl
    {
        private readonly DatabaseService _dbService;
        private readonly ActivityLogService _logService;

        // Walk-In Controls
        private TextBox txtFirstName;
        private TextBox txtLastName;
        private TextBox txtEmail;
        private TextBox txtPhone;
        private ComboBox cboIDType;
        private TextBox txtIDNumber;
        private ComboBox cboRoom;
        private NumericUpDown numGuests;
        private DateTimePicker dtpCheckOut;
        private TextBox txtNotes;
        private Button btnCheckIn;

        // Reservation Controls
        private ComboBox cboReservation;
        private Panel panelReservationDetails;
        private Label lblResGuestName;
        private Label lblResRoomNumber;
        private Label lblResCheckIn;
        private Label lblResCheckOut;
        private Label lblResGuests;
        private Label lblResTotal;
        private Label lblResSpecialRequests;
        private Button btnCheckInReservation;

        // NEW: Inline info when no reservations exist (instead of MessageBox)
        private Label? lblNoPendingReservations;

        // NEW: Inline info when no rooms are available (Walk‑In)
        private Label? lblNoAvailableRooms;

        public CheckInControl()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            _logService = new ActivityLogService();

            InitializeCustomControls();

            // Ensure the Load handler runs so tabs get populated
            this.Load += CheckInControl_Load;
        }

        private void InitializeCustomControls()
        {
            // This will be called after InitializeComponent()
            // Create all controls programmatically or use designer

            // Ensure the TabControl fills the user control
            if (tabCheckInType != null)
            {
                tabCheckInType.Dock = DockStyle.Fill;
                tabCheckInType.TabPages.Clear();

                TabPage walkInTab = new TabPage("Walk-In");
                TabPage reservationTab = new TabPage("Reservation");
                tabCheckInType.TabPages.Add(walkInTab);
                tabCheckInType.TabPages.Add(reservationTab);
            }
        }

        private void CheckInControl_Load(object sender, EventArgs e)
        {
            // Guard if the TabControl wasn't created by designer for some reason
            if (tabCheckInType == null || tabCheckInType.TabPages.Count < 2)
                return;

            InitializeWalkInTab();
            InitializeReservationTab();
            LoadAvailableRooms();
            LoadPendingReservations();
        }

        #region Walk-In Tab Initialization

        private void InitializeWalkInTab()
        {
            var walkInTab = tabCheckInType.TabPages[0];
            walkInTab.BackColor = Color.White;
            walkInTab.AutoScroll = true;

            // Container Panel
            Panel container = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(1100, 550),
                AutoScroll = true,
                BackColor = Color.White
            };
            walkInTab.Controls.Clear();
            walkInTab.Controls.Add(container);

            int yPos = 10;

            // Guest Information Section
            Label lblGuestInfo = CreateSectionLabel("Guest Information", 10, yPos);
            container.Controls.Add(lblGuestInfo);
            yPos += 40;

            // First Name
            Label lblFirstName = new Label
            {
                Text = "First Name *",
                Location = new Point(10, yPos),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9)
            };
            container.Controls.Add(lblFirstName);

            txtFirstName = new TextBox
            {
                Location = new Point(10, yPos + 22),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10)
            };
            container.Controls.Add(txtFirstName);

            // Last Name
            Label lblLastName = new Label
            {
                Text = "Last Name *",
                Location = new Point(280, yPos),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9)
            };
            container.Controls.Add(lblLastName);

            txtLastName = new TextBox
            {
                Location = new Point(280, yPos + 22),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10)
            };
            container.Controls.Add(txtLastName);
            yPos += 60;

            // Email
            Label lblEmail = new Label
            {
                Text = "Email",
                Location = new Point(10, yPos),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9)
            };
            container.Controls.Add(lblEmail);

            txtEmail = new TextBox
            {
                Location = new Point(10, yPos + 22),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10)
            };
            container.Controls.Add(txtEmail);

            // Phone
            Label lblPhone = new Label
            {
                Text = "Phone Number *",
                Location = new Point(280, yPos),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9)
            };
            container.Controls.Add(lblPhone);

            txtPhone = new TextBox
            {
                Location = new Point(280, yPos + 22),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10)
            };
            container.Controls.Add(txtPhone);
            yPos += 60;

            // ID Type
            Label lblIDType = new Label
            {
                Text = "ID Type *",
                Location = new Point(10, yPos),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9)
            };
            container.Controls.Add(lblIDType);

            cboIDType = new ComboBox
            {
                Location = new Point(10, yPos + 22),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboIDType.Items.AddRange(new object[] { "Passport", "Driver License", "National ID" });
            container.Controls.Add(cboIDType);

            // ID Number
            Label lblIDNumber = new Label
            {
                Text = "ID Number *",
                Location = new Point(280, yPos),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9)
            };
            container.Controls.Add(lblIDNumber);

            txtIDNumber = new TextBox
            {
                Location = new Point(280, yPos + 22),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10)
            };
            container.Controls.Add(txtIDNumber);
            yPos += 60;

            // Room & Stay Information Section
            Label lblStayInfo = CreateSectionLabel("Room & Stay Information", 10, yPos);
            container.Controls.Add(lblStayInfo);
            yPos += 40;

            // Select Room
            Label lblRoom = new Label
            {
                Text = "Select Room *",
                Location = new Point(10, yPos),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9)
            };
            container.Controls.Add(lblRoom);

            cboRoom = new ComboBox
            {
                Location = new Point(10, yPos + 22),
                Size = new Size(520, 25),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "DisplayText",
                ValueMember = "RoomId"
            };
            container.Controls.Add(cboRoom);

            // NEW: inline info when no rooms are available (hidden by default)
            lblNoAvailableRooms = new Label
            {
                Text = "No rooms available at the moment.",
                Location = new Point(10, yPos + 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                Visible = false
            };
            container.Controls.Add(lblNoAvailableRooms);

            yPos += 60;

            // Number of Guests
            Label lblGuests = new Label
            {
                Text = "Number of Guests *",
                Location = new Point(10, yPos),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9)
            };
            container.Controls.Add(lblGuests);

            numGuests = new NumericUpDown
            {
                Location = new Point(10, yPos + 22),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10),
                Minimum = 1,
                Maximum = 10,
                Value = 1
            };
            container.Controls.Add(numGuests);

            // Check-Out Date
            Label lblCheckOut = new Label
            {
                Text = "Expected Check-Out Date *",
                Location = new Point(280, yPos),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 9)
            };
            container.Controls.Add(lblCheckOut);

            dtpCheckOut = new DateTimePicker
            {
                Location = new Point(280, yPos + 22),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10),
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today.AddDays(1)
            };
            container.Controls.Add(dtpCheckOut);
            yPos += 60;

            // Notes
            Label lblNotes = new Label
            {
                Text = "Special Requests / Notes",
                Location = new Point(10, yPos),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9)
            };
            container.Controls.Add(lblNotes);

            txtNotes = new TextBox
            {
                Location = new Point(10, yPos + 22),
                Size = new Size(520, 60),
                Font = new Font("Segoe UI", 10),
                Multiline = true
            };
            container.Controls.Add(txtNotes);
            yPos += 95;

            // Check-In Button
            btnCheckIn = new Button
            {
                Text = "Complete Check-In",
                Location = new Point(10, yPos),
                Size = new Size(520, 45),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCheckIn.FlatAppearance.BorderSize = 0;
            btnCheckIn.Click += BtnCheckIn_Click;
            container.Controls.Add(btnCheckIn);
        }

        #endregion

        #region Reservation Tab Initialization

        private void InitializeReservationTab()
        {
            var reservationTab = tabCheckInType.TabPages[1];
            reservationTab.BackColor = Color.White;

            Panel container = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(1100, 550),
                BackColor = Color.White
            };
            reservationTab.Controls.Clear();
            reservationTab.Controls.Add(container);

            // Select Reservation
            Label lblSelectRes = new Label
            {
                Text = "Select Reservation *",
                Location = new Point(10, 10),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9)
            };
            container.Controls.Add(lblSelectRes);

            cboReservation = new ComboBox
            {
                Location = new Point(10, 35),
                Size = new Size(700, 25),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "DisplayText",
                ValueMember = "ReservationID"
            };
            cboReservation.SelectedIndexChanged += CboReservation_SelectedIndexChanged;
            container.Controls.Add(cboReservation);

            // NEW: info label shown when there are no reservations (hidden by default)
            lblNoPendingReservations = new Label
            {
                Text = "No reservations pending check-in.",
                Location = new Point(10, 38),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                Visible = false
            };
            container.Controls.Add(lblNoPendingReservations);

            // Reservation Details Panel
            panelReservationDetails = new Panel
            {
                Location = new Point(10, 80),
                Size = new Size(700, 350),
                BackColor = Color.FromArgb(249, 250, 251),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            container.Controls.Add(panelReservationDetails);

            // Details labels
            Label lblDetailsTitle = new Label
            {
                Text = "Reservation Details",
                Location = new Point(15, 15),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            panelReservationDetails.Controls.Add(lblDetailsTitle);

            int yPos = 50;
            CreateDetailRow(panelReservationDetails, "Guest:", out lblResGuestName, 15, yPos);
            yPos += 30;
            CreateDetailRow(panelReservationDetails, "Room:", out lblResRoomNumber, 15, yPos);
            yPos += 30;
            CreateDetailRow(panelReservationDetails, "Check-In Date:", out lblResCheckIn, 15, yPos);
            yPos += 30;
            CreateDetailRow(panelReservationDetails, "Check-Out Date:", out lblResCheckOut, 15, yPos);
            yPos += 30;
            CreateDetailRow(panelReservationDetails, "Number of Guests:", out lblResGuests, 15, yPos);
            yPos += 30;
            CreateDetailRow(panelReservationDetails, "Total Amount:", out lblResTotal, 15, yPos);
            yPos += 30;
            CreateDetailRow(panelReservationDetails, "Special Requests:", out lblResSpecialRequests, 15, yPos);

            // Check-In Button for Reservation
            btnCheckInReservation = new Button
            {
                Text = "Complete Check-In",
                Location = new Point(10, 450),
                Size = new Size(700, 45),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnCheckInReservation.FlatAppearance.BorderSize = 0;
            btnCheckInReservation.Click += BtnCheckInReservation_Click;
            container.Controls.Add(btnCheckInReservation);
        }

        #endregion

        #region Helper Methods

        private Label CreateSectionLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
        }

        private void CreateDetailRow(Panel parent, string labelText, out Label valueLabel, int x, int y)
        {
            Label label = new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            parent.Controls.Add(label);

            valueLabel = new Label
            {
                Text = "",
                Location = new Point(x + 185, y),
                Size = new Size(480, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            parent.Controls.Add(valueLabel);
        }

        #endregion

        #region Load Data Methods

        private void LoadAvailableRooms()
        {
            try
            {
                cboRoom.Items.Clear();

                using (var conn = _dbService.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT RoomID, RoomNumber, RoomType, PricePerNight, MaxOccupancy
                        FROM Rooms
                        WHERE Status = 'Available'
                        ORDER BY RoomNumber";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var room = new
                            {
                                RoomId = reader.GetInt32(0),
                                RoomNumber = reader.GetString(1),
                                RoomType = reader.GetString(2),
                                PricePerNight = reader.GetDecimal(3),
                                MaxOccupancy = reader.GetInt32(4),
                                DisplayText = $"Room {reader.GetString(1)} - {reader.GetString(2)} (${reader.GetDecimal(3):F2}/night) - Max {reader.GetInt32(4)} guests"
                            };

                            cboRoom.Items.Add(room);
                        }
                    }
                }

                // Inline handling (no MessageBox)
                bool hasRooms = cboRoom.Items.Count > 0;

                if (lblNoAvailableRooms != null)
                    lblNoAvailableRooms.Visible = !hasRooms;

                cboRoom.Visible = hasRooms;
                cboRoom.Enabled = hasRooms;
                if (!hasRooms) cboRoom.SelectedIndex = -1;

                // Disable/enable dependent inputs
                numGuests.Enabled = hasRooms;
                dtpCheckOut.Enabled = hasRooms;
                txtNotes.Enabled = hasRooms;
                btnCheckIn.Enabled = hasRooms;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading available rooms: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void LoadPendingReservations()
        {
            try
            {
                cboReservation.Items.Clear();

                using (var conn = _dbService.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            r.ReservationID,
                            r.RoomID,
                            r.GuestID,
                            g.FirstName + ' ' + g.LastName AS GuestName,
                            rm.RoomNumber,
                            r.CheckInDate,
                            r.CheckOutDate,
                            r.NumberOfGuests,
                            r.TotalAmount,
                            r.SpecialRequests
                        FROM Reservations r
                        INNER JOIN Guests g ON r.GuestID = g.GuestID
                        INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                        WHERE r.ReservationStatus = 'Confirmed'
                          AND r.CheckInDate <= CAST(GETDATE() AS DATE)
                          AND r.CheckInDate >= DATEADD(DAY, -1, CAST(GETDATE() AS DATE))
                        ORDER BY r.CheckInDate";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var reservation = new
                            {
                                ReservationId = reader.GetInt32(0),
                                RoomId       = reader.GetInt32(1),
                                GuestId      = reader.GetInt32(2),
                                GuestName    = reader.GetString(3),
                                RoomNumber   = reader.GetString(4),
                                CheckInDate  = reader.GetDateTime(5),
                                CheckOutDate = reader.GetDateTime(6),
                                NumberOfGuests = reader.GetInt32(7),
                                TotalAmount    = reader.GetDecimal(8),
                                SpecialRequests = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                DisplayText = $"{reader.GetString(3)} - Room {reader.GetString(4)} - {reader.GetDateTime(5):yyyy-MM-dd}"
                            };

                            cboReservation.Items.Add(reservation);
                        }
                    }
                }

                // NEW: No MessageBox. Show/Hide inline info and toggle related controls.
                if (cboReservation.Items.Count == 0)
                {
                    if (lblNoPendingReservations != null)
                        lblNoPendingReservations.Visible = true;

                    cboReservation.Visible = false;
                    panelReservationDetails.Visible = false;
                    btnCheckInReservation.Enabled = false;
                }
                else
                {
                    if (lblNoPendingReservations != null)
                        lblNoPendingReservations.Visible = false;

                    cboReservation.Visible = true;
                    cboReservation.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading reservations: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        #endregion

        #region Event Handlers

        private void CboReservation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboReservation.SelectedItem != null)
            {
                dynamic reservation = cboReservation.SelectedItem;

                lblResGuestName.Text = reservation.GuestName;
                lblResRoomNumber.Text = $"Room {reservation.RoomNumber}";
                lblResCheckIn.Text = ((DateTime)reservation.CheckInDate).ToString("yyyy-MM-dd");
                lblResCheckOut.Text = ((DateTime)reservation.CheckOutDate).ToString("yyyy-MM-dd");
                lblResGuests.Text = reservation.NumberOfGuests.ToString();
                lblResTotal.Text = $"{reservation.TotalAmount:C2}";
                lblResSpecialRequests.Text = string.IsNullOrEmpty(reservation.SpecialRequests) ? "None" : reservation.SpecialRequests;

                panelReservationDetails.Visible = true;
                btnCheckInReservation.Enabled = true;
            }
            else
            {
                panelReservationDetails.Visible = false;
                btnCheckInReservation.Enabled = false;
            }
        }

        private void BtnCheckIn_Click(object sender, EventArgs e)
        {
            // Walk-In check-in logic
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                    string.IsNullOrWhiteSpace(txtLastName.Text) ||
                    string.IsNullOrWhiteSpace(txtPhone.Text) ||
                    cboIDType.SelectedItem == null ||
                    string.IsNullOrWhiteSpace(txtIDNumber.Text) ||
                    cboRoom.SelectedItem == null ||
                    numGuests.Value <= 0)
                {
                    MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Proceed with check-in
                using (var conn = _dbService.GetConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            int checkInId;

                            // 1) Create a CheckIns row
                            string insertCheckIn = @"
                                INSERT INTO CheckIns
                                    (ReservationID, GuestID, RoomID, EmployeeID, CheckInDateTime, ExpectedCheckOutDate, NumberOfGuests, Notes, CreatedAt)
                                VALUES
                                    (NULL, @GuestID, @RoomID, @EmployeeID, @CheckInDateTime, @ExpectedCheckOutDate, @NumberOfGuests, @Notes, @CreatedAt);
                                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            using (var cmd = new SqlCommand(insertCheckIn, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@GuestID", CurrentUser.EmployeeId);
                                cmd.Parameters.AddWithValue("@RoomID", ((dynamic)cboRoom.SelectedItem).RoomId);
                                cmd.Parameters.AddWithValue("@EmployeeID", CurrentUser.EmployeeId);
                                cmd.Parameters.AddWithValue("@CheckInDateTime", DateTime.Now);
                                cmd.Parameters.AddWithValue("@ExpectedCheckOutDate", dtpCheckOut.Value);
                                cmd.Parameters.AddWithValue("@NumberOfGuests", (int)numGuests.Value);
                                cmd.Parameters.AddWithValue("@Notes", txtNotes.Text);
                                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                                checkInId = (int)cmd.ExecuteScalar();
                            }

                            // 2) Update room status to Occupied
                            UpdateRoomStatus(conn, transaction, ((dynamic)cboRoom.SelectedItem).RoomId, "Occupied");

                            transaction.Commit();

                            // Log the check-in activity
                            _logService.LogActivity(
                                CurrentUser.EmployeeId,
                                "CheckIn",
                                $"Walk-in guest {txtFirstName.Text} {txtLastName.Text} checked into Room {((dynamic)cboRoom.SelectedItem).RoomNumber}",
                                checkInId
                            );

                            MessageBox.Show("Check-in successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Clear input fields and reset UI
                            ClearWalkInFields();
                            LoadAvailableRooms();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Error during check-in: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Replace BtnCheckInReservation_Click to INSERT into CheckIns like Walk‑In
        private void BtnCheckInReservation_Click(object sender, EventArgs e)
        {
            if (cboReservation.SelectedItem == null)
                return;

            dynamic reservation = cboReservation.SelectedItem;

            try
            {
                using (var conn = _dbService.GetConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        int checkInId;

                        try
                        {
                            // 1) Mark reservation as CheckedIn
                            string updateQuery = @"
                                UPDATE Reservations
                                SET ReservationStatus = 'CheckedIn', EmployeeID = @EmployeeId, UpdatedAt = @Now
                                WHERE ReservationID = @ReservationId";

                            using (var cmd = new SqlCommand(updateQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@EmployeeId", CurrentUser.EmployeeId);
                                cmd.Parameters.AddWithValue("@Now", DateTime.Now);
                                cmd.Parameters.AddWithValue("@ReservationId", reservation.ReservationId);
                                cmd.ExecuteNonQuery();
                            }

                            // 2) Create a CheckIns row
                            string insertCheckIn = @"
                                INSERT INTO CheckIns
                                    (ReservationID, GuestID, RoomID, EmployeeID, CheckInDateTime, ExpectedCheckOutDate, NumberOfGuests, Notes, CreatedAt)
                                VALUES
                                    (@ReservationID, @GuestID, @RoomID, @EmployeeID, @CheckInDateTime, @ExpectedCheckOutDate, @NumberOfGuests, @Notes, @CreatedAt);
                                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            using (var cmd = new SqlCommand(insertCheckIn, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ReservationID", (int)reservation.ReservationId);
                                cmd.Parameters.AddWithValue("@GuestID", (int)reservation.GuestId);
                                cmd.Parameters.AddWithValue("@RoomID", (int)reservation.RoomId);
                                cmd.Parameters.AddWithValue("@EmployeeID", CurrentUser.EmployeeId);
                                cmd.Parameters.AddWithValue("@CheckInDateTime", DateTime.Now);
                                cmd.Parameters.AddWithValue("@ExpectedCheckOutDate", (DateTime)reservation.CheckOutDate);
                                cmd.Parameters.AddWithValue("@NumberOfGuests", (int)reservation.NumberOfGuests);
                                cmd.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace((string)reservation.SpecialRequests) ? (object)DBNull.Value : (string)reservation.SpecialRequests);
                                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                                checkInId = (int)cmd.ExecuteScalar();
                            }

                            // 3) Update room status to Occupied
                            UpdateRoomStatus(conn, transaction, (int)reservation.RoomId, "Occupied");

                            transaction.Commit();

                            // Log after successful commit (reference CheckInId like Walk‑In)
                            try
                            {
                                _logService.LogActivity(
                                    CurrentUser.EmployeeId,
                                    "CheckIn",
                                    $"Guest {reservation.GuestName} checked into Room {reservation.RoomNumber} (Reservation)",
                                    checkInId
                                );
                            }
                            catch { /* don't block UX on log failure */ }

                            MessageBox.Show(
                                $"Guest {reservation.GuestName} successfully checked in to Room {reservation.RoomNumber}!",
                                "Check-In Successful",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );

                            LoadPendingReservations();
                            LoadAvailableRooms();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show(
                                $"Error during reservation check-in: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void UpdateRoomStatus(SqlConnection conn, SqlTransaction transaction, int roomId, string status)
        {
            string updateRoomQuery = "UPDATE Rooms SET Status = @Status WHERE RoomID = @RoomID";

            using (var cmd = new SqlCommand(updateRoomQuery, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@RoomID", roomId);
                cmd.ExecuteNonQuery();
            }
        }

        private void ClearWalkInFields()
        {
            txtFirstName.Clear();
            txtLastName.Clear();
            txtEmail.Clear();
            txtPhone.Clear();
            cboIDType.SelectedIndex = -1;
            txtIDNumber.Clear();
            cboRoom.SelectedIndex = -1;
            numGuests.Value = 1;
            dtpCheckOut.Value = DateTime.Today.AddDays(1);
            txtNotes.Clear();
        }

        #endregion
    }
}

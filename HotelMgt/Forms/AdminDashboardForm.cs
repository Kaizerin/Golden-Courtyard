using HotelMgt.Custom;          // RoundedPanel
using HotelMgt.otherUI;
using HotelMgt.Services;
using HotelMgt.UIStyles;        // builder helpers
using HotelMgt.UserControls.Admin;
using HotelMgt.UserControls.Employee;
using HotelMgt.Utilities;
using Microsoft.Data.SqlClient; // Use Microsoft.Data.SqlClient only
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        // Admin overview grids
        private DataGridView dgvCurrentOccupancy = null!;

        // Header logo
        private RoundedPanel? _headerLogoPanel;

        private bool _suppressLogoutPrompt;
        private ImageList? _tabImages; // icons (optional)
        private Panel? _tabHost;

        // Activity Logs controls (Overview)
        private DateTimePicker dtpLogDate = null!;
        private ComboBox cboLogEmployee = null!;
        private ComboBox cboLogType = null!;
        private Label lblLogSummary = null!;
        private DataGridView dgvActivityLogs = null!;
        private Label lblActivityLogsEmpty = null!;

        // Live updates
        private DateTime? _lastActivityMax;
        private DateTime? _lastPaymentMax;

        private Dictionary<TabPage, Action> _tabRefreshActions = null!;

        // Lazy-load flags
        private bool _overviewBuilt = false;

        public AdminDashboardForm()
        {
            InitializeComponent();

            _authService = new AuthenticationService();
            _dbService = new DatabaseService();

            tabControl.SelectedIndexChanged += tabControl_SelectedIndexChanged;
            btnLogout.Click += btnLogout_Click;

            // Header design: fully delegated to builder
            AdminDashboardViewBuilder.InitializeHeader(panelHeader, lblTitle, lblWelcome, btnLogout, out _headerLogoPanel);

            // Tabs design consolidated
            var iconMap = new Dictionary<TabPage, string>();
            TryAddIcon(tabOverview, "ic_home", iconMap);
            TryAddIcon(tabCheckIn, "ic_checkin", iconMap);
            TryAddIcon(tabCheckOut, "ic_checkout", iconMap);
            TryAddIcon(tabReservations, "ic_calendar", iconMap);
            TryAddIcon(tabAvailableRooms, "ic_bed", iconMap);
            TryAddIcon(tabGuestSearch, "ic_search", iconMap);
            TryAddIcon(tabEmployeeManagement, "ic_users", iconMap);
            TryAddIcon(tabRoomManagement, "ic_settings", iconMap);
            TryAddIcon(tabRevenueReport, "ic_chart", iconMap);

            AdminDashboardViewBuilder.SetupTabs(tabControl, leftPadding: 20, iconMap, out _tabImages, out _tabHost);

            LoadUserControls();

            _tabRefreshActions = new Dictionary<TabPage, Action>
            {
                { tabRoomManagement, () =>
                    {
                        if (tabRoomManagement.Controls.Count > 0 &&
                            tabRoomManagement.Controls[0] is Panel hostPanel &&
                            hostPanel.Controls.Count > 0 &&
                            hostPanel.Controls[0] is RoomManagementControl ctrl)
                        {
                            ctrl.LoadRooms();
                        }
                    }
                },
                { tabAvailableRooms, () =>
                    {
                        if (tabAvailableRooms.Controls.Count > 0 &&
                            tabAvailableRooms.Controls[0] is Panel hostPanel &&
                            hostPanel.Controls.Count > 0 &&
                            hostPanel.Controls[0] is AvailableRoomsControl ctrl)
                        {
                            ctrl.LoadRooms();
                        }
                    }
                },
                // Add more tab-control mappings as needed
            };

            SharedTimerManager.SharedTick += SharedTimerManager_SharedTick;
        }

        private static void TryAddIcon(TabPage? page, string key, IDictionary<TabPage, string> map)
        {
            if (page != null) map[page] = key;
        }

        private async void AdminDashboardForm_Load(object sender, EventArgs e)
        {
            // Use middle name if present
            string fullName = string.IsNullOrWhiteSpace(CurrentUser.MiddleName)
                ? $"{CurrentUser.FirstName} {CurrentUser.LastName}"
                : $"{CurrentUser.FirstName} {CurrentUser.MiddleName} {CurrentUser.LastName}";

            this.Text = $"Hotel Management - {fullName} ({CurrentUser.Role})";
            lblWelcome.Text = $"Welcome, {fullName}";

            // Lazy-load Overview tab if it's the initial tab
            if (tabControl.SelectedTab == tabOverview && !_overviewBuilt)
            {
                BuildAndLoadOverviewTab();
                await LoadOverviewStatsAsync();
                await LoadActivityLogsAsync();
            }
        }

        // Lazy-load Overview tab UI and data
        private void BuildAndLoadOverviewTab()
        {
            if (_overviewBuilt) return;

            AdminDashboardViewBuilder.BuildOverviewTab(
                tabOverview,
                out lblAvailableRooms, out lblOccupiedRooms, out lblReservedRooms, out lblActiveCheckIns,
                out dgvCurrentOccupancy,
                out dtpLogDate, out cboLogEmployee, out cboLogType,
                out lblLogSummary, out dgvActivityLogs, out lblActivityLogsEmpty);

            OccupancyNotesEditor.Enable(
                dgvCurrentOccupancy,
                () => _dbService.GetConnection()
            );

            // Populate filters BEFORE wiring
            LoadEmployeesForLogFilter();
            LoadActivityTypesForLogFilter();
            cboLogEmployee.SelectedIndex = cboLogEmployee.Items.Count > 0 ? 0 : -1;
            cboLogType.SelectedIndex = cboLogType.Items.Count > 0 ? 0 : -1;

            // Wire log filters
            dtpLogDate.ValueChanged += async (_, __) => await LoadActivityLogsAsync();
            cboLogEmployee.SelectedIndexChanged += async (_, __) => await LoadActivityLogsAsync();
            cboLogType.SelectedIndexChanged += async (_, __) => await LoadActivityLogsAsync();

            _overviewBuilt = true;
        }

        // Async stats loading
        private async Task LoadOverviewStatsAsync()
        {
            try
            {
                using var conn = _dbService.GetConnection();
                await conn.OpenAsync();

                int available = 0, occupied = 0, reserved = 0, active = 0;

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Rooms WHERE Status = 'Available'", conn))
                    available = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                using (var cmd = new SqlCommand(
                    "SELECT COUNT(DISTINCT RoomID) FROM CheckIns WHERE ActualCheckOutDateTime IS NULL", conn))
                    occupied = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                using (var cmd = new SqlCommand(@"
                    SELECT COUNT(DISTINCT RoomID)
                    FROM Reservations
                    WHERE ReservationStatus IN ('Confirmed', 'Pending')
                      AND CheckInDate >= CAST(GETDATE() AS DATE)", conn))
                    reserved = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM CheckIns WHERE ActualCheckOutDateTime IS NULL", conn))
                    active = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                lblAvailableRooms.Text = available.ToString();
                lblOccupiedRooms.Text = occupied.ToString();
                lblReservedRooms.Text = reserved.ToString();
                lblActiveCheckIns.Text = active.ToString();

                if (dgvCurrentOccupancy != null)
                    await LoadCurrentOccupancyAsync(conn);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statistics: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadCurrentOccupancyAsync(SqlConnection existingConn)
        {
            DataTable dt;
            try
            {
                // Load data in background thread
                dt = await Task.Run(() => DashboardUtils.GetGuestOccupancyData(existingConn));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading current occupancy: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Update DataGridView on UI thread
            if (dgvCurrentOccupancy.InvokeRequired)
            {
                dgvCurrentOccupancy.Invoke(new Action(() => BindOccupancyGrid(dt)));
            }
            else
            {
                BindOccupancyGrid(dt);
            }
        }

        // Helper to bind and format the grid (UI thread only)
        private void BindOccupancyGrid(DataTable dt)
        {
            dgvCurrentOccupancy.SuspendLayout();
            dgvCurrentOccupancy.DataSource = dt;

            var cols = dgvCurrentOccupancy.Columns;
            if (cols["GuestName"] is { } guestCol)
                guestCol.HeaderText = "Guest";
            if (cols["Check In"] is { } checkInCol)
                checkInCol.DefaultCellStyle.Format = "yyyy-MM-dd";
            if (cols["Expected Check Out"] is { } expectedCol)
                expectedCol.DefaultCellStyle.Format = "yyyy-MM-dd";
            if (cols["Room"] is { } roomCol)
                roomCol.FillWeight = 70;
            if (cols["Contact"] is { } contactCol)
                contactCol.FillWeight = 120;
            if (cols["Guests"] is { } guestsCol)
                guestsCol.FillWeight = 60;
            if (cols["Amenities"] is { } amenitiesCol)
            {
                amenitiesCol.FillWeight = 220;
                amenitiesCol.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }
            if (cols["Notes"] is { } notesCol)
            {
                notesCol.FillWeight = 200;
                notesCol.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }

            dgvCurrentOccupancy.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            dgvCurrentOccupancy.ResumeLayout();
        }

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
                    "SELECT EmployeeID, FirstName, MiddleName, LastName FROM Employees WHERE Role = @Role ORDER BY FirstName, LastName",
                    conn);
                cmd.Parameters.AddWithValue("@Role", "Employee");

                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var id = rdr.GetInt32(0);
                    var firstName = rdr.GetString(1);
                    var middleName = rdr.IsDBNull(2) ? "" : rdr.GetString(2);
                    var lastName = rdr.GetString(3);
                    var name = string.IsNullOrWhiteSpace(middleName)
                        ? $"{firstName} {lastName}"
                        : $"{firstName} {middleName} {lastName}";
                    items.Add(new KeyValuePair<int?, string>(id, name));
                }

                cboLogEmployee.DataSource = items;
                cboLogEmployee.DisplayMember = "Value";
                cboLogEmployee.ValueMember = "Key";
                if (cboLogEmployee.Items.Count > 0) cboLogEmployee.SelectedIndex = 0;
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

        private void LoadActivityTypesForLogFilter()
        {
            var items = new List<KeyValuePair<string?, string>>
            {
                new KeyValuePair<string?, string>(null, "All Types"),
                new KeyValuePair<string?, string>("login", "Login"),
                new KeyValuePair<string?, string>("checkin", "Check-In"),
                new KeyValuePair<string?, string>("checkout", "Check-Out"),
                new KeyValuePair<string?, string>("reservation", "Reservation"),
                new KeyValuePair<string?, string>("payment", "Payment")
            };
            cboLogType.DataSource = items;
            cboLogType.DisplayMember = "Value";
            cboLogType.ValueMember = "Key";
            if (cboLogType.Items.Count > 0) cboLogType.SelectedIndex = 0;
        }

        // Async activity logs loading
        private async Task LoadActivityLogsAsync()
        {
            try
            {
                using var conn = _dbService.GetConnection();
                await conn.OpenAsync();

                var sql = @"
WITH Combined AS (
    SELECT 
        al.ActivityDateTime,
        (e.FirstName + ' ' + e.LastName) AS Employee,
        al.ActivityType,
        al.ActivityDescription,
        LOWER(REPLACE(REPLACE(al.ActivityType,'-',''),' ','')) AS NormType
    FROM ActivityLog al
    INNER JOIN Employees e ON al.EmployeeID = e.EmployeeID
    WHERE e.Role = 'Employee'
      AND CAST(al.ActivityDateTime AS DATE) = @Date
      AND (@EmpId IS NULL OR al.EmployeeID = @EmpId)

    UNION ALL

    SELECT 
        p.PaymentDate AS ActivityDateTime,
        (e2.FirstName + ' ' + e2.LastName) AS Employee,
        'Payment' AS ActivityType,
        ('Payment ' + p.PaymentStatus 
            + ' - ' + CONVERT(nvarchar(20), p.Amount) 
            + ' via ' + p.PaymentMethod
            + CASE WHEN p.TransactionReference IS NOT NULL AND LTRIM(RTRIM(p.TransactionReference)) <> '' 
                   THEN ' (Ref: ' + p.TransactionReference + ')' ELSE '' END
            + ' - Res#' + CONVERT(nvarchar(20), p.ReservationID)
            + CASE WHEN p.Notes IS NOT NULL AND LTRIM(RTRIM(p.Notes)) <> '' 
                   THEN ' - Notes: ' + p.Notes ELSE '' END
          ) AS ActivityDescription,
        'payment' AS NormType
    FROM Payments p
    INNER JOIN Employees e2 ON p.EmployeeID = e2.EmployeeID
    WHERE e2.Role = 'Employee'
      AND CAST(p.PaymentDate AS DATE) = @Date
      AND (@EmpId IS NULL OR p.EmployeeID = @EmpId)
)
SELECT ActivityDateTime, Employee, ActivityType, ActivityDescription
FROM Combined
WHERE (@TypePattern IS NULL OR NormType LIKE @TypePattern)
ORDER BY ActivityDateTime DESC;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Date", dtpLogDate.Value.Date);

                var empId = (cboLogEmployee.SelectedValue as int?) ??
                            (cboLogEmployee.SelectedItem is KeyValuePair<int?, string> kv ? kv.Key : null);
                cmd.Parameters.AddWithValue("@EmpId", (object?)empId ?? DBNull.Value);

                var typeGroup = (cboLogType.SelectedValue as string) ??
                                (cboLogType.SelectedItem is KeyValuePair<string?, string> kv2 ? kv2.Key : null);
                var typePattern = typeGroup is null ? null : $"{typeGroup}%";
                cmd.Parameters.AddWithValue("@TypePattern", (object?)typePattern ?? DBNull.Value);

                var dt = new DataTable();
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    await Task.Run(() => adapter.Fill(dt));
                }

                dgvActivityLogs.DataSource = dt;

                var cols = dgvActivityLogs.Columns;

                if (cols["ActivityDateTime"] is { } timeCol)
                {
                    timeCol.HeaderText = "Time";
                    timeCol.DefaultCellStyle.Format = "HH:mm";
                    timeCol.FillWeight = 70;
                }
                if (cols["Employee"] is { } empCol)
                {
                    empCol.HeaderText = "Employee";
                    empCol.FillWeight = 160;
                }
                if (cols["ActivityType"] is { } typeCol)
                {
                    typeCol.HeaderText = "Activity Type";
                    typeCol.FillWeight = 120;
                }
                if (cols["ActivityDescription"] is { } descCol)
                {
                    descCol.HeaderText = "Description";
                    descCol.FillWeight = 450;
                    descCol.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                }

                dgvActivityLogs.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                lblLogSummary.Text = $"Showing {dt.Rows.Count} activities for {dtpLogDate.Value:yyyy-MM-dd}";

                DateTime? newMax = null;
                foreach (DataRow row in dt.Rows)
                {
                    var ts = (DateTime)row["ActivityDateTime"];
                    if (!newMax.HasValue || ts > newMax.Value) newMax = ts;
                }
                _lastActivityMax = newMax;
                _lastPaymentMax = newMax;

                bool empty = dt.Rows.Count == 0;
                lblActivityLogsEmpty.Visible = empty;
                if (empty) lblActivityLogsEmpty.BringToFront();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading activity logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void tabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_tabRefreshActions.TryGetValue(tabControl.SelectedTab, out var refreshAction))
            {
                refreshAction();
            }

            if (tabControl?.SelectedTab == tabOverview)
            {
                if (!_overviewBuilt)
                {
                    BuildAndLoadOverviewTab();
                }
                await LoadOverviewStatsAsync();
                await LoadActivityLogsAsync();
            }
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
                    _suppressLogoutPrompt = true;
                    this.Close();
                };
                login.Show();
            }
        }

        private void AdminDashboardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_suppressLogoutPrompt) return;

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
                    _suppressLogoutPrompt = true;
                    this.Close();
                };
                login.Show();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void LogRefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (tabControl?.SelectedTab != tabOverview) return;

            try
            {
                var (activityMax, paymentMax) = GetLatestLogTimestamps();

                bool hasNewActivity = activityMax.HasValue && (!_lastActivityMax.HasValue || activityMax > _lastActivityMax);
                bool hasNewPayment = paymentMax.HasValue && (!_lastPaymentMax.HasValue || paymentMax > _lastPaymentMax);

                if (hasNewActivity || hasNewPayment)
                {
                    _ = LoadActivityLogsAsync();
                }
            }
            catch
            {
                // swallow
            }
        }

        private (DateTime? activityMax, DateTime? paymentMax) GetLatestLogTimestamps()
        {
            using var conn = _dbService.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand(@"
        SELECT MAX(ActivityDateTime)
        FROM ActivityLog
        WHERE CAST(ActivityDateTime AS DATE) = @Date
          AND (@EmpId IS NULL OR EmployeeID = @EmpId);

        SELECT MAX(PaymentDate)
        FROM Payments
        WHERE CAST(PaymentDate AS DATE) = @Date
          AND (@EmpId IS NULL OR EmployeeID = @EmpId);", conn);

            cmd.Parameters.AddWithValue("@Date", dtpLogDate.Value.Date);

            var empId = (cboLogEmployee.SelectedValue as int?) ??
                        (cboLogEmployee.SelectedItem is KeyValuePair<int?, string> kv ? kv.Key : null);
            cmd.Parameters.AddWithValue("@EmpId", (object?)empId ?? DBNull.Value);

            using var rdr = cmd.ExecuteReader();

            DateTime? aMax = null, pMax = null;

            if (rdr.Read() && !rdr.IsDBNull(0))
                aMax = rdr.GetDateTime(0);

            if (rdr.NextResult() && rdr.Read() && !rdr.IsDBNull(0))
                pMax = rdr.GetDateTime(0);

            return (aMax, pMax);
        }

        private void SharedTimerManager_SharedTick(object? sender, EventArgs e)
        {
            if (tabControl?.SelectedTab != tabOverview) return;

            try
            {
                var (activityMax, paymentMax) = GetLatestLogTimestamps();

                bool hasNewActivity = activityMax.HasValue && (!_lastActivityMax.HasValue || activityMax > _lastActivityMax);
                bool hasNewPayment = paymentMax.HasValue && (!_lastPaymentMax.HasValue || paymentMax > _lastPaymentMax);

                if (hasNewActivity || hasNewPayment)
                {
                    _ = LoadActivityLogsAsync();
                }
            }
            catch
            {
                // swallow
            }
        }

        private void LoadUserControls()
        {
            DashboardTabLoader.LoadStandardTabs(
                tabCheckIn!,
                tabCheckOut!,
                tabReservations!,
                tabAvailableRooms!,
                tabGuestSearch!,
                useScrollHost: true // Admin uses scroll host
            );

            // Employees
            var employeeManagementControl = new EmployeeManagementControl();
            InstallInScrollHost(tabEmployeeManagement!, employeeManagementControl);

            // Room Management
            var roomManagementControl = new RoomManagementControl();
            InstallInScrollHost(tabRoomManagement!, roomManagementControl);

            // Reports — RESTORE original behavior (no scroll host)
            var revenueReportControl = new RevenueReportControl();
            tabRevenueReport!.Controls.Clear();
            revenueReportControl.Dock = DockStyle.Fill;
            tabRevenueReport!.Controls.Add(revenueReportControl);
        }

        // --- Scroll host helpers unchanged ---
        private static void InstallInScrollHost(TabPage page, Control content)
        {
            if (page == null || content == null) return;

            page.SuspendLayout();
            page.Controls.Clear();

            var host = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = page.BackColor,
                Padding = new Padding(0)
            };

            content.Margin = Padding.Empty;
            content.Dock = DockStyle.Fill;
            content.MaximumSize = new Size(int.MaxValue, int.MaxValue);

            void UpdateScrollExtent()
            {
                var pref = content.PreferredSize;
                var minW = Math.Max(0, pref.Width);
                var minH = Math.Max(0, pref.Height);
                if (host.AutoScrollMinSize.Width != minW || host.AutoScrollMinSize.Height != minH)
                {
                    host.AutoScrollMinSize = new Size(minW, minH);
                }
            }

            content.Layout += (_, __) => UpdateScrollExtent();
            content.SizeChanged += (_, __) => UpdateScrollExtent();
            host.Resize += (_, __) => UpdateScrollExtent();

            host.Controls.Add(content);
            page.Controls.Add(host);

            UpdateScrollExtent();

            page.ResumeLayout(performLayout: true);
        }

        private static void InstallInScrollHostTop(TabPage page, Control content)
        {
            if (page == null || content == null) return;

            page.SuspendLayout();
            page.Controls.Clear();

            var host = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = page.BackColor,
                Padding = new Padding(0)
            };

            content.Margin = Padding.Empty;
            content.AutoSize = true;
            content.Dock = DockStyle.Top;
            content.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            content.MaximumSize = new Size(int.MaxValue, int.MaxValue);

            void SyncWidth(object? _, EventArgs __)
            {
                content.Width = host.ClientSize.Width - content.Margin.Horizontal;
            }
            host.Resize += SyncWidth;
            SyncWidth(null, EventArgs.Empty);

            host.Controls.Add(content);
            page.Controls.Add(host);

            page.ResumeLayout(performLayout: true);
        }
    }
}
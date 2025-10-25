namespace HotelMgt.Forms
{
    partial class EmployeeDashboardForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblWelcome;
        private System.Windows.Forms.Label lblRole;
        private HotelMgt.Custom.RoundedButton btnLogout; // use RoundedButton
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabOverview;
        private System.Windows.Forms.TabPage tabCheckIn;
        private System.Windows.Forms.TabPage tabCheckOut;
        private System.Windows.Forms.TabPage tabReservations;
        private System.Windows.Forms.TabPage tabAvailableRooms;
        private System.Windows.Forms.TabPage tabGuestSearch;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            tabControl = new TabControl();
            tabOverview = new TabPage();
            tabCheckIn = new TabPage();
            tabCheckOut = new TabPage();
            tabReservations = new TabPage();
            tabAvailableRooms = new TabPage();
            tabGuestSearch = new TabPage();
            panelHeader = new Panel();
            btnLogout = new HotelMgt.Custom.RoundedButton();
            lblRole = new Label();
            lblWelcome = new Label();
            lblTitle = new Label();
            tabControl.SuspendLayout();
            panelHeader.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabOverview);
            tabControl.Controls.Add(tabCheckIn);
            tabControl.Controls.Add(tabCheckOut);
            tabControl.Controls.Add(tabReservations);
            tabControl.Controls.Add(tabAvailableRooms);
            tabControl.Controls.Add(tabGuestSearch);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tabControl.Location = new Point(0, 70);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1384, 791);
            tabControl.TabIndex = 1;
            // 
            // tabOverview
            // 
            tabOverview.Location = new Point(4, 28);
            tabOverview.Name = "tabOverview";
            tabOverview.Size = new Size(1376, 759);
            tabOverview.TabIndex = 0;
            tabOverview.Text = "Overview";
            tabOverview.UseVisualStyleBackColor = true;
            // 
            // tabCheckIn
            // 
            tabCheckIn.Location = new Point(4, 28);
            tabCheckIn.Name = "tabCheckIn";
            tabCheckIn.Size = new Size(1376, 759);
            tabCheckIn.TabIndex = 1;
            tabCheckIn.Text = "Check-In";
            tabCheckIn.UseVisualStyleBackColor = true;
            // 
            // tabCheckOut
            // 
            tabCheckOut.Location = new Point(4, 28);
            tabCheckOut.Name = "tabCheckOut";
            tabCheckOut.Size = new Size(1376, 759);
            tabCheckOut.TabIndex = 2;
            tabCheckOut.Text = "Check-Out";
            tabCheckOut.UseVisualStyleBackColor = true;
            // 
            // tabReservations
            // 
            tabReservations.Location = new Point(4, 28);
            tabReservations.Name = "tabReservations";
            tabReservations.Size = new Size(1376, 759);
            tabReservations.TabIndex = 3;
            tabReservations.Text = "Reservations";
            tabReservations.UseVisualStyleBackColor = true;
            // 
            // tabAvailableRooms
            // 
            tabAvailableRooms.Location = new Point(4, 28);
            tabAvailableRooms.Name = "tabAvailableRooms";
            tabAvailableRooms.Size = new Size(1376, 759);
            tabAvailableRooms.TabIndex = 4;
            tabAvailableRooms.Text = "Available Rooms";
            tabAvailableRooms.UseVisualStyleBackColor = true;
            // 
            // tabGuestSearch
            // 
            tabGuestSearch.Location = new Point(4, 28);
            tabGuestSearch.Name = "tabGuestSearch";
            tabGuestSearch.Size = new Size(1376, 759);
            tabGuestSearch.TabIndex = 5;
            tabGuestSearch.Text = "Guest Search";
            tabGuestSearch.UseVisualStyleBackColor = true;
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(37, 99, 235);
            panelHeader.Controls.Add(btnLogout);
            panelHeader.Controls.Add(lblRole);
            panelHeader.Controls.Add(lblWelcome);
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(1384, 70);
            panelHeader.TabIndex = 0;
            panelHeader.Resize += PanelHeader_Resize;
            // 
            // btnLogout
            // 
            btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogout.BackColor = Color.FromArgb(239, 68, 68);
            btnLogout.BorderColor = Color.Transparent;
            btnLogout.BorderRadius = 8;
            btnLogout.BorderSize = 0;
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnLogout.ForeColor = Color.White;
            btnLogout.Location = new Point(1250, 15);
            btnLogout.Name = "btnLogout";
            btnLogout.Size = new Size(120, 40);
            btnLogout.TabIndex = 3;
            btnLogout.Text = "Logout";
            btnLogout.UseVisualStyleBackColor = false;
            // optional hover/press colors to avoid white flicker
            btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 64, 64);
            btnLogout.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 56, 56);
            // 
            // lblRole
            // 
            lblRole.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblRole.AutoSize = true;
            lblRole.BackColor = Color.FromArgb(59, 130, 246);
            lblRole.BorderStyle = BorderStyle.FixedSingle;
            lblRole.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRole.ForeColor = Color.Snow;
            lblRole.Location = new Point(1150, 20);
            lblRole.Name = "lblRole";
            lblRole.Size = new Size(59, 17);
            lblRole.TabIndex = 2;
            lblRole.Text = "EMPLOYEE";
            lblRole.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblWelcome
            // 
            lblWelcome.AutoSize = true;
            lblWelcome.ForeColor = Color.FromArgb(200, 220, 255);
            lblWelcome.Location = new Point(20, 42);
            lblWelcome.Name = "lblWelcome";
            lblWelcome.Size = new Size(103, 17);
            lblWelcome.TabIndex = 1;
            lblWelcome.Text = "Welcome, [Name]";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.Snow;
            lblTitle.Location = new Point(20, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(242, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Employee Dashboard";
            // 
            // EmployeeDashboardForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(240, 244, 248);
            ClientSize = new Size(1384, 861);
            Controls.Add(tabControl);
            Controls.Add(panelHeader);
            MinimumSize = new Size(1200, 700);
            Name = "EmployeeDashboardForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Hotel Management - Employee Dashboard";
            Load += EmployeeDashboardForm_Load;
            tabControl.ResumeLayout(false);
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ResumeLayout(false);
        }
    }
}
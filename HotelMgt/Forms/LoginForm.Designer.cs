// Improved design version of LoginForm.Designer.cs (names and functions unchanged)
namespace HotelMgt.Forms
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelLoginCard = new HotelMgt.Custom.RoundedPanel();
            panelLogo = new HotelMgt.Custom.RoundedPanel();
            txtUsername = new HotelMgt.Custom.RoundedTextBox();
            txtPassword = new HotelMgt.Custom.RoundedTextBox();
            btnLogin = new HotelMgt.Custom.RoundedButton();
            lbPassword = new Label();
            lblUsername = new Label();
            lblDescription = new Label();
            lblTitle = new Label();
            chkShowPassword = new CheckBox();
            panelLoginCard.SuspendLayout();
            SuspendLayout();

            // Root Form
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(241, 245, 249); // Soft gray background
            ClientSize = new Size(520, 600);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Hotel Management System - Login";

            // panelLoginCard
            panelLoginCard.BackColor = Color.White;
            panelLoginCard.BorderRadius = 16;
            panelLoginCard.Size = new Size(420, 520);
            panelLoginCard.Location = new Point((ClientSize.Width - panelLoginCard.Width) / 2, 40);
            panelLoginCard.Padding = new Padding(20, 20, 20, 20);

            // panelLogo
            panelLogo.BackColor = Color.FromArgb(37, 99, 235);
            panelLogo.BorderRadius = 12;
            panelLogo.Size = new Size(70, 70);
            panelLogo.Location = new Point((panelLoginCard.Width - panelLogo.Width) / 2, 40);

            // lblTitle
            lblTitle.AutoSize = false;
            lblTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(30, 41, 59);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Text = "Hotel Management System";
            lblTitle.Size = new Size(panelLoginCard.Width - 40, 40);
            lblTitle.Location = new Point(20, 130);

            // lblDescription
            lblDescription.AutoSize = false;
            lblDescription.Font = new Font("Segoe UI", 11F);
            lblDescription.ForeColor = Color.FromArgb(100, 116, 139);
            lblDescription.Text = "Enter your credentials to access the reception desk";
            lblDescription.TextAlign = ContentAlignment.MiddleCenter;
            lblDescription.Size = new Size(panelLoginCard.Width - 40, 30);
            lblDescription.Location = new Point(20, 175);

            // lblUsername
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblUsername.ForeColor = Color.FromArgb(30, 41, 59);
            lblUsername.Text = "Employee Username";
            lblUsername.Location = new Point(40, 230);

            // txtUsername
            txtUsername.BackColor = Color.White;
            txtUsername.BorderColor = Color.FromArgb(203, 213, 225);
            txtUsername.BorderFocusColor = Color.FromArgb(59, 130, 246);
            txtUsername.BorderRadius = 8;
            txtUsername.Font = new Font("Segoe UI", 10F);
            txtUsername.PlaceholderText = "Enter your username";
            txtUsername.Location = new Point(40, 255);
            txtUsername.Size = new Size(340, 40);

            // lbPassword
            lbPassword.AutoSize = true;
            lbPassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lbPassword.ForeColor = Color.FromArgb(30, 41, 59);
            lbPassword.Text = "Password";
            lbPassword.Location = new Point(40, 310);

            // txtPassword
            txtPassword.BackColor = Color.White;
            txtPassword.BorderColor = Color.FromArgb(203, 213, 225);
            txtPassword.BorderFocusColor = Color.FromArgb(59, 130, 246);
            txtPassword.BorderRadius = 8;
            txtPassword.Font = new Font("Segoe UI", 10F);
            txtPassword.PlaceholderText = "Enter your password";
            txtPassword.PasswordChar = true;
            txtPassword.Location = new Point(40, 335);
            txtPassword.Size = new Size(340, 40);


            // chkShowPassword
            chkShowPassword.Text = "Show Password";
            chkShowPassword.Font = new Font("Segoe UI", 9F);
            chkShowPassword.ForeColor = Color.FromArgb(100, 116, 139);
            chkShowPassword.AutoSize = true;
            chkShowPassword.Location = new Point(40, 380);
            chkShowPassword.CheckedChanged += chkShowPassword_CheckedChanged;

            // btnLogin
            btnLogin.BackColor = Color.FromArgb(37, 99, 235);
            btnLogin.BorderRadius = 10;
            btnLogin.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            btnLogin.ForeColor = Color.White;
            btnLogin.Text = "Login";
            btnLogin.Size = new Size(340, 45);
            btnLogin.Location = new Point(40, 420);
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Click += btnLogin_Click;

            panelLoginCard.Controls.Add(panelLogo);
            panelLoginCard.Controls.Add(lblTitle);
            panelLoginCard.Controls.Add(lblDescription);
            panelLoginCard.Controls.Add(lblUsername);
            panelLoginCard.Controls.Add(txtUsername);
            panelLoginCard.Controls.Add(lbPassword);
            panelLoginCard.Controls.Add(txtPassword);
            panelLoginCard.Controls.Add(chkShowPassword);
            panelLoginCard.Controls.Add(btnLogin);

            Controls.Add(panelLoginCard);

            panelLoginCard.ResumeLayout(false);
            panelLoginCard.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Custom.RoundedPanel panelLoginCard;
        private Custom.RoundedTextBox txtUsername;
        private Custom.RoundedTextBox txtPassword;
        private Custom.RoundedButton btnLogin;
        private Label lbPassword;
        private Label lblUsername;
        private Label lblDescription;
        private Label lblTitle;
        private Custom.RoundedPanel panelLogo;
        private CheckBox chkShowPassword;
    }
}

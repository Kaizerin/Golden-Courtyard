using HotelMgt.Data;

namespace HotelMgt
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Load += new System.EventHandler(this.Form1_Load);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (DatabaseHelper.TestConnection(out string error))
                MessageBox.Show("Connected to database successfully!");
            else
                MessageBox.Show($"Connection failed: {error}");

            try
            {
                var conn = DatabaseHelper.GetConnection();
                conn.Open();
                MessageBox.Show("Connected to database successfully!");
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaskControl;
using TaskControl.Models;

namespace TaskControl
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string u = tbUsername.Text.Trim();
            string p = tbPassword.Text;

            if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            DataTable dt = Db.Query(@"
SELECT 
    u.Id,
    u.Username,
    u.CreatedAt,
    r.Name AS RoleName
FROM Users u
JOIN Roles r ON r.Id = u.RoleId
WHERE u.Username = @u AND u.PasswordHash = @p",
                new SqlParameter("@u", u),
                new SqlParameter("@p", p));

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Неверный логин или пароль");
                return;
            }

            UserSession.UserId = Convert.ToInt32(dt.Rows[0]["Id"]);
            UserSession.Username = dt.Rows[0]["Username"].ToString();
            UserSession.RoleName = dt.Rows[0]["RoleName"].ToString();

            var main = new MainForm();
            main.Show();
            this.Hide();
        }




        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }
    }
}

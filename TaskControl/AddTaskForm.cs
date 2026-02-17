using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaskControl.Models;

namespace TaskControl
{
    public partial class AddTaskForm : Form
    {
        public string TaskTitle => tbTitle.Text.Trim();
        public int Days => (int)nudDays.Value;
        public int SelectedExecutorId
        {
            get
            {
                if (cbExecutor.SelectedValue == null) return UserSession.UserId; // fallback
                return (int)cbExecutor.SelectedValue;
            }
        }
        private void LoadExecutors()
        {
            DataTable dt = Db.Query(@"
SELECT Id, Username
FROM Users
ORDER BY Username");

            cbExecutor.DisplayMember = "Username";
            cbExecutor.ValueMember = "Id";
            cbExecutor.DataSource = dt;

            cbExecutor.SelectedValue = UserSession.UserId;
        }

        public AddTaskForm()
        {
            InitializeComponent();
            LoadExecutors();
        }
    }
}

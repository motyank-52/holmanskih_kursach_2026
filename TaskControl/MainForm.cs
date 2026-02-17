using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IdentityModel.Selectors;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaskControl.Models;

namespace TaskControl
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            LoadTasks();

            this.BackColor = Color.FromArgb(245, 246, 248);
            panelTop.BackColor = Color.FromArgb(235, 235, 235);    

            gvTasks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gvTasks.RowHeadersVisible = false;
            gvTasks.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gvTasks.MultiSelect = false;
            gvTasks.ReadOnly = true;
            gvTasks.AllowUserToAddRows = false;
            gvTasks.AllowUserToDeleteRows = false;
            gvTasks.AllowUserToResizeRows = false;
            gvTasks.BackgroundColor = System.Drawing.SystemColors.Window;
            gvTasks.BackgroundColor = Color.White;
            gvTasks.GridColor = Color.LightGray;
            gvTasks.BorderStyle = BorderStyle.None;
            gvTasks.EnableHeadersVisualStyles = false;
            gvTasks.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(230, 230, 230);
            gvTasks.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            gvTasks.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);
            gvTasks.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            gvTasks.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 240);
            gvTasks.DefaultCellStyle.SelectionForeColor = Color.Black;
            gvTasks.BorderStyle = BorderStyle.FixedSingle;
            if (gvTasks.Columns.Contains("Срок"))
                gvTasks.Columns["Срок"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm";
            if (gvTasks.Columns.Contains("Id"))
                gvTasks.Columns["Id"].Visible = false;
        }

        private void LoadTasks(bool onlyOverdue = false)
        {
            string where = "";
            string roleFilter = "";
            string overdueFilter = "";

            // Фильтр по роли
            if (UserSession.RoleName != "Admin")
            {
                roleFilter = "t.ExecutorId = @uid";
            }

            // Фильтр просроченных
            if (onlyOverdue)
            {
                overdueFilter = "t.Deadline < GETDATE() AND s.Name <> N'Завершена'";
            }

            // Собираем WHERE
            if (!string.IsNullOrEmpty(roleFilter) && !string.IsNullOrEmpty(overdueFilter))
                where = $"WHERE {roleFilter} AND {overdueFilter}";
            else if (!string.IsNullOrEmpty(roleFilter))
                where = $"WHERE {roleFilter}";
            else if (!string.IsNullOrEmpty(overdueFilter))
                where = $"WHERE {overdueFilter}";

            gvTasks.DataSource = Db.Query($@"
SELECT 
    t.Id,
    t.Title AS [Название],
    s.Name AS [Статус],
    p.Name AS [Приоритет],
    u1.Username AS [Создатель],
    u2.Username AS [Исполнитель],
    t.Deadline AS [Срок]
FROM Tasks t
JOIN TaskStatuses s ON s.Id = t.StatusId
JOIN TaskPriorities p ON p.Id = t.PriorityId
JOIN Users u1 ON u1.Id = t.CreatorId
JOIN Users u2 ON u2.Id = t.ExecutorId
{where}
ORDER BY t.Deadline",
                new SqlParameter("@uid", UserSession.UserId));
        }
        private string _username;
        private string _role;
        private DateTime _createdAt;

        public MainForm(string role, string username, DateTime createdAt)
        {
            InitializeComponent();
            _createdAt = createdAt;
            _username = username;
            _role = role;
            LoadTasks();
            ApplyRolePermissions();
        }
        
        private void btnAll_Click(object sender, EventArgs e)
        {
            LoadTasks(false);
        }

        private void btnOverdue_Click(object sender, EventArgs e)
        {
            LoadTasks(true);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using (var f = new AddTaskForm())
            {
                if (f.ShowDialog() != DialogResult.OK) return;
                if (string.IsNullOrWhiteSpace(f.TaskTitle))
                {
                    MessageBox.Show("Введите название задачи");
                    return;
                }

                Db.Exec(@"
INSERT INTO Tasks (Title, Description, CreatorId, ExecutorId, StatusId, PriorityId, Deadline)
VALUES (@t, NULL, @creator, @executor, 1, 2, DATEADD(day, @d, GETDATE()))",
                    new SqlParameter("@t", f.TaskTitle),
                    new SqlParameter("@creator", UserSession.UserId),
                    new SqlParameter("@executor", f.SelectedExecutorId),
                    new SqlParameter("@d", f.Days));

                LoadTasks(false);
            }
        }

        private int? SelectedTaskId()
        {
            if (gvTasks.CurrentRow == null) return null;
            return Convert.ToInt32(gvTasks.CurrentRow.Cells["Id"].Value);
        }
        private void btnMarkDone_Click(object sender, EventArgs e)
        {
            var id = SelectedTaskId();
            if (id == null) return;
            Db.Exec("UPDATE Tasks SET StatusId = 3 WHERE Id = @id",
                new SqlParameter("@id", id.Value));

            LoadTasks();
        }
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadTasks();
        } 
        
        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        private void ApplyRolePermissions()
        {
            if (_role != "Admin")
            {
                btnDelete.Enabled = false;
            }
        }
        private void gvTasks_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            int id = Convert.ToInt32(gvTasks.Rows[e.RowIndex].Cells["Id"].Value);

            if (MessageBox.Show("Пометить задачу как выполненную?",
                "Статус",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Db.Exec("UPDATE Tasks SET StatusId = 3 WHERE Id = @id",
                    new SqlParameter("@id", id));

                LoadTasks();
            }
        }


        private void gvTasks_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow row in gvTasks.Rows)
            {
                if (row.IsNewRow) continue;

                string status = row.Cells["Статус"].Value?.ToString();
                DateTime deadline = Convert.ToDateTime(row.Cells["Срок"].Value);

                // Просрочена
                if (deadline < DateTime.Now && status != "Завершена")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230); // красный
                    continue;
                }

                if (status == "Новая")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(230, 240, 255); // синий
                }
                else if (status == "В работе")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 220); // жёлтый
                }
                else if (status == "Завершена")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(235, 235, 235); // серый
                    row.DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (gvTasks.CurrentRow == null)
            {
                MessageBox.Show("Выберите задачу");
                return;
            }

            int id = Convert.ToInt32(gvTasks.CurrentRow.Cells["Id"].Value);

            var confirm = MessageBox.Show(
                "Удалить выбранную задачу?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            Db.Exec("DELETE FROM Tasks WHERE Id = @id",
                new SqlParameter("@id", id));

            LoadTasks(false);
        }
    }
}

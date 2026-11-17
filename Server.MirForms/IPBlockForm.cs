using System;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using Server.MirEnvir;
using Server.MirNetwork;

namespace Server
{
    /// <summary>
    /// Manages the server IP ban list. Bans live in Envir.IPBlocks and every change is written to
    /// Envir/IPBlocks.txt, so they survive a restart.
    /// </summary>
    public partial class IPBlockForm : Form
    {
        public IPBlockForm()
        {
            InitializeComponent();
            UiText.Apply(this);
            RefreshList();
        }

        private static string Trans(string key, string source)
        {
            return GameLanguage.ServerStore.Translate(key, source);
        }

        private void RefreshList()
        {
            var now = DateTime.UtcNow;
            var blocks = Envir.IPBlocks.OrderBy(x => x.Value).ToArray();

            lvBlocks.BeginUpdate();
            lvBlocks.Items.Clear();

            foreach (var block in blocks)
            {
                var item = new ListViewItem(block.Key) { Tag = block.Key };

                item.SubItems.Add(block.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(block.Value > now
                    ? FormatRemaining(block.Value - now)
                    : Trans("ui.IPBlockForm.expired", "expired"));

                lvBlocks.Items.Add(item);
            }

            lvBlocks.EndUpdate();

            lblCount.Text = string.Format(Trans("ui.IPBlockForm.count", "Blocked IPs: {0}"), blocks.Length);
        }

        private static string FormatRemaining(TimeSpan remaining)
        {
            return remaining.TotalDays >= 1
                ? $"{remaining.Days}d {remaining.Hours}h"
                : $"{remaining.Hours}h {remaining.Minutes}m";
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!IPAddress.TryParse(txtIP.Text.Trim(), out var address))
            {
                MessageBox.Show(Trans("ui.IPBlockForm.badAddress", "Enter a valid IPv4 or IPv6 address."),
                    Trans("ui.IPBlockForm.#Title", "Blocked IPs"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Envir.Main.UpdateIPBlock(NetUtil.AccessKey(address), TimeSpan.FromHours((double)numHours.Value));

            txtIP.Clear();

            RefreshList();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lvBlocks.SelectedItems.Count == 0) return;

            foreach (ListViewItem item in lvBlocks.SelectedItems.Cast<ListViewItem>().ToList())
                Envir.Main.RemoveIPBlock(item.Tag as string);

            RefreshList();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (Envir.IPBlocks.IsEmpty) return;

            if (MessageBox.Show(Trans("ui.IPBlockForm.clearConfirm", "Remove all IP bans?"),
                    Trans("ui.IPBlockForm.#Title", "Blocked IPs"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            Envir.Main.ClearIPBlocks();

            RefreshList();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}

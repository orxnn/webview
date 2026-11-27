using System;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Mini_Browser_Project
{
    public partial class Form1 : Form
    {
        private const string HomePage = "https://www.example.com";
        private bool _closingPlusGuard = false;

        public Form1()
        {
            InitializeComponent();
        }

        // ================= FORM LOAD =================
        private async void Form1_Load(object sender, EventArgs e)
        {
            // Başlanğıc seçilmişlər
            lstFavorites.Items.Add("google.com");
            lstFavorites.Items.Add("wikipedia.org");
            lstFavorites.Items.Add("youtube.com");
            lstFavorites.Items.Add("azertag.az");

            await InitWebView(webView21);
            await InitWebView(webView22);

            webView21.CoreWebView2.Navigate(HomePage);
        }

        // ================= WEBVIEW2 HAZIRLAMA =================
        private async System.Threading.Tasks.Task InitWebView(WebView2 wv)
        {
            await wv.EnsureCoreWebView2Async(null);

            // Səhifə yüklənəndə: URL yenilə + tarixçəyə yaz
            wv.CoreWebView2.NavigationCompleted += (s, args) =>
            {
                if (args.IsSuccess && wv.Source != null)
                {
                    if (GetCurrentWebView() == wv)
                        txtUrl.Text = wv.Source.ToString();

                    AddToHistory(wv.Source.Host);
                }
            };

            // Tab başlığı = səhifənin başlığı
            wv.CoreWebView2.DocumentTitleChanged += (s, args) =>
            {
                if (wv.Parent is TabPage page)
                {
                    string title = wv.CoreWebView2.DocumentTitle;
                    if (string.IsNullOrWhiteSpace(title)) title = "Yeni tab";
                    page.Text = title.Length > 18 ? title.Substring(0, 18) + "…" : title;
                }
            };

            // target=_blank linkləri yeni tabda açılsın
            wv.CoreWebView2.NewWindowRequested += (s, args) =>
            {
                args.Handled = true;
                AddNewTab(args.Uri);
            };
        }

        // ================= KÖMƏKÇİ METODLAR =================
        private WebView2 GetCurrentWebView()
        {
            var page = tabControl1.SelectedTab;
            if (page == null || page == tabPlus || page.Controls.Count == 0) return null;
            return page.Controls[0] as WebView2;
        }

        private string NormalizeUrl(string input)
        {
            input = input.Trim();
            if (string.IsNullOrEmpty(input)) return HomePage;

            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return input;

            // Boşluq var və ya nöqtə yoxdursa -> axtarış
            if (input.Contains(" ") || !input.Contains("."))
                return "https://www.bing.com/search?q=" + Uri.EscapeDataString(input);

            return "https://" + input;
        }

        private void NavigateTo(string input)
        {
            var wv = GetCurrentWebView();
            if (wv?.CoreWebView2 == null) return;
            wv.CoreWebView2.Navigate(NormalizeUrl(input));
        }

        private void AddToHistory(string host)
        {
            if (string.IsNullOrEmpty(host)) return;
            if (lstHistory.Items.Count > 0 && lstHistory.Items[0].ToString() == host) return;
            lstHistory.Items.Insert(0, host);
        }

        private async void AddNewTab(string url)
        {
            var wv = new WebView2 { Dock = DockStyle.Fill };
            var page = new TabPage("Yeni tab");
            page.Controls.Add(wv);

            // "+" tabından əvvələ əlavə et
            int plusIndex = tabControl1.TabPages.IndexOf(tabPlus);
            tabControl1.TabPages.Insert(plusIndex, page);
            tabControl1.SelectedTab = page;

            await InitWebView(wv);
            wv.CoreWebView2.Navigate(NormalizeUrl(url));
        }

        // ================= DÜYMƏLƏR =================
        private void btnBack_Click(object sender, EventArgs e)
        {
            var wv = GetCurrentWebView();
            if (wv?.CoreWebView2 != null && wv.CoreWebView2.CanGoBack)
                wv.CoreWebView2.GoBack();
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            var wv = GetCurrentWebView();
            if (wv?.CoreWebView2 != null && wv.CoreWebView2.CanGoForward)
                wv.CoreWebView2.GoForward();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            GetCurrentWebView()?.CoreWebView2?.Reload();
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            NavigateTo(HomePage);
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            NavigateTo(txtUrl.Text);
        }

        private void txtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                NavigateTo(txtUrl.Text);
            }
        }

        // ================= SEÇİLMİŞLƏR =================
        private void btnAddFav_Click(object sender, EventArgs e)
        {
            var wv = GetCurrentWebView();
            if (wv?.Source == null) return;

            string host = wv.Source.Host;
            if (string.IsNullOrEmpty(host)) return;

            if (lstFavorites.Items.Contains(host))
            {
                MessageBox.Show("Bu sayt artıq seçilmişlərdədir.", "Məlumat",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            lstFavorites.Items.Add(host);
        }

        private void lstFavorites_DoubleClick(object sender, EventArgs e)
        {
            if (lstFavorites.SelectedItem != null)
                NavigateTo(lstFavorites.SelectedItem.ToString());
        }

        private void lstHistory_DoubleClick(object sender, EventArgs e)
        {
            if (lstHistory.SelectedItem != null)
                NavigateTo(lstHistory.SelectedItem.ToString());
        }

        // ================= TAB-LAR =================
        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // "+" tabına klik -> yeni tab aç, "+" seçilməsin
            if (e.TabPage == tabPlus && !_closingPlusGuard)
            {
                e.Cancel = true;
                AddNewTab(HomePage);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var wv = GetCurrentWebView();
            if (wv?.Source != null)
                txtUrl.Text = wv.Source.ToString();
        }
    }
}

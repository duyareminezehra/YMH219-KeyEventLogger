using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using YMH219_KeyEventLogger.Models;
using YMH219_KeyEventLogger.Services;

namespace YMH219_KeyEventLogger.Forms
{
    /// <summary>
    /// Eğitsel Key Event Logger uygulamasının ana Windows Forms arayüzü.
    /// Yalnızca kendi penceresi aktifken KeyDown, KeyPress ve KeyUp olaylarını yakalar.
    /// </summary>
    public class MainForm : Form
    {
        private readonly KeyLoggerService _loggerService = new KeyLoggerService();

        private Panel _headerPanel = null!;
        private Panel _statusBadgePanel = null!;
        private Label _lblStatusIndicator = null!;
        private Button _btnStart = null!;
        private Button _btnStop = null!;
        private Button _btnClear = null!;
        private Button _btnExportTxt = null!;
        private CheckBox _chkKeyUp = null!;
        private CheckBox _chkKeyPress = null!;

        private TextBox _txtTestInput = null!;
        private CheckBox _chkEnableTimeFilter = null!;
        private DateTimePicker _dtpFrom = null!;
        private DateTimePicker _dtpTo = null!;
        private TextBox _txtSearchKey = null!;
        private Button _btnApplyFilter = null!;
        private Button _btnResetFilter = null!;

        private ListBox _lstEventLogs = null!;
        private Label _lblTotalEvents = null!;
        private Label _lblTotalKeyDown = null!;
        private Label _lblUniqueKeys = null!;
        private ListBox _lstTopKeys = null!;
        private Label _lblFooterNote = null!;

        private bool _isFilterActive = false;

        public MainForm()
        {
            Text = "YMH219 - Eğitsel Key Event Logger (Klavye Olay Günlüğü)";
            Size = new Size(980, 660);
            MinimumSize = new Size(880, 580);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            BackColor = Color.FromArgb(241, 245, 249);
            Font = new Font("Segoe UI", 9.2f);

            InitializeComponents();
            BindEvents();
            UpdateUiState();
        }

        private void InitializeComponents()
        {
            // 1. Üst Kontrol ve Durum Çubuğu
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(14, 12, 14, 12)
            };

            _btnStart = CreateStyledButton("Kaydı Başlat", new Point(14, 15), new Size(118, 38), Color.FromArgb(22, 163, 74));
            _btnStop = CreateStyledButton("Kaydı Durdur", new Point(140, 15), new Size(118, 38), Color.FromArgb(220, 38, 38));
            _btnClear = CreateStyledButton("Temizle", new Point(266, 15), new Size(96, 38), Color.FromArgb(71, 85, 105));
            _btnExportTxt = CreateStyledButton(".txt Kaydet", new Point(370, 15), new Size(115, 38), Color.FromArgb(37, 99, 235));

            _chkKeyPress = new CheckBox
            {
                Text = "KeyPress",
                Checked = true,
                Location = new Point(500, 14),
                AutoSize = true,
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 8.8f)
            };

            _chkKeyUp = new CheckBox
            {
                Text = "KeyUp",
                Checked = true,
                Location = new Point(500, 36),
                AutoSize = true,
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 8.8f)
            };

            _statusBadgePanel = new Panel
            {
                Location = new Point(605, 13),
                Size = new Size(345, 42),
                BackColor = Color.FromArgb(153, 27, 27)
            };

            _lblStatusIndicator = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "● DURUM: KAYIT DURDURULDU",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            _statusBadgePanel.Controls.Add(_lblStatusIndicator);

            _headerPanel.Controls.AddRange(new Control[]
            {
                _btnStart,
                _btnStop,
                _btnClear,
                _btnExportTxt,
                _chkKeyPress,
                _chkKeyUp,
                _statusBadgePanel
            });

            // 2. Test Yazı Alanı ve Tarih/Saat Filtreleme Paneli
            var middleControlsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 112,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10)
            };

            var lblInputPrompt = new Label
            {
                Text = "Deneme Yazı Alanı (Bu pencere aktifken bastığınız tüm tuşlar anlık yakalanır):",
                Location = new Point(14, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            _txtTestInput = new TextBox
            {
                Location = new Point(14, 32),
                Size = new Size(935, 27),
                Font = new Font("Segoe UI", 10f),
                PlaceholderText = "Kaydı başlattıktan sonra buraya metin yazarak KeyDown, KeyPress ve KeyUp olaylarını test edebilirsiniz..."
            };

            _chkEnableTimeFilter = new CheckBox
            {
                Text = "Saat Aralığı Filtresi:",
                Location = new Point(14, 72),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _dtpFrom = new DateTimePicker
            {
                Location = new Point(160, 69),
                Size = new Size(105, 25),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Now.AddMinutes(-15)
            };

            var lblDash = new Label
            {
                Text = "—",
                Location = new Point(270, 73),
                AutoSize = true
            };

            _dtpTo = new DateTimePicker
            {
                Location = new Point(290, 69),
                Size = new Size(105, 25),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Now.AddMinutes(5)
            };

            var lblSearchKey = new Label
            {
                Text = "Tuş / Olay Ara:",
                Location = new Point(415, 73),
                AutoSize = true,
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _txtSearchKey = new TextBox
            {
                Location = new Point(512, 69),
                Size = new Size(155, 25),
                PlaceholderText = "Örn: Space, Return, A..."
            };

            _btnApplyFilter = CreateStyledButton("Filtrele", new Point(680, 67), new Size(110, 30), Color.FromArgb(14, 116, 144));
            _btnResetFilter = CreateStyledButton("Tümünü Göster", new Point(798, 67), new Size(150, 30), Color.FromArgb(100, 116, 139));

            middleControlsPanel.Controls.AddRange(new Control[]
            {
                lblInputPrompt,
                _txtTestInput,
                _chkEnableTimeFilter,
                _dtpFrom,
                lblDash,
                _dtpTo,
                lblSearchKey,
                _txtSearchKey,
                _btnApplyFilter,
                _btnResetFilter
            });

            // 3. Alt Güvenlik Bilgilendirme Çubuğu
            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.FromArgb(226, 232, 240)
            };

            _lblFooterNote = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Text = "🔒 Güvenlik Sınırı: Bu uygulama yalnızca kendi penceresi odaktayken (aktifken) tuş olaylarını yakalar; arka planda veya global olarak çalışmaz.",
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
            };
            footerPanel.Controls.Add(_lblFooterNote);

            // 4. Ana İçerik Alanı (Sol: Olay Listesi, Sağ: İstatistik Paneli)
            var mainBodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 10, 14, 10)
            };

            var rightStatsGroup = new GroupBox
            {
                Text = "Canlı Tuş İstatistikleri (Bonus)",
                Dock = DockStyle.Right,
                Width = 265,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(10)
            };

            _lblTotalEvents = new Label
            {
                Text = "Toplam Olay Kaydı: 0",
                Location = new Point(14, 28),
                Size = new Size(235, 22),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };

            _lblTotalKeyDown = new Label
            {
                Text = "Toplam Tuş Basımı (KeyDown): 0",
                Location = new Point(14, 54),
                Size = new Size(235, 22),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            _lblUniqueKeys = new Label
            {
                Text = "Farklı Tuş Çeşidi: 0",
                Location = new Point(14, 78),
                Size = new Size(235, 22),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            var lblTopKeysHeader = new Label
            {
                Text = "En Sık Kullanılan Tuşlar:",
                Location = new Point(14, 110),
                Size = new Size(235, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _lstTopKeys = new ListBox
            {
                Location = new Point(14, 134),
                Size = new Size(236, 240),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Consolas", 9.5f),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            rightStatsGroup.Controls.AddRange(new Control[]
            {
                _lblTotalEvents,
                _lblTotalKeyDown,
                _lblUniqueKeys,
                lblTopKeysHeader,
                _lstTopKeys
            });

            var leftLogsGroup = new GroupBox
            {
                Text = "Yakalanan Klavye Olayları (Zaman Damgalı Kayıt Listesi)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(10)
            };

            _lstEventLogs = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.2f),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(226, 232, 240),
                HorizontalScrollbar = true
            };

            leftLogsGroup.Controls.Add(_lstEventLogs);

            mainBodyPanel.Controls.Add(leftLogsGroup);
            mainBodyPanel.Controls.Add(rightStatsGroup);

            Controls.Add(mainBodyPanel);
            Controls.Add(middleControlsPanel);
            Controls.Add(_headerPanel);
            Controls.Add(footerPanel);
        }

        private static Button CreateStyledButton(string text, Point location, Size size, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void BindEvents()
        {
            _btnStart.Click += (_, _) =>
            {
                _loggerService.StartRecording();
                _txtTestInput.Focus();
            };

            _btnStop.Click += (_, _) => _loggerService.StopRecording();

            _btnClear.Click += (_, _) =>
            {
                _loggerService.ClearLogs();
                _txtTestInput.Clear();
                RefreshLogListAndStats();
            };

            _btnExportTxt.Click += (_, _) => ExportLogsToFileWithDialog();

            _chkKeyUp.CheckedChanged += (_, _) => _loggerService.CaptureKeyUp = _chkKeyUp.Checked;
            _chkKeyPress.CheckedChanged += (_, _) => _loggerService.CaptureKeyPress = _chkKeyPress.Checked;

            _btnApplyFilter.Click += (_, _) =>
            {
                _isFilterActive = true;
                RefreshLogListAndStats();
            };

            _btnResetFilter.Click += (_, _) =>
            {
                _isFilterActive = false;
                _chkEnableTimeFilter.Checked = false;
                _txtSearchKey.Clear();
                RefreshLogListAndStats();
            };

            _loggerService.RecordingStateChanged += (_, _) => UpdateUiState();
            _loggerService.LogsCleared += (_, _) => RefreshLogListAndStats();
            _loggerService.EntryAdded += (_, entry) =>
            {
                if (!_isFilterActive)
                {
                    _lstEventLogs.Items.Add(entry.ToDisplayString());
                    _lstEventLogs.TopIndex = Math.Max(0, _lstEventLogs.Items.Count - 1);
                    UpdateStatisticsPanel();
                }
                else
                {
                    RefreshLogListAndStats();
                }
            };

            // Form klavye olayları (KeyPreview = true sayesinde yalnızca form aktifken tetiklenir)
            KeyDown += MainForm_KeyDown;
            KeyPress += MainForm_KeyPress;
            KeyUp += MainForm_KeyUp;
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_txtSearchKey.Focused) return;
            _loggerService.RecordKeyEvent(KeyEventType.KeyDown, e.KeyCode, e.Modifiers);
        }

        private void MainForm_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (_txtSearchKey.Focused) return;
            _loggerService.RecordKeyEvent(KeyEventType.KeyPress, Keys.None, ModifierKeys, e.KeyChar);
        }

        private void MainForm_KeyUp(object? sender, KeyEventArgs e)
        {
            if (_txtSearchKey.Focused) return;
            _loggerService.RecordKeyEvent(KeyEventType.KeyUp, e.KeyCode, e.Modifiers);
        }

        private void UpdateUiState()
        {
            bool recording = _loggerService.IsRecording;
            _btnStart.Enabled = !recording;
            _btnStop.Enabled = recording;

            if (recording)
            {
                _statusBadgePanel.BackColor = Color.FromArgb(21, 128, 61);
                _lblStatusIndicator.Text = "● DURUM: KAYIT AKTİF (Pencere Odakta)";
            }
            else
            {
                _statusBadgePanel.BackColor = Color.FromArgb(185, 28, 28);
                _lblStatusIndicator.Text = "■ DURUM: KAYIT DURDURULDU";
            }
        }

        private List<KeyLogEntry> GetCurrentlyDisplayedEntries()
        {
            if (!_isFilterActive)
            {
                return new List<KeyLogEntry>(_loggerService.AllEntries);
            }

            DateTime? fromTime = _chkEnableTimeFilter.Checked ? _dtpFrom.Value : null;
            DateTime? toTime = _chkEnableTimeFilter.Checked ? _dtpTo.Value : null;
            return _loggerService.GetFilteredEntries(fromTime, toTime, _txtSearchKey.Text);
        }

        private void RefreshLogListAndStats()
        {
            List<KeyLogEntry> entries = GetCurrentlyDisplayedEntries();
            _lstEventLogs.BeginUpdate();
            _lstEventLogs.Items.Clear();
            foreach (KeyLogEntry entry in entries)
            {
                _lstEventLogs.Items.Add(entry.ToDisplayString());
            }
            if (_lstEventLogs.Items.Count > 0)
            {
                _lstEventLogs.TopIndex = _lstEventLogs.Items.Count - 1;
            }
            _lstEventLogs.EndUpdate();

            UpdateStatisticsPanel();
        }

        private void UpdateStatisticsPanel()
        {
            _lblTotalEvents.Text = $"Toplam Olay Kaydı: {_loggerService.TotalEventCount}";
            _lblTotalKeyDown.Text = $"Toplam Tuş Basımı (KeyDown): {_loggerService.TotalKeyDownCount}";
            _lblUniqueKeys.Text = $"Farklı Tuş Çeşidi: {_loggerService.UniqueKeyCount}";

            _lstTopKeys.BeginUpdate();
            _lstTopKeys.Items.Clear();
            List<KeyUsageStatistic> topStats = _loggerService.GetTopUsedKeys(8);
            if (topStats.Count == 0)
            {
                _lstTopKeys.Items.Add("Henüz kayıt yok.");
            }
            else
            {
                int rank = 1;
                foreach (KeyUsageStatistic stat in topStats)
                {
                    _lstTopKeys.Items.Add($"{rank}. {stat}");
                    rank++;
                }
            }
            _lstTopKeys.EndUpdate();
        }

        private void ExportLogsToFileWithDialog()
        {
            List<KeyLogEntry> entries = GetCurrentlyDisplayedEntries();
            if (entries.Count == 0)
            {
                MessageBox.Show(
                    "Kaydedilecek herhangi bir klavye olayı bulunamadı. Lütfen önce kaydı başlatıp birkaç tuşa basın.",
                    "Kayıt Listesi Boş",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Title = "Klavye Olay Kayıtlarını .txt Dosyasına Kaydet",
                Filter = "Metin Dosyası (*.txt)|*.txt|Tüm Dosyalar (*.*)|*.*",
                FileName = $"KeyEventLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _loggerService.ExportToTextFile(saveDialog.FileName, entries);
                    MessageBox.Show(
                        $"Toplam {entries.Count} adet olay kaydı başarıyla dosyaya aktarıldı:\n{saveDialog.FileName}",
                        "Kaydetme Başarılı",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Dosya kaydedilirken bir hata oluştu:\n{ex.Message}",
                        "Dosya Hatası",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// README.md dokümantasyonu için kayıt aktif ve filtrelenmiş görünümlerin ekran görüntüsünü üretir.
        /// </summary>
        public void SaveDocumentationScreenshots(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            Show();
            PerformLayout();

            // 1. Ekran Görüntüsü: Kayıt Aktif ve Canlı Tuş Listesi + İstatistikler
            _txtTestInput.Text = "Merhaba NTP 2026";
            _isFilterActive = false;
            _loggerService.LoadSampleDemoData(activeState: true);
            RefreshLogListAndStats();
            UpdateUiState();
            Refresh();
            SaveSnapshot(Path.Combine(outputDirectory, "keylogger_active.png"));

            // 2. Ekran Görüntüsü: Kayıt Durduruldu ve Arama/Filtreleme Uygulandı
            _loggerService.StopRecording();
            _txtSearchKey.Text = "KeyDown";
            _isFilterActive = true;
            RefreshLogListAndStats();
            UpdateUiState();
            Refresh();
            SaveSnapshot(Path.Combine(outputDirectory, "keylogger_filtered.png"));
        }

        private void SaveSnapshot(string filePath)
        {
            using var bmp = new Bitmap(Width, Height);
            DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
            bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
